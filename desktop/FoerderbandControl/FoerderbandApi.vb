Imports System.Globalization
Imports System.Net.Http
Imports System.Web.Script.Serialization

''' <summary>
''' Zustand des Foerderbands, wie ihn /api/status liefert. Jede Aktion der API
''' antwortet ebenfalls mit diesem Objekt - nach einem Befehl muss also nicht
''' extra nachgefragt werden.
''' </summary>
Public Class BeltStatus
    Public Property Running As Boolean
    Public Property Sensor As Boolean
    Public Property StepsTotal As Long
    Public Property StepsDone As Long
    Public Property StepsRemaining As Long
    Public Property MmTotal As Double
    Public Property MmDone As Double
    Public Property StepsPerMm As Double
    Public Property StepsPerRev As Integer
    Public Property DelayUs As Integer
    Public Property Umdrehungen As Integer
    Public Property WalzeMm As Double
    Public Property Version As String
    Public Property Ip As String
    Public Property Rssi As Integer

    ''' <summary> Bandweg einer Umdrehung in mm (PI * Walzendurchmesser). </summary>
    Public ReadOnly Property MmProUmdrehung As Double
        Get
            Return Math.PI * WalzeMm
        End Get
    End Property
End Class

''' <summary> Fehler, den das Geraet selbst gemeldet hat (ok:false in der Antwort). </summary>
Public Class FoerderbandException
    Inherits Exception

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub
End Class

''' <summary>
''' Schmaler Client fuer die JSON-API der Foerderband-Firmware ab v1.3.0.
''' Alle Endpoints vertragen GET mit Query-Parametern, deshalb kommt der Client
''' ohne Request-Bodies aus.
''' </summary>
Public Class FoerderbandApi

    Private ReadOnly _http As HttpClient
    Private ReadOnly _serializer As New JavaScriptSerializer()

    ''' <summary> Hostname oder IP, z.B. "foerderband.local" oder "192.168.2.40". </summary>
    Public Property Host As String

    Public Sub New(host As String)
        Me.Host = host
        _http = New HttpClient()
        ' Kurz halten: ein nicht erreichbares Geraet soll die Oberflaeche nicht einfrieren.
        _http.Timeout = TimeSpan.FromSeconds(5)
    End Sub

    Public Async Function GetStatusAsync() As Task(Of BeltStatus)
        Return Await CallAsync("/api/status")
    End Function

    ''' <summary> Faehrt die angegebene Strecke in Millimetern. </summary>
    Public Async Function RunMillimetersAsync(mm As Double) As Task(Of BeltStatus)
        Return Await CallAsync("/api/run?mm=" & Num(mm))
    End Function

    ''' <summary> Bricht einen laufenden Auftrag ab und schaltet den Treiber stromlos. </summary>
    Public Async Function StopAsync() As Task(Of BeltStatus)
        Return Await CallAsync("/api/stop")
    End Function

    ''' <summary> Schreibt die Motoreinstellungen und sichert sie im EEPROM des ESP. </summary>
    Public Async Function SaveConfigAsync(delayUs As Integer, umdrehungen As Integer,
                                          walzeMm As Double) As Task(Of BeltStatus)
        Dim query = String.Format("/api/config?delay={0}&umdr={1}&durchm={2}",
                                  delayUs, umdrehungen, Num(walzeMm))
        Return Await CallAsync(query)
    End Function

    ' ---------- intern ----------

    ''' <summary>
    ''' Ruft einen Endpoint auf und macht aus der Antwort einen BeltStatus.
    ''' Der Body wird auch bei HTTP-Fehlercodes gelesen: das Geraet legt seine
    ''' Begruendung dort ab (z.B. 409 "Band laeuft bereits").
    ''' </summary>
    Private Async Function CallAsync(path As String) As Task(Of BeltStatus)
        Dim url = "http://" & Host & path

        Dim body As String
        Using response = Await _http.GetAsync(url)
            body = Await response.Content.ReadAsStringAsync()

            If String.IsNullOrWhiteSpace(body) Then
                Throw New FoerderbandException("Leere Antwort (HTTP " & CInt(response.StatusCode) & ")")
            End If
        End Using

        Dim map As Dictionary(Of String, Object)
        Try
            map = _serializer.Deserialize(Of Dictionary(Of String, Object))(body)
        Catch
            Throw New FoerderbandException("Unerwartete Antwort - laeuft auf dem Geraet Firmware v1.3.0 oder neuer?")
        End Try

        If map Is Nothing Then
            Throw New FoerderbandException("Unerwartete Antwort - laeuft auf dem Geraet Firmware v1.3.0 oder neuer?")
        End If

        If Not Flag(map, "ok") Then
            Throw New FoerderbandException(Str(map, "error", "Unbekannter Fehler"))
        End If

        Return ToStatus(map)
    End Function

    Private Shared Function ToStatus(m As Dictionary(Of String, Object)) As BeltStatus
        Dim s As New BeltStatus()
        s.Running = Flag(m, "running")
        s.Sensor = Flag(m, "sensor")
        s.StepsTotal = Lng(m, "steps_total")
        s.StepsDone = Lng(m, "steps_done")
        s.StepsRemaining = Lng(m, "steps_remaining")
        s.MmTotal = Dbl(m, "mm_total")
        s.MmDone = Dbl(m, "mm_done")
        s.StepsPerMm = Dbl(m, "steps_per_mm")
        s.StepsPerRev = CInt(Lng(m, "steps_per_rev"))
        s.DelayUs = CInt(Lng(m, "delay_us"))
        s.Umdrehungen = CInt(Lng(m, "umdrehungen"))
        s.WalzeMm = Dbl(m, "walze_mm")
        s.Version = Str(m, "version", "?")
        s.Ip = Str(m, "ip", "")
        s.Rssi = CInt(Lng(m, "rssi"))
        Return s
    End Function

    Private Shared Function Flag(m As Dictionary(Of String, Object), key As String) As Boolean
        Dim v As Object = Nothing
        If Not m.TryGetValue(key, v) OrElse v Is Nothing Then Return False
        Return TypeOf v Is Boolean AndAlso CBool(v)
    End Function

    Private Shared Function Dbl(m As Dictionary(Of String, Object), key As String) As Double
        Dim v As Object = Nothing
        If Not m.TryGetValue(key, v) OrElse v Is Nothing Then Return 0
        Return Convert.ToDouble(v, CultureInfo.InvariantCulture)
    End Function

    Private Shared Function Lng(m As Dictionary(Of String, Object), key As String) As Long
        Dim v As Object = Nothing
        If Not m.TryGetValue(key, v) OrElse v Is Nothing Then Return 0
        Return Convert.ToInt64(Convert.ToDouble(v, CultureInfo.InvariantCulture))
    End Function

    Private Shared Function Str(m As Dictionary(Of String, Object), key As String,
                                fallback As String) As String
        Dim v As Object = Nothing
        If Not m.TryGetValue(key, v) OrElse v Is Nothing Then Return fallback
        Return Convert.ToString(v)
    End Function

    ''' <summary> Zahl fuer die URL - immer mit Punkt, unabhaengig von der Windows-Sprache. </summary>
    Private Shared Function Num(value As Double) As String
        Return value.ToString("0.###", CultureInfo.InvariantCulture)
    End Function

End Class

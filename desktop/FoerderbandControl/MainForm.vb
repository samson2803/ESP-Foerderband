Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms

''' <summary>
''' Steuerfenster fuer das Foerderband. Redet ausschliesslich ueber die JSON-API
''' der Firmware ab v1.3.0 mit dem Geraet.
''' </summary>
Public Class MainForm

    ' Waehrend einer Fahrt haeufiger nachfragen - eine Fahrt dauert je nach
    ' Strecke nur wenige Sekunden, bei 2s-Takt saehe man den Balken kaum.
    Private Const PollRunMs As Integer = 300
    Private Const PollIdleMs As Integer = 2000

    ' So viele Fehlversuche in Folge, bevor die Verbindung als verloren gilt.
    Private Const MaxPollFailures As Integer = 3

    ' Ein FlatStyle-Button mit fester BackColor wird von Windows im gesperrten Zustand
    ' nicht abgeblendet - die Farben muessen wir deshalb selbst umschalten.
    Private Shared ReadOnly RunColor As Color = Color.FromArgb(42, 125, 225)
    Private Shared ReadOnly StopColor As Color = Color.FromArgb(200, 60, 60)
    Private Shared ReadOnly DeadColor As Color = Color.FromArgb(202, 206, 212)
    Private Shared ReadOnly DeadText As Color = Color.FromArgb(120, 125, 132)

    Private _api As FoerderbandApi
    Private _lastStatus As BeltStatus

    Private _polling As Boolean      ' laeuft gerade eine Statusabfrage?
    Private _busy As Boolean         ' laeuft gerade ein Befehl (Fahren/Stop/Speichern)?
    Private _settingsDirty As Boolean ' Nutzer hat Einstellungen veraendert, aber nicht gespeichert
    Private _suppressEvents As Boolean
    Private _failCount As Integer

    ' ---------- Start / Ende ----------

    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        _suppressEvents = True
        cboUnit.Items.AddRange(New Object() {"cm", "mm"})
        cboUnit.SelectedIndex = 0
        txtHost.Text = LoadHost()
        _suppressEvents = False

        SetDisconnected(Nothing)
    End Sub

    Private Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        pollTimer.Stop()
        SaveHost(txtHost.Text.Trim())
    End Sub

    ' ---------- Verbindung ----------

    Private Async Sub btnConnect_Click(sender As Object, e As EventArgs) Handles btnConnect.Click
        Dim host = txtHost.Text.Trim()
        If host.Length = 0 Then
            Say("Bitte Hostnamen oder IP eintragen.", True)
            Return
        End If

        pollTimer.Stop()
        btnConnect.Enabled = False
        Say("Verbinde mit " & host & " ...")

        Try
            _api = New FoerderbandApi(host)
            Dim st = Await _api.GetStatusAsync()

            _failCount = 0
            _settingsDirty = False
            ApplyStatus(st)
            SetConnected(True)

            pollTimer.Interval = If(st.Running, PollRunMs, PollIdleMs)
            pollTimer.Start()
            Say("Verbunden mit " & host & ".")
        Catch ex As Exception
            SetDisconnected("Keine Verbindung zu " & host & ": " & ex.Message)
        Finally
            btnConnect.Enabled = True
        End Try
    End Sub

    Private Sub SetConnected(value As Boolean)
        grpDrive.Enabled = value
        grpSettings.Enabled = value
        btnConnect.Text = If(value, "Neu verbinden", "Verbinden")

        If Not value Then
            SetAction(btnRun, False, RunColor)
            SetAction(btnStop, False, StopColor)
        End If
    End Sub

    ''' <summary> Setzt Freigabe und Farbe eines Aktionsknopfes gemeinsam. </summary>
    Private Shared Sub SetAction(button As Button, enabled As Boolean, active As Color)
        button.Enabled = enabled
        button.BackColor = If(enabled, active, DeadColor)
        button.ForeColor = If(enabled, Color.White, DeadText)
    End Sub

    Private Sub SetDisconnected(reason As String)
        pollTimer.Stop()
        _lastStatus = Nothing
        _failCount = 0
        SetConnected(False)

        lblConnState.Text = "nicht verbunden"
        lblConnState.ForeColor = Color.Gray
        lblCalc.Text = "—"
        lblProgress.Text = "—"
        lblGeometry.Text = "—"
        prgRun.Value = 0

        If reason IsNot Nothing Then Say(reason, True)
    End Sub

    ' ---------- Statusabfrage ----------

    Private Async Sub pollTimer_Tick(sender As Object, e As EventArgs) Handles pollTimer.Tick
        ' Ueberlappende Abfragen vermeiden: bei 300ms-Takt und langsamem WLAN
        ' wuerden sich die Anfragen sonst stapeln.
        If _polling OrElse _busy Then Return

        _polling = True
        Try
            Dim st = Await _api.GetStatusAsync()
            _failCount = 0
            ApplyStatus(st)
        Catch ex As Exception
            _failCount += 1
            If _failCount >= MaxPollFailures Then
                SetDisconnected("Verbindung verloren: " & ex.Message)
            End If
        Finally
            _polling = False
        End Try
    End Sub

    ''' <summary> Uebertraegt einen Geraetezustand in die Oberflaeche. </summary>
    Private Sub ApplyStatus(st As BeltStatus)
        _lastStatus = st

        Dim sensor = If(st.Sensor, "  ·  Lichtschranke ausgelöst", "")
        lblConnState.Text = String.Format("verbunden  ·  Firmware v{0}  ·  {1}  ·  {2} dBm{3}",
                                          st.Version, st.Ip, st.Rssi, sensor)
        lblConnState.ForeColor = Color.FromArgb(0, 130, 60)

        ' Nicht ueber Eingaben schreiben, an denen der Nutzer gerade arbeitet.
        If Not _settingsDirty Then
            _suppressEvents = True
            numDelay.Value = Fit(st.DelayUs, numDelay)
            numWalze.Value = Fit(CDec(st.WalzeMm), numWalze)
            numUmdr.Value = Fit(st.Umdrehungen, numUmdr)
            _suppressEvents = False
        End If

        lblGeometry.Text = String.Format("{0:N2} Schritte/mm  ·  {1:N1} mm Bandweg je Umdrehung",
                                        st.StepsPerMm, st.MmProUmdrehung)

        UpdateProgress(st)
        UpdateCalc()

        ' Fahren sperren, solange das Band laeuft; Stop nur dann freigeben.
        SetAction(btnRun, Not st.Running, RunColor)
        SetAction(btnStop, st.Running, StopColor)
        btnQ10.Enabled = Not st.Running
        btnQ20.Enabled = Not st.Running
        btnQ30.Enabled = Not st.Running
        btnQ50.Enabled = Not st.Running

        Dim wanted = If(st.Running, PollRunMs, PollIdleMs)
        If pollTimer.Interval <> wanted Then pollTimer.Interval = wanted
    End Sub

    Private Sub UpdateProgress(st As BeltStatus)
        prgRun.Maximum = 1000

        If st.StepsTotal <= 0 Then
            prgRun.Value = 0
            lblProgress.Text = "bereit"
            Return
        End If

        Dim frac = st.StepsDone / CDbl(st.StepsTotal)
        prgRun.Value = Math.Max(0, Math.Min(1000, CInt(Math.Round(frac * 1000))))

        If st.Running Then
            lblProgress.Text = String.Format("läuft  —  {0:N1} von {1:N1} cm  ({2:N0} %)",
                                             st.MmDone / 10.0, st.MmTotal / 10.0, frac * 100.0)
        ElseIf st.StepsRemaining > 0 Then
            ' stepsRemaining ueberlebt einen Stop - daran haengt diese Unterscheidung.
            lblProgress.Text = String.Format("abgebrochen bei {0:N1} von {1:N1} cm",
                                             st.MmDone / 10.0, st.MmTotal / 10.0)
        Else
            lblProgress.Text = String.Format("fertig  —  {0:N1} cm gefahren", st.MmTotal / 10.0)
        End If
    End Sub

    ' ---------- Fahren ----------

    Private Async Sub btnRun_Click(sender As Object, e As EventArgs) Handles btnRun.Click
        Await RunAsync(DistanceMm())
    End Sub

    Private Async Sub QuickButton_Click(sender As Object, e As EventArgs) _
            Handles btnQ10.Click, btnQ20.Click, btnQ30.Click, btnQ50.Click

        Dim cm As Double
        If sender Is btnQ10 Then
            cm = 10
        ElseIf sender Is btnQ20 Then
            cm = 20
        ElseIf sender Is btnQ30 Then
            cm = 30
        Else
            cm = 50
        End If

        ' Eingabefeld mitziehen, damit sichtbar ist, was gefahren wird.
        _suppressEvents = True
        cboUnit.SelectedIndex = 0
        numDistance.Value = Fit(CDec(cm), numDistance)
        _suppressEvents = False

        Await RunAsync(cm * 10.0)
    End Sub

    Private Async Function RunAsync(mm As Double) As Task
        If _api Is Nothing OrElse mm <= 0 Then Return

        _busy = True
        Try
            Say(String.Format("Fahre {0:N1} cm ...", mm / 10.0))
            Dim st = Await _api.RunMillimetersAsync(mm)
            ApplyStatus(st)
            pollTimer.Interval = PollRunMs
        Catch ex As FoerderbandException
            Say("Gerät meldet: " & ex.Message, True)
        Catch ex As Exception
            Say("Fehler beim Fahren: " & ex.Message, True)
        Finally
            _busy = False
        End Try
    End Function

    Private Async Sub btnStop_Click(sender As Object, e As EventArgs) Handles btnStop.Click
        If _api Is Nothing Then Return

        _busy = True
        Try
            Dim st = Await _api.StopAsync()
            ApplyStatus(st)
            Say("Gestoppt.")
        Catch ex As Exception
            Say("Stop fehlgeschlagen: " & ex.Message, True)
        Finally
            _busy = False
        End Try
    End Sub

    ' ---------- Einstellungen ----------

    Private Sub Settings_Changed(sender As Object, e As EventArgs) _
            Handles numDelay.ValueChanged, numWalze.ValueChanged, numUmdr.ValueChanged

        If _suppressEvents Then Return
        _settingsDirty = True
        btnSaveSettings.Text = "Einstellungen auf dem Gerät speichern *"
    End Sub

    Private Async Sub btnSaveSettings_Click(sender As Object, e As EventArgs) Handles btnSaveSettings.Click
        If _api Is Nothing Then Return

        _busy = True
        btnSaveSettings.Enabled = False
        Try
            Dim st = Await _api.SaveConfigAsync(CInt(numDelay.Value),
                                                CInt(numUmdr.Value),
                                                CDbl(numWalze.Value))
            _settingsDirty = False
            btnSaveSettings.Text = "Einstellungen auf dem Gerät speichern"
            ApplyStatus(st)
            Say("Einstellungen gespeichert (im EEPROM des ESP).")
        Catch ex As FoerderbandException
            Say("Gerät meldet: " & ex.Message, True)
        Catch ex As Exception
            Say("Speichern fehlgeschlagen: " & ex.Message, True)
        Finally
            btnSaveSettings.Enabled = True
            _busy = False
        End Try
    End Sub

    ' ---------- Eingabe / Rechnerei ----------

    Private Sub Distance_Changed(sender As Object, e As EventArgs) _
            Handles numDistance.ValueChanged, cboUnit.SelectedIndexChanged

        If _suppressEvents Then Return
        UpdateCalc()
    End Sub

    Private Function DistanceMm() As Double
        Dim v = CDbl(numDistance.Value)
        Return If(cboUnit.SelectedIndex = 0, v * 10.0, v)
    End Function

    ''' <summary> Zeigt vorab, was der Auftrag am Gerät bedeutet. </summary>
    Private Sub UpdateCalc()
        If _lastStatus Is Nothing OrElse _lastStatus.StepsPerMm <= 0 Then
            lblCalc.Text = "—"
            Return
        End If

        Dim steps = CLng(Math.Round(DistanceMm() * _lastStatus.StepsPerMm))
        ' Je Schritt zweimal delayUs: einmal HIGH, einmal LOW.
        Dim seconds = steps * 2.0 * _lastStatus.DelayUs / 1000000.0
        lblCalc.Text = String.Format("= {0:N0} Schritte  ·  ca. {1:N1} s", steps, seconds)
    End Sub

    ''' <summary> Haelt einen Wert in den Grenzen des Eingabefelds. </summary>
    Private Shared Function Fit(value As Decimal, box As NumericUpDown) As Decimal
        If value < box.Minimum Then Return box.Minimum
        If value > box.Maximum Then Return box.Maximum
        Return value
    End Function

    ' ---------- Kleinkram ----------

    Private Sub Say(text As String, Optional isError As Boolean = False)
        lblStatus.Text = text
        lblStatus.ForeColor = If(isError, Color.Firebrick, SystemColors.ControlText)
    End Sub

    ''' <summary> Zuletzt benutzter Host, damit man ihn nicht jedes Mal tippt. </summary>
    Private Shared ReadOnly Property HostFile As String
        Get
            Return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FoerderbandControl", "host.txt")
        End Get
    End Property

    Private Shared Function LoadHost() As String
        Try
            If File.Exists(HostFile) Then
                Dim s = File.ReadAllText(HostFile).Trim()
                If s.Length > 0 Then Return s
            End If
        Catch
            ' Gespeicherter Host ist Komfort, kein Muss - Fehler hier sind egal.
        End Try
        Return "foerderband.local"
    End Function

    Private Shared Sub SaveHost(host As String)
        Try
            If host.Length = 0 Then Return
            Directory.CreateDirectory(Path.GetDirectoryName(HostFile))
            File.WriteAllText(HostFile, host)
        Catch
        End Try
    End Sub

End Class

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class MainForm
    Inherits System.Windows.Forms.Form

    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer

    Friend WithEvents grpConnection As System.Windows.Forms.GroupBox
    Friend WithEvents lblHostCaption As System.Windows.Forms.Label
    Friend WithEvents txtHost As System.Windows.Forms.TextBox
    Friend WithEvents btnConnect As System.Windows.Forms.Button
    Friend WithEvents lblConnState As System.Windows.Forms.Label

    Friend WithEvents grpDrive As System.Windows.Forms.GroupBox
    Friend WithEvents lblDistCaption As System.Windows.Forms.Label
    Friend WithEvents numDistance As System.Windows.Forms.NumericUpDown
    Friend WithEvents cboUnit As System.Windows.Forms.ComboBox
    Friend WithEvents lblCalc As System.Windows.Forms.Label
    Friend WithEvents btnQ10 As System.Windows.Forms.Button
    Friend WithEvents btnQ20 As System.Windows.Forms.Button
    Friend WithEvents btnQ30 As System.Windows.Forms.Button
    Friend WithEvents btnQ50 As System.Windows.Forms.Button
    Friend WithEvents btnRun As System.Windows.Forms.Button
    Friend WithEvents btnStop As System.Windows.Forms.Button
    Friend WithEvents prgRun As System.Windows.Forms.ProgressBar
    Friend WithEvents lblProgress As System.Windows.Forms.Label

    Friend WithEvents grpSettings As System.Windows.Forms.GroupBox
    Friend WithEvents lblDelay As System.Windows.Forms.Label
    Friend WithEvents numDelay As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblWalze As System.Windows.Forms.Label
    Friend WithEvents numWalze As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblUmdr As System.Windows.Forms.Label
    Friend WithEvents numUmdr As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblGeometry As System.Windows.Forms.Label
    Friend WithEvents btnSaveSettings As System.Windows.Forms.Button

    Friend WithEvents statusStrip As System.Windows.Forms.StatusStrip
    Friend WithEvents lblStatus As System.Windows.Forms.ToolStripStatusLabel
    Friend WithEvents pollTimer As System.Windows.Forms.Timer

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.grpConnection = New System.Windows.Forms.GroupBox()
        Me.lblHostCaption = New System.Windows.Forms.Label()
        Me.txtHost = New System.Windows.Forms.TextBox()
        Me.btnConnect = New System.Windows.Forms.Button()
        Me.lblConnState = New System.Windows.Forms.Label()
        Me.grpDrive = New System.Windows.Forms.GroupBox()
        Me.lblDistCaption = New System.Windows.Forms.Label()
        Me.numDistance = New System.Windows.Forms.NumericUpDown()
        Me.cboUnit = New System.Windows.Forms.ComboBox()
        Me.lblCalc = New System.Windows.Forms.Label()
        Me.btnQ10 = New System.Windows.Forms.Button()
        Me.btnQ20 = New System.Windows.Forms.Button()
        Me.btnQ30 = New System.Windows.Forms.Button()
        Me.btnQ50 = New System.Windows.Forms.Button()
        Me.btnRun = New System.Windows.Forms.Button()
        Me.btnStop = New System.Windows.Forms.Button()
        Me.prgRun = New System.Windows.Forms.ProgressBar()
        Me.lblProgress = New System.Windows.Forms.Label()
        Me.grpSettings = New System.Windows.Forms.GroupBox()
        Me.lblDelay = New System.Windows.Forms.Label()
        Me.numDelay = New System.Windows.Forms.NumericUpDown()
        Me.lblWalze = New System.Windows.Forms.Label()
        Me.numWalze = New System.Windows.Forms.NumericUpDown()
        Me.lblUmdr = New System.Windows.Forms.Label()
        Me.numUmdr = New System.Windows.Forms.NumericUpDown()
        Me.lblGeometry = New System.Windows.Forms.Label()
        Me.btnSaveSettings = New System.Windows.Forms.Button()
        Me.statusStrip = New System.Windows.Forms.StatusStrip()
        Me.lblStatus = New System.Windows.Forms.ToolStripStatusLabel()
        Me.pollTimer = New System.Windows.Forms.Timer(Me.components)
        Me.grpConnection.SuspendLayout()
        Me.grpDrive.SuspendLayout()
        CType(Me.numDistance, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.grpSettings.SuspendLayout()
        CType(Me.numDelay, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.numWalze, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.numUmdr, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.statusStrip.SuspendLayout()
        Me.SuspendLayout()
        '
        'grpConnection
        '
        Me.grpConnection.Controls.Add(Me.lblHostCaption)
        Me.grpConnection.Controls.Add(Me.txtHost)
        Me.grpConnection.Controls.Add(Me.btnConnect)
        Me.grpConnection.Controls.Add(Me.lblConnState)
        Me.grpConnection.Location = New System.Drawing.Point(12, 12)
        Me.grpConnection.Name = "grpConnection"
        Me.grpConnection.Size = New System.Drawing.Size(430, 86)
        Me.grpConnection.TabIndex = 0
        Me.grpConnection.TabStop = False
        Me.grpConnection.Text = "Verbindung"
        '
        'lblHostCaption
        '
        Me.lblHostCaption.AutoSize = True
        Me.lblHostCaption.Location = New System.Drawing.Point(12, 29)
        Me.lblHostCaption.Name = "lblHostCaption"
        Me.lblHostCaption.Size = New System.Drawing.Size(41, 15)
        Me.lblHostCaption.TabIndex = 0
        Me.lblHostCaption.Text = "Gerät:"
        '
        'txtHost
        '
        Me.txtHost.Location = New System.Drawing.Point(68, 26)
        Me.txtHost.Name = "txtHost"
        Me.txtHost.Size = New System.Drawing.Size(216, 23)
        Me.txtHost.TabIndex = 1
        Me.txtHost.Text = "foerderband.local"
        '
        'btnConnect
        '
        Me.btnConnect.Location = New System.Drawing.Point(292, 25)
        Me.btnConnect.Name = "btnConnect"
        Me.btnConnect.Size = New System.Drawing.Size(126, 25)
        Me.btnConnect.TabIndex = 2
        Me.btnConnect.Text = "Verbinden"
        Me.btnConnect.UseVisualStyleBackColor = True
        '
        'lblConnState
        '
        Me.lblConnState.AutoEllipsis = True
        Me.lblConnState.ForeColor = System.Drawing.Color.Gray
        Me.lblConnState.Location = New System.Drawing.Point(12, 59)
        Me.lblConnState.Name = "lblConnState"
        Me.lblConnState.Size = New System.Drawing.Size(406, 15)
        Me.lblConnState.TabIndex = 3
        Me.lblConnState.Text = "nicht verbunden"
        '
        'grpDrive
        '
        Me.grpDrive.Controls.Add(Me.lblDistCaption)
        Me.grpDrive.Controls.Add(Me.numDistance)
        Me.grpDrive.Controls.Add(Me.cboUnit)
        Me.grpDrive.Controls.Add(Me.lblCalc)
        Me.grpDrive.Controls.Add(Me.btnQ10)
        Me.grpDrive.Controls.Add(Me.btnQ20)
        Me.grpDrive.Controls.Add(Me.btnQ30)
        Me.grpDrive.Controls.Add(Me.btnQ50)
        Me.grpDrive.Controls.Add(Me.btnRun)
        Me.grpDrive.Controls.Add(Me.btnStop)
        Me.grpDrive.Controls.Add(Me.prgRun)
        Me.grpDrive.Controls.Add(Me.lblProgress)
        Me.grpDrive.Location = New System.Drawing.Point(12, 104)
        Me.grpDrive.Name = "grpDrive"
        Me.grpDrive.Size = New System.Drawing.Size(430, 208)
        Me.grpDrive.TabIndex = 1
        Me.grpDrive.TabStop = False
        Me.grpDrive.Text = "Fahren"
        '
        'lblDistCaption
        '
        Me.lblDistCaption.AutoSize = True
        Me.lblDistCaption.Location = New System.Drawing.Point(12, 30)
        Me.lblDistCaption.Name = "lblDistCaption"
        Me.lblDistCaption.Size = New System.Drawing.Size(48, 15)
        Me.lblDistCaption.TabIndex = 0
        Me.lblDistCaption.Text = "Strecke:"
        '
        'numDistance
        '
        Me.numDistance.DecimalPlaces = 1
        Me.numDistance.Location = New System.Drawing.Point(68, 27)
        Me.numDistance.Maximum = New Decimal(New Integer() {5000, 0, 0, 0})
        Me.numDistance.Minimum = New Decimal(New Integer() {1, 0, 0, 65536})
        Me.numDistance.Name = "numDistance"
        Me.numDistance.Size = New System.Drawing.Size(88, 23)
        Me.numDistance.TabIndex = 1
        Me.numDistance.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        Me.numDistance.Value = New Decimal(New Integer() {30, 0, 0, 0})
        '
        'cboUnit
        '
        Me.cboUnit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboUnit.Location = New System.Drawing.Point(162, 27)
        Me.cboUnit.Name = "cboUnit"
        Me.cboUnit.Size = New System.Drawing.Size(56, 23)
        Me.cboUnit.TabIndex = 2
        '
        'lblCalc
        '
        Me.lblCalc.AutoEllipsis = True
        Me.lblCalc.ForeColor = System.Drawing.Color.Gray
        Me.lblCalc.Location = New System.Drawing.Point(228, 30)
        Me.lblCalc.Name = "lblCalc"
        Me.lblCalc.Size = New System.Drawing.Size(190, 15)
        Me.lblCalc.TabIndex = 3
        Me.lblCalc.Text = "—"
        '
        'btnQ10
        '
        Me.btnQ10.Location = New System.Drawing.Point(12, 58)
        Me.btnQ10.Name = "btnQ10"
        Me.btnQ10.Size = New System.Drawing.Size(96, 26)
        Me.btnQ10.TabIndex = 4
        Me.btnQ10.Text = "10 cm"
        Me.btnQ10.UseVisualStyleBackColor = True
        '
        'btnQ20
        '
        Me.btnQ20.Location = New System.Drawing.Point(114, 58)
        Me.btnQ20.Name = "btnQ20"
        Me.btnQ20.Size = New System.Drawing.Size(96, 26)
        Me.btnQ20.TabIndex = 5
        Me.btnQ20.Text = "20 cm"
        Me.btnQ20.UseVisualStyleBackColor = True
        '
        'btnQ30
        '
        Me.btnQ30.Location = New System.Drawing.Point(216, 58)
        Me.btnQ30.Name = "btnQ30"
        Me.btnQ30.Size = New System.Drawing.Size(96, 26)
        Me.btnQ30.TabIndex = 6
        Me.btnQ30.Text = "30 cm"
        Me.btnQ30.UseVisualStyleBackColor = True
        '
        'btnQ50
        '
        Me.btnQ50.Location = New System.Drawing.Point(318, 58)
        Me.btnQ50.Name = "btnQ50"
        Me.btnQ50.Size = New System.Drawing.Size(96, 26)
        Me.btnQ50.TabIndex = 7
        Me.btnQ50.Text = "50 cm"
        Me.btnQ50.UseVisualStyleBackColor = True
        '
        'btnRun
        '
        Me.btnRun.BackColor = System.Drawing.Color.FromArgb(CType(CType(42, Byte), Integer), CType(CType(125, Byte), Integer), CType(CType(225, Byte), Integer))
        Me.btnRun.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnRun.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.btnRun.ForeColor = System.Drawing.Color.White
        Me.btnRun.Location = New System.Drawing.Point(12, 94)
        Me.btnRun.Name = "btnRun"
        Me.btnRun.Size = New System.Drawing.Size(300, 46)
        Me.btnRun.TabIndex = 8
        Me.btnRun.Text = "Fahren"
        Me.btnRun.UseVisualStyleBackColor = False
        '
        'btnStop
        '
        Me.btnStop.BackColor = System.Drawing.Color.FromArgb(CType(CType(200, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer))
        Me.btnStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnStop.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.btnStop.ForeColor = System.Drawing.Color.White
        Me.btnStop.Location = New System.Drawing.Point(318, 94)
        Me.btnStop.Name = "btnStop"
        Me.btnStop.Size = New System.Drawing.Size(96, 46)
        Me.btnStop.TabIndex = 9
        Me.btnStop.Text = "STOP"
        Me.btnStop.UseVisualStyleBackColor = False
        '
        'prgRun
        '
        Me.prgRun.Location = New System.Drawing.Point(12, 152)
        Me.prgRun.Name = "prgRun"
        Me.prgRun.Size = New System.Drawing.Size(402, 18)
        Me.prgRun.TabIndex = 10
        '
        'lblProgress
        '
        Me.lblProgress.AutoEllipsis = True
        Me.lblProgress.ForeColor = System.Drawing.Color.Gray
        Me.lblProgress.Location = New System.Drawing.Point(12, 178)
        Me.lblProgress.Name = "lblProgress"
        Me.lblProgress.Size = New System.Drawing.Size(402, 15)
        Me.lblProgress.TabIndex = 11
        Me.lblProgress.Text = "—"
        '
        'grpSettings
        '
        Me.grpSettings.Controls.Add(Me.lblDelay)
        Me.grpSettings.Controls.Add(Me.numDelay)
        Me.grpSettings.Controls.Add(Me.lblWalze)
        Me.grpSettings.Controls.Add(Me.numWalze)
        Me.grpSettings.Controls.Add(Me.lblUmdr)
        Me.grpSettings.Controls.Add(Me.numUmdr)
        Me.grpSettings.Controls.Add(Me.lblGeometry)
        Me.grpSettings.Controls.Add(Me.btnSaveSettings)
        Me.grpSettings.Location = New System.Drawing.Point(12, 318)
        Me.grpSettings.Name = "grpSettings"
        Me.grpSettings.Size = New System.Drawing.Size(430, 178)
        Me.grpSettings.TabIndex = 2
        Me.grpSettings.TabStop = False
        Me.grpSettings.Text = "Einstellungen"
        '
        'lblDelay
        '
        Me.lblDelay.AutoSize = True
        Me.lblDelay.Location = New System.Drawing.Point(12, 28)
        Me.lblDelay.Name = "lblDelay"
        Me.lblDelay.Size = New System.Drawing.Size(216, 15)
        Me.lblDelay.TabIndex = 0
        Me.lblDelay.Text = "Tempo (Delay in µs, kleiner = schneller):"
        '
        'numDelay
        '
        Me.numDelay.Increment = New Decimal(New Integer() {50, 0, 0, 0})
        Me.numDelay.Location = New System.Drawing.Point(300, 25)
        Me.numDelay.Maximum = New Decimal(New Integer() {5000, 0, 0, 0})
        Me.numDelay.Minimum = New Decimal(New Integer() {100, 0, 0, 0})
        Me.numDelay.Name = "numDelay"
        Me.numDelay.Size = New System.Drawing.Size(114, 23)
        Me.numDelay.TabIndex = 1
        Me.numDelay.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        Me.numDelay.Value = New Decimal(New Integer() {800, 0, 0, 0})
        '
        'lblWalze
        '
        Me.lblWalze.AutoSize = True
        Me.lblWalze.Location = New System.Drawing.Point(12, 58)
        Me.lblWalze.Name = "lblWalze"
        Me.lblWalze.Size = New System.Drawing.Size(205, 15)
        Me.lblWalze.TabIndex = 2
        Me.lblWalze.Text = "Walzendurchmesser inkl. Band (mm):"
        '
        'numWalze
        '
        Me.numWalze.DecimalPlaces = 1
        Me.numWalze.Increment = New Decimal(New Integer() {1, 0, 0, 65536})
        Me.numWalze.Location = New System.Drawing.Point(300, 55)
        Me.numWalze.Maximum = New Decimal(New Integer() {200, 0, 0, 0})
        Me.numWalze.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.numWalze.Name = "numWalze"
        Me.numWalze.Size = New System.Drawing.Size(114, 23)
        Me.numWalze.TabIndex = 3
        Me.numWalze.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        Me.numWalze.Value = New Decimal(New Integer() {335, 0, 0, 65536})
        '
        'lblUmdr
        '
        Me.lblUmdr.AutoSize = True
        Me.lblUmdr.Location = New System.Drawing.Point(12, 88)
        Me.lblUmdr.Name = "lblUmdr"
        Me.lblUmdr.Size = New System.Drawing.Size(232, 15)
        Me.lblUmdr.TabIndex = 4
        Me.lblUmdr.Text = "Umdrehungen pro Lichtschranken-Auslösung:"
        '
        'numUmdr
        '
        Me.numUmdr.Location = New System.Drawing.Point(300, 85)
        Me.numUmdr.Maximum = New Decimal(New Integer() {100, 0, 0, 0})
        Me.numUmdr.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.numUmdr.Name = "numUmdr"
        Me.numUmdr.Size = New System.Drawing.Size(114, 23)
        Me.numUmdr.TabIndex = 5
        Me.numUmdr.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        Me.numUmdr.Value = New Decimal(New Integer() {10, 0, 0, 0})
        '
        'lblGeometry
        '
        Me.lblGeometry.AutoEllipsis = True
        Me.lblGeometry.ForeColor = System.Drawing.Color.Gray
        Me.lblGeometry.Location = New System.Drawing.Point(12, 116)
        Me.lblGeometry.Name = "lblGeometry"
        Me.lblGeometry.Size = New System.Drawing.Size(406, 15)
        Me.lblGeometry.TabIndex = 6
        Me.lblGeometry.Text = "—"
        '
        'btnSaveSettings
        '
        Me.btnSaveSettings.Location = New System.Drawing.Point(12, 140)
        Me.btnSaveSettings.Name = "btnSaveSettings"
        Me.btnSaveSettings.Size = New System.Drawing.Size(402, 28)
        Me.btnSaveSettings.TabIndex = 7
        Me.btnSaveSettings.Text = "Einstellungen auf dem Gerät speichern"
        Me.btnSaveSettings.UseVisualStyleBackColor = True
        '
        'statusStrip
        '
        Me.statusStrip.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.lblStatus})
        Me.statusStrip.Location = New System.Drawing.Point(0, 506)
        Me.statusStrip.Name = "statusStrip"
        Me.statusStrip.Size = New System.Drawing.Size(454, 22)
        Me.statusStrip.SizingGrip = False
        Me.statusStrip.TabIndex = 3
        '
        'lblStatus
        '
        Me.lblStatus.Name = "lblStatus"
        Me.lblStatus.Size = New System.Drawing.Size(39, 17)
        Me.lblStatus.Text = "Bereit"
        '
        'pollTimer
        '
        Me.pollTimer.Interval = 2000
        '
        'MainForm
        '
        Me.AcceptButton = Me.btnRun
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(454, 528)
        Me.Controls.Add(Me.grpConnection)
        Me.Controls.Add(Me.grpDrive)
        Me.Controls.Add(Me.grpSettings)
        Me.Controls.Add(Me.statusStrip)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.Name = "MainForm"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Förderband-Steuerung"
        Me.grpConnection.ResumeLayout(False)
        Me.grpConnection.PerformLayout()
        Me.grpDrive.ResumeLayout(False)
        Me.grpDrive.PerformLayout()
        CType(Me.numDistance, System.ComponentModel.ISupportInitialize).EndInit()
        Me.grpSettings.ResumeLayout(False)
        Me.grpSettings.PerformLayout()
        CType(Me.numDelay, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.numWalze, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.numUmdr, System.ComponentModel.ISupportInitialize).EndInit()
        Me.statusStrip.ResumeLayout(False)
        Me.statusStrip.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub
End Class

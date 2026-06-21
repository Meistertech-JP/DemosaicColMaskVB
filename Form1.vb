Imports System.IO
Imports System.Threading.Tasks
Imports System.Windows.Forms

Public Class Form1
    Private processor As New AssetProcessor()
    Private allShaders As New List(Of AssetProcessor.ShaderInfo)()
    Private apkMap As New Dictionary(Of String, String)()
    Private apkSessionTempDir As String = Nothing

    Private WithEvents txtPath As TextBox
    Private WithEvents btnScanAll As Button
    Private WithEvents chkBackup As CheckBox
    Private WithEvents btnStart As Button
    Private WithEvents txtFilter As TextBox
    Private WithEvents lvAvailable As ListView
    Private WithEvents lvSelected As ListView
    Private WithEvents txtLog As TextBox
    Private progressBar As ProgressBar
    Private ctxSelected As ContextMenuStrip
    Private splitMain As SplitContainer
    Private splitCenter As SplitContainer

    Private Loc As New Localization()
    Private WithEvents cmbLang As ComboBox
    Private replacementMap As New Dictionary(Of String, AssetProcessor.ShaderInfo)()

    Public Sub New()
        InitializeComponent()
        processor.Loc = Loc
        SetupUI()
        ApplyLanguage()
        AddHandler processor.LogMessage, AddressOf OnLogMessage
        AddHandler processor.ProgressUpdate, AddressOf OnProgressUpdate
    End Sub

    Private Sub SetupUI()
        Me.Size = New Drawing.Size(800, 600)

        ' Top Panel
        Dim pnlTop As New Panel() With {.Dock = DockStyle.Top, .Height = 40}
        Me.Controls.Add(pnlTop)

        Dim lblPath As New Label() With {.Name = "lblPath", .Left = 10, .Top = 12, .Width = 40}
        pnlTop.Controls.Add(lblPath)

        txtPath = New TextBox() With {.Left = 50, .Top = 10, .Width = 260, .AllowDrop = True}
        pnlTop.Controls.Add(txtPath)

        btnScanAll = New Button() With {.Left = 320, .Top = 8, .Width = 80}
        pnlTop.Controls.Add(btnScanAll)

        chkBackup = New CheckBox() With {.Left = 410, .Top = 12, .Width = 90, .Checked = True}
        pnlTop.Controls.Add(chkBackup)

        btnStart = New Button() With {.Left = 510, .Top = 8, .Width = 110}
        pnlTop.Controls.Add(btnStart)

        cmbLang = New ComboBox() With {.Left = 630, .Top = 9, .Width = 120, .DropDownStyle = ComboBoxStyle.DropDownList}
        cmbLang.Items.Add(New With {.Text = "English", .Value = "en"})
        cmbLang.Items.Add(New With {.Text = "日本語", .Value = "ja"})
        cmbLang.Items.Add(New With {.Text = "繁體中文", .Value = "zh-Hant"})
        cmbLang.Items.Add(New With {.Text = "简体中文", .Value = "zh-Hans"})
        cmbLang.Items.Add(New With {.Text = "한국어", .Value = "ko"})
        cmbLang.DisplayMember = "Text"
        cmbLang.ValueMember = "Value"
        
        ' Select current lang
        For i As Integer = 0 To cmbLang.Items.Count - 1
            If CStr(cmbLang.Items(i).Value) = Loc.CurrentLang Then
                cmbLang.SelectedIndex = i
                Exit For
            End If
        Next
        pnlTop.Controls.Add(cmbLang)

        ' Main Split Container
        splitMain = New SplitContainer() With {.Dock = DockStyle.Fill, .Orientation = Orientation.Horizontal}
        Me.Controls.Add(splitMain)

        ' Bottom Panel (Log)
        Dim grpLog As New GroupBox() With {.Name = "grpLog", .Dock = DockStyle.Bottom, .Height = 120}
        Me.Controls.Add(grpLog)

        txtLog = New TextBox() With {.Dock = DockStyle.Fill, .Multiline = True, .ScrollBars = ScrollBars.Vertical, .ReadOnly = True}
        grpLog.Controls.Add(txtLog)

        progressBar = New ProgressBar() With {.Dock = DockStyle.Bottom, .Height = 20, .Visible = False}
        grpLog.Controls.Add(progressBar)

        ' Center Split Container (Available / Selected)
        splitCenter = New SplitContainer() With {.Dock = DockStyle.Fill, .Orientation = Orientation.Vertical}
        splitMain.Panel1.Controls.Add(splitCenter)

        ' Available List
        Dim grpAvailable As New GroupBox() With {.Name = "grpAvailable", .Dock = DockStyle.Fill}
        splitCenter.Panel1.Controls.Add(grpAvailable)

        txtFilter = New TextBox() With {.Dock = DockStyle.Top}
        grpAvailable.Controls.Add(txtFilter)

        lvAvailable = New ListView() With {.Dock = DockStyle.Fill, .View = View.Details, .FullRowSelect = True, .HideSelection = False}
        lvAvailable.Columns.Add("Name", 150)
        lvAvailable.Columns.Add("PathID", 80)
        lvAvailable.Columns.Add("File", 200)
        grpAvailable.Controls.Add(lvAvailable)

        ' Selected List
        Dim grpSelected As New GroupBox() With {.Name = "grpSelected", .Dock = DockStyle.Fill, .Padding = New Padding(3, 10, 3, 3)}
        splitCenter.Panel2.Controls.Add(grpSelected)

        lvSelected = New ListView() With {.Dock = DockStyle.Fill, .View = View.Details, .FullRowSelect = True, .HideSelection = False}
        lvSelected.Columns.Add("Name", 150)
        lvSelected.Columns.Add("Status", 100)
        lvSelected.Columns.Add("PathID", 80)
        lvSelected.Columns.Add("File", 200)
        grpSelected.Controls.Add(lvSelected)

        ' Context Menu for Selected List
        ctxSelected = New ContextMenuStrip()
        ctxSelected.Items.Add(Loc.GetString("ReplaceWith"))
        AddHandler ctxSelected.Items(0).Click, AddressOf OnReplaceWithClick
        ctxSelected.Items.Add(Loc.GetString("CopyInfo"))
        AddHandler ctxSelected.Items(1).Click, AddressOf OnCopyInfoClick
        lvSelected.ContextMenuStrip = ctxSelected

        ' Fix Docking Z-Order
        pnlTop.SendToBack()
        splitMain.BringToFront()
        grpLog.SendToBack()
        
        lvAvailable.BringToFront()
        txtFilter.SendToBack()
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Set splitter distances dynamically based on client size to support DPI scaling
        If splitMain IsNot Nothing AndAlso Me.ClientSize.Height > 0 Then
            splitMain.SplitterDistance = CInt(Me.ClientSize.Height * 0.7)
        End If
        If splitCenter IsNot Nothing AndAlso splitMain.Panel1.Width > 0 Then
            splitCenter.SplitterDistance = CInt(splitMain.Panel1.Width * 0.45)
        End If
    End Sub

    Private Sub cmbLang_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbLang.SelectedIndexChanged
        Dim selected = CStr(cmbLang.SelectedItem.Value)
        Loc.SaveSettings(selected)
        ApplyLanguage()
    End Sub

    Private Sub ApplyLanguage()
        Me.Text = Loc.GetString("Title")
        
        Dim pnlTop = Me.Controls.OfType(Of Panel)().FirstOrDefault(Function(p) p.Dock = DockStyle.Top)
        If pnlTop IsNot Nothing Then
            Dim lblPath = pnlTop.Controls.Find("lblPath", False).FirstOrDefault()
            If lblPath IsNot Nothing Then lblPath.Text = Loc.GetString("Path")
        End If

        btnScanAll.Text = Loc.GetString("ScanAll")
        chkBackup.Text = Loc.GetString("BackupFile")
        btnStart.Text = Loc.GetString("StartDemosaic")

        ' Find split panels
        Dim splitMain = Me.Controls.OfType(Of SplitContainer)().FirstOrDefault()
        If splitMain IsNot Nothing AndAlso splitMain.Panel1.Controls.Count > 0 Then
            Dim splitCenter = splitMain.Panel1.Controls.OfType(Of SplitContainer)().FirstOrDefault()
            If splitCenter IsNot Nothing Then
                Dim grpAvailable = splitCenter.Panel1.Controls.OfType(Of GroupBox)().FirstOrDefault()
                If grpAvailable IsNot Nothing Then
                    grpAvailable.Text = Loc.GetString("AvailableShaders")
                End If

                Dim grpSelected = splitCenter.Panel2.Controls.OfType(Of GroupBox)().FirstOrDefault()
                If grpSelected IsNot Nothing Then
                    grpSelected.Text = Loc.GetString("SelectedShaders")
                End If
            End If
        End If

        Dim grpLog = Me.Controls.OfType(Of GroupBox)().FirstOrDefault(Function(g) g.Name = "grpLog")
        If grpLog IsNot Nothing Then
            grpLog.Text = Loc.GetString("Log") ' Ensure "Log" exists in localization
        End If

        If lvAvailable IsNot Nothing AndAlso lvAvailable.Columns.Count >= 3 Then
            lvAvailable.Columns(0).Text = Loc.GetString("ColName")
            lvAvailable.Columns(1).Text = Loc.GetString("ColPathID")
            lvAvailable.Columns(2).Text = Loc.GetString("ColFile")
        End If

        If lvSelected IsNot Nothing AndAlso lvSelected.Columns.Count >= 4 Then
            lvSelected.Columns(0).Text = Loc.GetString("ColName")
            lvSelected.Columns(1).Text = Loc.GetString("ColStatus")
            lvSelected.Columns(2).Text = Loc.GetString("ColPathID")
            lvSelected.Columns(3).Text = Loc.GetString("ColFile")
        End If

        ' Update context menu language
        If ctxSelected IsNot Nothing AndAlso ctxSelected.Items.Count >= 2 Then
            ctxSelected.Items(0).Text = Loc.GetString("ReplaceWith")
            ctxSelected.Items(1).Text = Loc.GetString("CopyInfo")
        End If
    End Sub

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If Not String.IsNullOrEmpty(apkSessionTempDir) AndAlso Directory.Exists(apkSessionTempDir) Then
            Try
                Directory.Delete(apkSessionTempDir, True)
            Catch
            End Try
        End If
    End Sub

    Private Sub txtPath_DragEnter(sender As Object, e As DragEventArgs) Handles txtPath.DragEnter
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            e.Effect = DragDropEffects.Copy
        End If
    End Sub

    Private Sub txtPath_DragDrop(sender As Object, e As DragEventArgs) Handles txtPath.DragDrop
        Try
            Dim files As String() = CType(e.Data.GetData(DataFormats.FileDrop), String())
            If files IsNot Nothing AndAlso files.Length > 0 Then
                txtPath.Text = files(0)
            End If
        Catch ex As Exception
            MessageBox.Show(String.Format(Loc.GetString("LogError"), ex.Message))
        End Try
    End Sub

    Private Sub txtPath_DoubleClick(sender As Object, e As EventArgs) Handles txtPath.DoubleClick
        Try
            Using fbd As New FolderBrowserDialog()
                If fbd.ShowDialog() = DialogResult.OK Then
                    txtPath.Text = fbd.SelectedPath
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show(String.Format(Loc.GetString("LogError"), ex.Message))
        End Try
    End Sub

    Private lastScannedPath As String = ""

    Private Sub txtPath_TextChanged(sender As Object, e As EventArgs) Handles txtPath.TextChanged
        Dim path = txtPath.Text
        If Not String.IsNullOrWhiteSpace(path) AndAlso (Directory.Exists(path) OrElse File.Exists(path)) Then
            If path <> lastScannedPath Then
                lastScannedPath = path
                PerformScan(False)
            End If
        End If
    End Sub

    Private Sub OnLogMessage(msg As String)
        If Me.IsDisposed Then Return
        If Me.InvokeRequired Then
            Try
                Me.Invoke(Sub() OnLogMessage(msg))
            Catch
            End Try
        Else
            txtLog.AppendText(msg & vbCrLf)
        End If
    End Sub

    Private Sub OnProgressUpdate(value As Integer, max As Integer)
        If Me.IsDisposed Then Return
        If Me.InvokeRequired Then
            Try
                Me.Invoke(Sub() OnProgressUpdate(value, max))
            Catch
            End Try
        Else
            If max > 0 Then
                progressBar.Visible = True
                progressBar.Maximum = max
                progressBar.Value = value
                If value >= max Then
                    progressBar.Visible = False
                End If
            Else
                progressBar.Visible = False
            End If
        End If
    End Sub

    Private Sub btnScanAll_Click(sender As Object, e As EventArgs) Handles btnScanAll.Click
        PerformScan(True)
    End Sub

    Private Async Sub PerformScan(scanAll As Boolean)
        Dim path = txtPath.Text
        If String.IsNullOrWhiteSpace(path) Then
            MessageBox.Show(Loc.GetString("MsgSelectPath"))
            Return
        End If

        ToggleButtons(False)
        lvAvailable.Items.Clear()
        lvSelected.Items.Clear()
        allShaders.Clear()
        apkMap.Clear()
        replacementMap.Clear()
        txtLog.Clear()

        Dim apkModeDetected As Boolean = False

        If Not File.Exists("classdata.tpk") Then
            Try
                Dim assembly = System.Reflection.Assembly.GetExecutingAssembly()
                Using stream = assembly.GetManifestResourceStream("DemosaicColMaskVB.classdata.tpk")
                    If stream IsNot Nothing Then
                        Using fileStream = File.Create("classdata.tpk")
                            stream.CopyTo(fileStream)
                        End Using
                        txtLog.AppendText("Extracted built-in classdata.tpk." & vbCrLf)
                    Else
                        txtLog.AppendText("Warning: Built-in classdata.tpk not found in assembly." & vbCrLf)
                    End If
                End Using
            Catch ex As Exception
                txtLog.AppendText("Failed to extract classdata.tpk: " & ex.Message & vbCrLf)
            End Try
        End If

        Await Task.Run(Sub()
                           allShaders = processor.ScanShaders(path, scanAll, apkMap)
                       End Sub)

        If apkMap.Count > 0 Then
            chkBackup.Checked = False
        Else
            chkBackup.Checked = True
        End If

        UpdateAvailableList()
        AutoSelectMosaicShaders()
        ToggleButtons(True)
    End Sub

    Private Sub ToggleButtons(enabled As Boolean)
        btnScanAll.Enabled = enabled
        btnStart.Enabled = enabled
    End Sub

    Private Sub UpdateAvailableList()
        lvAvailable.Items.Clear()
        Dim filter = txtFilter.Text.ToLower()

        Dim selectedKeys As New HashSet(Of String)()
        For Each item As ListViewItem In lvSelected.Items
            selectedKeys.Add(item.SubItems(2).Text & "|" & item.SubItems(3).Text)
        Next

        For Each shader In allShaders
            Dim key = shader.PathId.ToString() & "|" & shader.FilePath
            If selectedKeys.Contains(key) Then Continue For

            If String.IsNullOrEmpty(filter) OrElse shader.Name.ToLower().Contains(filter) OrElse shader.FilePath.ToLower().Contains(filter) Then
                Dim lvi As New ListViewItem(shader.Name)
                lvi.SubItems.Add(shader.PathId.ToString())
                lvi.SubItems.Add(Path.GetFileName(shader.FilePath))
                lvi.Tag = shader
                If IsPotentialMosaicShader(shader.Name) Then
                    lvi.BackColor = Drawing.Color.FromArgb(255, 238, 218)
                End If
                lvAvailable.Items.Add(lvi)
            End If
        Next
    End Sub

    Private Function IsPotentialMosaicShader(shaderName As String) As Boolean
        If String.IsNullOrWhiteSpace(shaderName) Then Return False
        Return shaderName.ToLower().Contains("pixel")
    End Function

    Private Sub txtFilter_TextChanged(sender As Object, e As EventArgs) Handles txtFilter.TextChanged
        UpdateAvailableList()
    End Sub

    Private Sub AutoSelectMosaicShaders()
        Dim keywords As String() = {"mos", "moz", "masi", "maz", "censor", "ピクセル", "モザイク"}
        Dim toMove As New List(Of ListViewItem)()

        For Each item As ListViewItem In lvAvailable.Items
            Dim name = item.Text.ToLower()
            For Each k In keywords
                If name.Contains(k) Then
                    toMove.Add(item)
                    Exit For
                End If
            Next
        Next

        For Each item In toMove
            lvAvailable.Items.Remove(item)
            Dim newItem As New ListViewItem(item.Text)
            newItem.SubItems.Add("-> colMask edit 0") ' Status
            newItem.SubItems.Add(item.SubItems(1).Text) ' PathId
            newItem.SubItems.Add(item.SubItems(2).Text) ' File
            newItem.Tag = item.Tag
            lvSelected.Items.Add(newItem)
        Next
    End Sub

    Private Sub lvAvailable_DoubleClick(sender As Object, e As EventArgs) Handles lvAvailable.DoubleClick
        If lvAvailable.SelectedItems.Count > 0 Then
            For Each item As ListViewItem In lvAvailable.SelectedItems
                lvAvailable.Items.Remove(item)
                Dim newItem As New ListViewItem(item.Text)
                newItem.SubItems.Add("-> colMask edit 0") ' Status
                newItem.SubItems.Add(item.SubItems(1).Text) ' PathId
                newItem.SubItems.Add(item.SubItems(2).Text) ' File
                newItem.Tag = item.Tag
                lvSelected.Items.Add(newItem)
            Next
        End If
    End Sub

    Private Sub lvSelected_DoubleClick(sender As Object, e As EventArgs) Handles lvSelected.DoubleClick
        If lvSelected.SelectedItems.Count > 0 Then
            For Each item As ListViewItem In lvSelected.SelectedItems
                lvSelected.Items.Remove(item)
                Dim key = item.SubItems(2).Text & "|" & item.SubItems(3).Text
                If replacementMap.ContainsKey(key) Then
                    replacementMap.Remove(key)
                End If
            Next
            UpdateAvailableList()
        End If
    End Sub

    Private Sub OnReplaceWithClick(sender As Object, e As EventArgs)
        If lvSelected.SelectedItems.Count = 0 Then Return
        Dim selectedItem = lvSelected.SelectedItems(0)
        Dim originalShader = CType(selectedItem.Tag, AssetProcessor.ShaderInfo)

        ' Get available shaders to choose from (ones not currently in selected list)
        Dim availableShaders As New List(Of AssetProcessor.ShaderInfo)()
        Dim selectedKeys As New HashSet(Of String)()
        For Each item As ListViewItem In lvSelected.Items
            selectedKeys.Add(item.SubItems(2).Text & "|" & item.SubItems(3).Text)
        Next
        For Each shader In allShaders
            Dim key = shader.PathId.ToString() & "|" & shader.FilePath
            If Not selectedKeys.Contains(key) Then
                availableShaders.Add(shader)
            End If
        Next

        Using frm As New ReplaceForm(availableShaders, originalShader, Loc)
            If frm.ShowDialog(Me) = DialogResult.OK Then
                Dim targetShader = frm.SelectedShader
                Dim key = originalShader.PathId.ToString() & "|" & originalShader.FilePath
                replacementMap(key) = targetShader
                selectedItem.SubItems(1).Text = "-> " & targetShader.Name
                UpdateAvailableList()
            End If
        End Using
    End Sub

    Private Sub OnCopyInfoClick(sender As Object, e As EventArgs)
        If lvSelected.SelectedItems.Count = 0 Then Return
        Dim selectedItem = lvSelected.SelectedItems(0)
        Dim shader = CType(selectedItem.Tag, AssetProcessor.ShaderInfo)
        Dim info = $"Name: {shader.Name}{vbCrLf}PathID: {shader.PathId}{vbCrLf}File: {Path.GetFileName(shader.FilePath)}{vbCrLf}Full Path: {shader.FilePath}"
        Try
            Clipboard.SetText(info)
        Catch
        End Try
    End Sub

    Private Async Sub btnStart_Click(sender As Object, e As EventArgs) Handles btnStart.Click
        If lvSelected.Items.Count = 0 Then
            MessageBox.Show(Loc.GetString("MsgNoSelection"))
            Return
        End If

        ' Pre-flight check for APK mode
        If apkMap.Count > 0 Then
            Dim exeDir = AppDomain.CurrentDomain.BaseDirectory
            Dim jars = Directory.GetFiles(exeDir, "uber-apk-signer*.jar")
            If jars.Length = 0 Then
                MessageBox.Show(String.Format(Loc.GetString("LogError"), "uber-apk-signer.jar not found."))
                Return
            End If

            Try
                Dim psi As New ProcessStartInfo("java", "-version")
                psi.UseShellExecute = False
                psi.CreateNoWindow = True
                psi.RedirectStandardError = True
                Using proc = Process.Start(psi)
                    proc.WaitForExit()
                End Using
            Catch ex As Exception
                MessageBox.Show(String.Format(Loc.GetString("LogError"), "Java is not installed or not in PATH."))
                Return
            End Try
        End If

        ToggleButtons(False)
        Dim backup = chkBackup.Checked
        Dim selectedItems As New List(Of ListViewItem)()
        For Each item As ListViewItem In lvSelected.Items
            selectedItems.Add(item)
        Next

        Await Task.Run(Sub()
                           Try
                               Dim shadersToProcess As New Dictionary(Of String, HashSet(Of Long))()

                               For Each item In selectedItems
                                   Dim shader = CType(item.Tag, AssetProcessor.ShaderInfo)
                                   Dim status = item.SubItems(1).Text
                                   Dim key = item.SubItems(2).Text & "|" & item.SubItems(3).Text

                                   If replacementMap.ContainsKey(key) Then
                                       Dim sourceShader = replacementMap(key)
                                       processor.ExecuteReplacement(shader, sourceShader, backup)
                                   Else
                                       If Not shadersToProcess.ContainsKey(shader.FilePath) Then
                                           shadersToProcess(shader.FilePath) = New HashSet(Of Long)()
                                       End If
                                       shadersToProcess(shader.FilePath).Add(shader.PathId)
                                   End If
                               Next

                               If shadersToProcess.Count > 0 Then
                                   processor.ProcessAssets(shadersToProcess, backup)
                               End If

                               If apkMap.Count > 0 Then
                                   For Each kvp In apkMap
                                       Dim originalApk = kvp.Key
                                       Dim tempDir = kvp.Value
                                       
                                       Dim modifiedFiles As New List(Of String)()
                                       For Each f In shadersToProcess.Keys
                                           If f.StartsWith(tempDir) Then
                                               modifiedFiles.Add(f)
                                           End If
                                       Next
                                       
                                       If modifiedFiles.Count > 0 Then
                                           processor.RepackAndSignApk(originalApk, tempDir, modifiedFiles)
                                       End If
                                   Next
                               End If
                           Catch ex As Exception
                               If Not Me.IsDisposed AndAlso Me.InvokeRequired Then
                                   Me.Invoke(Sub() MessageBox.Show(String.Format(Loc.GetString("LogError"), ex.Message)))
                               End If
                           End Try
                       End Sub)

        ToggleButtons(True)
        MessageBox.Show(Loc.GetString("MsgComplete"))
    End Sub

End Class

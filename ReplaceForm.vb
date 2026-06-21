Imports System.Windows.Forms
Imports System.Drawing

Public Class ReplaceForm
    Inherits Form

    Private lvShaders As ListView
    Private btnOk As Button
    Private btnCancel As Button

    Public SelectedShader As AssetProcessor.ShaderInfo
    Private allShaders As List(Of AssetProcessor.ShaderInfo)

    Public Sub New(availableShaders As List(Of AssetProcessor.ShaderInfo), targetShader As AssetProcessor.ShaderInfo, loc As Localization)
        Me.allShaders = availableShaders

        Me.Text = loc.GetString("SelectSourceShader")
        Me.Size = New Size(620, 450)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MinimumSize = New Size(500, 350)

        ' --- Right side button panel ---
        Dim pnlButtons As New Panel() With {.Dock = DockStyle.Right, .Width = 90}
        Me.Controls.Add(pnlButtons)

        btnOk = New Button() With {.Text = loc.GetString("OK"), .Width = 75, .Height = 28, .Location = New Point(7, 10)}
        AddHandler btnOk.Click, AddressOf btnOk_Click
        pnlButtons.Controls.Add(btnOk)

        btnCancel = New Button() With {.Text = loc.GetString("Cancel"), .Width = 75, .Height = 28, .Location = New Point(7, 44)}
        btnCancel.DialogResult = DialogResult.Cancel
        pnlButtons.Controls.Add(btnCancel)

        Me.AcceptButton = btnOk
        Me.CancelButton = btnCancel

        ' --- Top info panel ---
        Dim pnlInfo As New GroupBox() With {
            .Text = loc.GetString("TargetShaderInfo"),
            .Dock = DockStyle.Top,
            .Height = 75,
            .Padding = New Padding(6)
        }
        Me.Controls.Add(pnlInfo)

        Dim lblInfo As New Label() With {
            .Dock = DockStyle.Fill,
            .Text = String.Format("Name: {0}" & vbCrLf & "PathID: {1}" & vbCrLf & "File: {2}",
                targetShader.Name,
                targetShader.PathId,
                IO.Path.GetFileName(targetShader.FilePath))
        }
        pnlInfo.Controls.Add(lblInfo)

        ' --- Shader list ---
        lvShaders = New ListView() With {
            .Dock = DockStyle.Fill,
            .View = View.Details,
            .FullRowSelect = True,
            .HideSelection = False,
            .MultiSelect = False
        }
        lvShaders.Columns.Add(loc.GetString("ColName"), 200)
        lvShaders.Columns.Add(loc.GetString("ColPathID"), 60)
        lvShaders.Columns.Add(loc.GetString("ColFile"), 220)
        AddHandler lvShaders.DoubleClick, AddressOf btnOk_Click
        Me.Controls.Add(lvShaders)

        ' Fix Z-order so list fills remaining space
        lvShaders.BringToFront()

        PopulateList()
    End Sub

    Private Sub PopulateList()
        lvShaders.Items.Clear()
        For Each s In allShaders
            Dim lvi As New ListViewItem(s.Name)
            lvi.SubItems.Add(s.PathId.ToString())
            lvi.SubItems.Add(IO.Path.GetFileName(s.FilePath))
            lvi.Tag = s
            lvShaders.Items.Add(lvi)
        Next
        If lvShaders.Items.Count > 0 Then
            lvShaders.Items(0).Selected = True
        End If
    End Sub

    Private Sub btnOk_Click(sender As Object, e As EventArgs)
        If lvShaders.SelectedItems.Count > 0 Then
            SelectedShader = CType(lvShaders.SelectedItems(0).Tag, AssetProcessor.ShaderInfo)
            Me.DialogResult = DialogResult.OK
            Me.Close()
        End If
    End Sub
End Class

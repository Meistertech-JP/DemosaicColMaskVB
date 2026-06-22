Imports System.Drawing
Imports System.Windows.Forms

Public Class KeywordSettingsForm
    Inherits Form

    Private ReadOnly Loc As Localization
    Private ReadOnly lstKeywords As ListBox
    Private ReadOnly txtKeyword As TextBox
    Private ReadOnly btnAdd As Button
    Private ReadOnly btnDelete As Button
    Private ReadOnly btnDefaults As Button
    Private ReadOnly btnOk As Button
    Private ReadOnly btnCancel As Button

    Public ReadOnly Property Keywords As List(Of String)
        Get
            Dim values As New List(Of String)()
            For Each item In lstKeywords.Items
                values.Add(CStr(item))
            Next
            Return values
        End Get
    End Property

    Public Sub New(currentKeywords As IEnumerable(Of String), loc As Localization)
        Me.Loc = loc

        Me.Text = loc.GetString("KeywordSettings")
        Me.Size = New Size(430, 430)
        Me.MinimumSize = New Size(380, 340)
        Me.StartPosition = FormStartPosition.CenterParent

        Dim pnlBottom As New Panel() With {.Dock = DockStyle.Bottom, .Height = 42}
        Me.Controls.Add(pnlBottom)

        btnOk = New Button() With {.Text = loc.GetString("OK"), .Width = 78, .Height = 28, .Left = 255, .Top = 7}
        AddHandler btnOk.Click, AddressOf btnOk_Click
        pnlBottom.Controls.Add(btnOk)

        btnCancel = New Button() With {.Text = loc.GetString("Cancel"), .Width = 78, .Height = 28, .Left = 337, .Top = 7}
        btnCancel.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnCancel.DialogResult = DialogResult.Cancel
        pnlBottom.Controls.Add(btnCancel)

        Dim pnlActions As New Panel() With {.Dock = DockStyle.Bottom, .Height = 40}
        Me.Controls.Add(pnlActions)

        txtKeyword = New TextBox() With {.Left = 10, .Top = 9, .Width = 185, .Anchor = AnchorStyles.Left Or AnchorStyles.Top Or AnchorStyles.Right}
        AddHandler txtKeyword.KeyDown, AddressOf txtKeyword_KeyDown
        pnlActions.Controls.Add(txtKeyword)

        btnAdd = New Button() With {.Text = loc.GetString("AddKeyword"), .Width = 64, .Height = 26, .Left = 202, .Top = 7}
        btnAdd.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        AddHandler btnAdd.Click, AddressOf btnAdd_Click
        pnlActions.Controls.Add(btnAdd)

        btnDelete = New Button() With {.Text = loc.GetString("DeleteKeyword"), .Width = 64, .Height = 26, .Left = 270, .Top = 7}
        btnDelete.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        AddHandler btnDelete.Click, AddressOf btnDelete_Click
        pnlActions.Controls.Add(btnDelete)

        btnDefaults = New Button() With {.Text = loc.GetString("RestoreDefaults"), .Width = 84, .Height = 26, .Left = 338, .Top = 7}
        btnDefaults.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        AddHandler btnDefaults.Click, AddressOf btnDefaults_Click
        pnlActions.Controls.Add(btnDefaults)

        Dim lblTitle As New Label() With {
            .Text = loc.GetString("KeywordList"),
            .Dock = DockStyle.Top,
            .Height = 28,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Padding = New Padding(10, 0, 0, 0)
        }
        Me.Controls.Add(lblTitle)

        lstKeywords = New ListBox() With {.Dock = DockStyle.Fill}
        AddHandler lstKeywords.DoubleClick, AddressOf lstKeywords_DoubleClick
        Me.Controls.Add(lstKeywords)
        lstKeywords.BringToFront()

        LoadKeywords(currentKeywords)
        Me.AcceptButton = btnAdd
        Me.CancelButton = btnCancel
    End Sub

    Private Sub LoadKeywords(values As IEnumerable(Of String))
        lstKeywords.Items.Clear()
        Dim seen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

        For Each value In values
            If value Is Nothing Then Continue For
            Dim keyword = value.Trim()
            If keyword.Length = 0 Then Continue For
            If seen.Add(keyword) Then lstKeywords.Items.Add(keyword)
        Next
    End Sub

    Private Sub AddKeyword()
        Dim keyword = txtKeyword.Text.Trim()
        If keyword.Length = 0 Then Return

        For Each item In lstKeywords.Items
            If String.Equals(CStr(item), keyword, StringComparison.OrdinalIgnoreCase) Then
                lstKeywords.SelectedItem = item
                txtKeyword.SelectAll()
                Return
            End If
        Next

        lstKeywords.Items.Add(keyword)
        lstKeywords.SelectedItem = keyword
        txtKeyword.Clear()
        txtKeyword.Focus()
    End Sub

    Private Sub btnAdd_Click(sender As Object, e As EventArgs)
        AddKeyword()
    End Sub

    Private Sub btnDelete_Click(sender As Object, e As EventArgs)
        If lstKeywords.SelectedIndex >= 0 Then
            Dim nextIndex = Math.Min(lstKeywords.SelectedIndex, lstKeywords.Items.Count - 2)
            lstKeywords.Items.RemoveAt(lstKeywords.SelectedIndex)
            If nextIndex >= 0 AndAlso nextIndex < lstKeywords.Items.Count Then
                lstKeywords.SelectedIndex = nextIndex
            End If
        End If
    End Sub

    Private Sub btnDefaults_Click(sender As Object, e As EventArgs)
        LoadKeywords(KeywordSettings.GetDefaultKeywords())
    End Sub

    Private Sub btnOk_Click(sender As Object, e As EventArgs)
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub txtKeyword_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            AddKeyword()
            e.SuppressKeyPress = True
        End If
    End Sub

    Private Sub lstKeywords_DoubleClick(sender As Object, e As EventArgs)
        If lstKeywords.SelectedItem IsNot Nothing Then
            txtKeyword.Text = CStr(lstKeywords.SelectedItem)
            txtKeyword.SelectAll()
            txtKeyword.Focus()
        End If
    End Sub
End Class

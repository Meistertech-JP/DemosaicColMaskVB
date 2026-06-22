Imports System.Collections
Imports System.Windows.Forms

Public Class ListViewColumnSorter
    Implements IComparer

    Public Property SortColumn As Integer = -1
    Public Property Order As SortOrder = SortOrder.None

    Public Shared Sub Attach(listView As ListView)
        If listView Is Nothing Then Return
        listView.ListViewItemSorter = New ListViewColumnSorter()
        AddHandler listView.ColumnClick, AddressOf OnColumnClick
    End Sub

    Private Shared Sub OnColumnClick(sender As Object, e As ColumnClickEventArgs)
        Dim listView = TryCast(sender, ListView)
        If listView Is Nothing Then Return

        Dim sorter = TryCast(listView.ListViewItemSorter, ListViewColumnSorter)
        If sorter Is Nothing Then
            sorter = New ListViewColumnSorter()
            listView.ListViewItemSorter = sorter
        End If

        If e.Column = sorter.SortColumn Then
            sorter.Order = If(sorter.Order = SortOrder.Ascending, SortOrder.Descending, SortOrder.Ascending)
        Else
            sorter.SortColumn = e.Column
            sorter.Order = SortOrder.Ascending
        End If

        listView.Sort()
    End Sub

    Public Function Compare(x As Object, y As Object) As Integer Implements IComparer.Compare
        Dim left = TryCast(x, ListViewItem)
        Dim right = TryCast(y, ListViewItem)
        If left Is Nothing OrElse right Is Nothing OrElse SortColumn < 0 OrElse Order = SortOrder.None Then Return 0

        Dim leftText = GetSubItemText(left, SortColumn)
        Dim rightText = GetSubItemText(right, SortColumn)

        Dim leftNumber As Long
        Dim rightNumber As Long
        Dim result As Integer
        If Long.TryParse(leftText, leftNumber) AndAlso Long.TryParse(rightText, rightNumber) Then
            result = leftNumber.CompareTo(rightNumber)
        Else
            result = StringComparer.CurrentCultureIgnoreCase.Compare(leftText, rightText)
        End If

        If Order = SortOrder.Descending Then result = -result
        Return result
    End Function

    Private Function GetSubItemText(item As ListViewItem, columnIndex As Integer) As String
        If columnIndex >= 0 AndAlso columnIndex < item.SubItems.Count Then
            Return item.SubItems(columnIndex).Text
        End If

        Return ""
    End Function
End Class

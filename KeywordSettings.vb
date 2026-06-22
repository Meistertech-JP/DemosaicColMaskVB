Imports System.IO
Imports System.Text.Json

Public NotInheritable Class KeywordSettings
    Private Shared ReadOnly ConfigPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mosaic_keywords.json")
    Private Shared ReadOnly DefaultKeywordValues As String() = {"mos", "moz", "masi", "maz", "censor", "ピクセル", "モザイク"}

    Private Sub New()
    End Sub

    Public Shared Function GetDefaultKeywords() As List(Of String)
        Return New List(Of String)(DefaultKeywordValues)
    End Function

    Public Shared Function Load() As List(Of String)
        Try
            If File.Exists(ConfigPath) Then
                Dim json = File.ReadAllText(ConfigPath)
                Dim values = JsonSerializer.Deserialize(Of List(Of String))(json)
                Dim normalized = Normalize(values)
                If normalized.Count > 0 Then Return normalized
            End If
        Catch
        End Try

        Return GetDefaultKeywords()
    End Function

    Public Shared Sub Save(keywords As IEnumerable(Of String))
        Dim normalized = Normalize(keywords)
        If normalized.Count = 0 Then normalized = GetDefaultKeywords()

        Try
            Dim options As New JsonSerializerOptions With {.WriteIndented = True}
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(normalized, options))
        Catch
        End Try
    End Sub

    Private Shared Function Normalize(values As IEnumerable(Of String)) As List(Of String)
        Dim result As New List(Of String)()
        If values Is Nothing Then Return result

        Dim seen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each value In values
            If value Is Nothing Then Continue For
            Dim keyword = value.Trim()
            If keyword.Length = 0 Then Continue For
            If seen.Add(keyword) Then result.Add(keyword)
        Next

        Return result
    End Function
End Class

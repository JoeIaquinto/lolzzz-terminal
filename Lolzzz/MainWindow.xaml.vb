Imports System.Diagnostics.Process


Class LolzzzTerminal

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()
        Dim lzVM As LolzzzViewModel = New LolzzzViewModel()
        DataContext = lzVM
        ' Add any initialization after the InitializeComponent() call.

    End Sub
    Private Sub memeGrid_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles memeGrid.MouseDoubleClick
        Dim key As Integer = sender.CurrentItem.Row("MemeKey")
        System.Diagnostics.Process.Start("http://www.lolzzz.net/Order/Details/" & key.ToString)
    End Sub

End Class

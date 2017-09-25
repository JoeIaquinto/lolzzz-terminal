Imports System.Diagnostics.Process
Imports System.Data

Class LolzzzTerminal

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()
        Dim lzVM As LolzzzViewModel = New LolzzzViewModel()
        DataContext = lzVM
        ' Add any initialization after the InitializeComponent() call.

    End Sub
    Private Sub memeGrid_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles memeGrid.MouseDoubleClick
        If sender.CurrentItem IsNot Nothing AndAlso TypeOf (sender.CurrentItem) Is DataRowView Then

            Dim key As Integer = sender.CurrentItem.Row("MemeKey")
            System.Diagnostics.Process.Start("http://www.lolzzz.net/Order/Details/" & key.ToString)
        End If
    End Sub

End Class

Imports System.Net.Http
Imports Newtonsoft.Json
Imports System.ComponentModel
Imports System.Data
Imports System.Windows.Input
Imports System.Diagnostics.Process

Public Class LolzzzViewModel
    Implements INotifyPropertyChanged
    'Constants
    Private Const ALL_VALUE As String = "__*__"
    Private Const FILTER_REPLACE_STRING = "!!~!!"
    Private Const FILTER_BIT_REPLACE_STRING = "~~!~~"

    Private ReadOnly httpClient = New HttpClient()
    Private _keys As List(Of String)
    Private _memesDT As DataTable
    Private _columnFilterGenericString As String
    Private _bitFields As List(Of String)
    Private _columnFilterTable As DataTable
    Private _currentColumnFilterText As String
    Private _currentColumnFilter As String = ALL_VALUE
    Private _apiKey As String = "31E2B259-343B-4752-8930-5D02AE1F352A"
    Public Sub New()
        '"MemeKey":1,"Ticker":"SWAG","Name":"Swag Baby","FullImageURL":"http://i.imgur.com/SYluBOh.gif","LZValue":0.0000,
        '"CurrentSharePrice":0.0000,"Likes":27,"Dislikes":8,"UserName":"OriginalScrubLord",
        '"CreateDate""/Date(1450187533410)/","Headwinds":0,"Tailwinds":0,"RND":0,"Marketing":0,"CurrentOrders":[],
        '"CurrentOwners":[{"QtyOwned":9500,"userKey":1,"UserName":"OriginalScrubLord"},{"QtyOwned":500,"userKey":1133,
        '"UserName":"Twincam"}]}
        _memesDT = New DataTable
        _memesDT.Columns.Add("MemeKey")
        _memesDT.Columns.Add("Ticker")
        _memesDT.Columns.Add("Name")
        _memesDT.Columns.Add("FullImageURL")
        _memesDT.Columns.Add("LZValue")
        _memesDT.Columns.Add("CurrentSharePrice")
        _memesDT.Columns.Add("Likes")
        _memesDT.Columns.Add("Dislikes")
        _memesDT.Columns.Add("UserName")
        _memesDT.Columns.Add("CreateDate")
        _memesDT.Columns.Add("Headwinds")
        _memesDT.Columns.Add("Tailwinds")
        _memesDT.Columns.Add("RND")
        _memesDT.Columns.Add("Marketing")
        _memesDT.Columns.Add("CurrentOwners")

        CreateTableOfColumnFilters()
        FireKeyDownload(_apiKey)

    End Sub

    Public Property Keys As List(Of String)
        Get
            Return _keys
        End Get
        Set(value As List(Of String))
            _keys = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("Keys"))
        End Set
    End Property

    Public Property CurrentColumnFilterText As String
        Get
            Return _currentColumnFilterText
        End Get
        Set(value As String)
            _currentColumnFilterText = value

            ApplyFilters()
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("CurrentColumnFilterText"))
        End Set
    End Property

    Public Property ColumnFilterTable As DataTable
        Get
            Return _columnFilterTable
        End Get
        Set(value As DataTable)
            _columnFilterTable = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("ColumnFilterTable"))
        End Set
    End Property

    Public Property CurrentColumnFilter As String
        Get
            Return _currentColumnFilter
        End Get
        Set(value As String)
            _currentColumnFilter = value
            ApplyFilters()
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("CurrentColumnFilter"))
        End Set
    End Property

    Public Property MemesDV As DataView

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Public Sub ApplyFilters()
        If MemesDV Is Nothing Then
            Exit Sub
        End If

        Dim filter As String = ""

        If Not String.IsNullOrEmpty(_currentColumnFilterText) Then
            Dim fixedColumnFilterText As String = SQLFixup(_currentColumnFilterText.Replace("[", "[[").Replace("]", "]]").Replace("[[", "[[]").Replace("]]", "[]]").Replace("*", "[*]").Replace("%", "[%]").Replace("[[[]]]", "[[][]]"))
            Dim columnFilterString As String = ""

            If _currentColumnFilter = ALL_VALUE Then
                columnFilterString = _columnFilterGenericString.Replace(FILTER_REPLACE_STRING, fixedColumnFilterText)

                'If the user types "1" or "0" then we want to use Boolean.TrueString/FalseString for the bit fields.
                If fixedColumnFilterText = "1" Or String.Equals(fixedColumnFilterText, Boolean.TrueString, StringComparison.InvariantCultureIgnoreCase) Then
                    columnFilterString = columnFilterString.Replace(FILTER_BIT_REPLACE_STRING, Boolean.TrueString)
                ElseIf fixedColumnFilterText = "0" Or String.Equals(fixedColumnFilterText, Boolean.FalseString, StringComparison.InvariantCultureIgnoreCase) Then
                    columnFilterString = columnFilterString.Replace(FILTER_BIT_REPLACE_STRING, Boolean.FalseString)
                Else
                    'If it is not a 0, 1, FalseString, or TrueString then we want all bit column checks to return false.
                    'Since the bit filters are "VALUE = STRING" replacing FILTER_BIT_REPLACE_STRING with the current text will handle it.
                    columnFilterString = columnFilterString.Replace(FILTER_BIT_REPLACE_STRING, fixedColumnFilterText)
                End If
            Else
                'If we are filtering on a bit field then we need to convert 1 to true and 0 to false.
                If _bitFields.Contains(_currentColumnFilter) Then
                    If String.Equals(fixedColumnFilterText, Boolean.TrueString, StringComparison.InvariantCultureIgnoreCase) Then
                        columnFilterString = "[" & _currentColumnFilter & "]=1"
                    ElseIf String.Equals(fixedColumnFilterText, Boolean.FalseString, StringComparison.InvariantCultureIgnoreCase) Then
                        columnFilterString = "[" & _currentColumnFilter & "]=0"
                    Else
                        'If it is not a 0, 1, FalseString, or TrueString then we want to clear all rows.
                        columnFilterString &= "0 = 1"
                    End If
                Else
                    columnFilterString &= String.Format(" Convert([{0}], 'System.String') LIKE '%{1}%' ", New String() {_currentColumnFilter, fixedColumnFilterText})
                End If
            End If

            AppendFilterCriteria(columnFilterString, filter)
        End If

        MemesDV.RowFilter = filter

        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("MemesDV"))
    End Sub

    Public Async Sub FireKeyDownload(apiKey As String)
        Dim keysPage As String = "http://www.lolzzz.net/LZAPI/GetMemeKeys?apikey=" & apiKey
        Dim k As String = Await DownloadPageAsync(keysPage)
        k = k.Substring(1, k.Length - 2)
        Keys = New List(Of String)(k.Split(","))
        For i = 0 To Keys.Count - 1
            FireMemeDownload(apiKey, Keys(i))
        Next
    End Sub

    Public Async Sub FireMemeDownload(apiKey As String, memekey As String)
        Dim getMemeByKeyPage As String = "http://www.lolzzz.net/LZAPI/GetMemeByKey?apikey=31E2B259-343B-4752-8930-5D02AE1F352A&memekey=" & memekey
        Dim memeJson As String = Await DownloadPageAsync(getMemeByKeyPage)
        Dim meme As Meme = JsonConvert.DeserializeObject(Of Meme)(memeJson)
        _memesDT.Rows.Add(MemeAsDataRow(meme))
        MemesDV = _memesDT.DefaultView
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("MemesDV"))
    End Sub

    Async Function DownloadPageAsync(page As String) As Task(Of String)

        Using response As HttpResponseMessage = Await httpClient.GetAsync(page)
            Using content As HttpContent = response.Content
                ' Get contents of page as a String.
                Dim result As String = Await content.ReadAsStringAsync()
                ' If data exists, print a substring.
                If result IsNot Nothing Then
                    Console.WriteLine(result)
                    Return result
                End If
            End Using
        End Using
        Return Nothing
    End Function

    Public Function MemeAsDataRow(m As Meme) As DataRow
        Dim dr As DataRow = _memesDT.NewRow()
        dr("MemeKey") = m.MemeKey
        dr("Ticker") = m.Ticker
        dr("Name") = m.Name
        dr("FullImageURL") = m.FullImageURL
        dr("LZValue") = m.LZValue
        dr("CurrentSharePrice") = m.CurrentSharePrice
        dr("Likes") = m.Likes
        dr("Dislikes") = m.Dislikes
        dr("UserName") = m.UserName
        dr("CreateDate") = m.CreateDate
        dr("Headwinds") = m.Headwinds
        dr("Tailwinds") = m.Tailwinds
        dr("RND") = m.RND
        dr("Marketing") = m.Marketing
        dr("CurrentOwners") = OwnersToString(m.CurrentOwners)
        Return dr
    End Function

    Public Function OwnersToString(m As CurrentOwner()) As String
        Dim ret As String = ""
        Dim o = From mo In m
                Order By mo.QtyOwned Descending, mo.UserName
                Select mo
        For i = 0 To o.Count - 1
            ret &= $"{o(i).UserName}: {o(i).QtyOwned}
"
        Next
        Return ret
    End Function

    Private Sub CreateTableOfColumnFilters()
        Dim newRow As DataRow
        Dim columnsDT As DataTable = New DataTable()

        columnsDT.Columns.Add(New DataColumn("ColumnName"))
        columnsDT.Columns.Add(New DataColumn("ID"))
        _columnFilterGenericString = ""
        _bitFields = New List(Of String)

        For Each dc As DataColumn In _memesDT.Columns

            'The column name will be seen so it must be translated.  The ID is only used behind the scenes so it can remain in english.
            newRow = columnsDT.NewRow
            newRow("ID") = dc.ColumnName



            'This gets displayed so translate it
            newRow("ColumnName") = dc.ColumnName

            columnsDT.Rows.Add(newRow)

            'We want to create the generic filter All string now in order to avoid having to create it everytime that {All} is selected for the column filter.
            'The convert is needed to avoid errors on integer columns.
            If dc.DataType = GetType(Boolean) Then
                'We cannot use a case statement in the row filter.  With this, we want to have a separate string to replace for bit columns.
                'This way when "1" is being filtered on we can change this to Boolean.TrueString.
                _columnFilterGenericString &= String.Format("Convert([{0}], 'System.String') = '{1}' OR ", New Object() {dc.ColumnName, FILTER_BIT_REPLACE_STRING})
                _bitFields.Add(dc.ColumnName)
            Else
                _columnFilterGenericString &= String.Format("Convert([{0}], 'System.String') LIKE '%{1}%' OR ", New Object() {dc.ColumnName, FILTER_REPLACE_STRING})
            End If
        Next

        'If _columnFilterGenericString is not empty then it will end in " OR " we want to remove that.  Also, wrap it in parenthesis so the logic will stay contained.
        If _columnFilterGenericString.EndsWith(" OR ") Then
            _columnFilterGenericString = _columnFilterGenericString.Substring(0, _columnFilterGenericString.Length - " OR ".Length)
            _columnFilterGenericString = "(" & _columnFilterGenericString & ")"
        End If

        'Order _columnFilterTable alphabetically.
        columnsDT = columnsDT.Select("", "ColumnName").CopyToDataTable

        'Insert the {All} row at the top
        newRow = columnsDT.NewRow
        newRow("ColumnName") = "{All}"
        newRow("ID") = ALL_VALUE
        columnsDT.Rows.InsertAt(newRow, 0)

        _columnFilterTable = columnsDT

        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("ColumnFilterTable"))
    End Sub
    Private Function SQLFixup(ByVal sqlString As String) As String
        Return sqlString.Replace("'", "''")
    End Function

    Private Sub AppendFilterCriteria(ByVal filterCondition As String, ByRef resultingFilterString As String)
        If resultingFilterString.Length > 0 Then
            resultingFilterString &= " AND "
        End If

        resultingFilterString &= filterCondition
    End Sub

    Private _openAllCommand As RelayCommand

    Public ReadOnly Property OpenAllCommand As ICommand
        Get
            If _openAllCommand Is Nothing Then
                _openAllCommand = New RelayCommand(AddressOf OpenAll)
            End If
            Return _openAllCommand
        End Get
    End Property

    Private Sub OpenAll(arg As Object)
        For mk = 0 To MemesDV.Count - 1
            Dim key As Integer = MemesDV.Item(mk)("MemeKey")
            System.Diagnostics.Process.Start("http://www.lolzzz.net/Order/Details/" & key.ToString)
        Next
    End Sub

    Private _refreshCommand As RelayCommand

    Public ReadOnly Property RefreshCommand As ICommand
        Get
            If _refreshCommand Is Nothing Then
                _refreshCommand = New RelayCommand(AddressOf Refresh)
            End If
            Return _refreshCommand
        End Get
    End Property

    Private Sub Refresh(arg As Object)
        _memesDT.Rows.Clear()
        FireKeyDownload(_apiKey)
    End Sub

End Class


Public Class CurrentOrder
    Public Property OrderType As Integer
    Public Property OrderCreatorUserName As String
    Public Property Price As Double
    Public Property ExpirationDate As DateTime
    Public Property CreateDate As DateTime
    Public Property RemainingQty As Integer
End Class

Public Class CurrentOwner
    Public Property QtyOwned As Integer
    Public Property userKey As Integer
    Public Property UserName As String
End Class

Public Class Meme
    Public Property MemeKey As Integer
    Public Property Ticker As String
    Public Property Name As String
    Public Property FullImageURL As String
    Public Property LZValue As Double
    Public Property CurrentSharePrice As Double
    Public Property Likes As Integer
    Public Property Dislikes As Integer
    Public Property UserName As String
    Public Property CreateDate As DateTime
    Public Property Headwinds As Integer
    Public Property Tailwinds As Integer
    Public Property RND As Integer
    Public Property Marketing As Integer
    Public Property CurrentOrders As CurrentOrder()
    Public Property CurrentOwners As CurrentOwner()

End Class



''' <summary>
''' A command whose sole purpose is to 
''' relay its functionality to other
''' objects by invoking delegates. The
''' default return value for the CanExecute
''' method is 'true'.
''' </summary>
Public Class RelayCommand
    Implements ICommand

#Region "Fields"

    Private ReadOnly _execute As Action(Of Object)
    Private ReadOnly _canExecute As Predicate(Of Object)

#End Region

#Region "Constructors"

    ''' <summary>
    ''' Creates a new command that can always execute.
    ''' </summary>
    ''' <param name="execute">The execution logic.</param>
    Public Sub New(ByVal execute As Action(Of Object))
        Me.New(execute, Nothing)
    End Sub

    ''' <summary>
    ''' Creates a new command.
    ''' </summary>
    ''' <param name="execute">The execution logic.</param>
    ''' <param name="canExecute">The execution status logic.</param>
    Public Sub New(ByVal execute As Action(Of Object), ByVal canExecute As Predicate(Of Object))
        If execute Is Nothing Then
            Throw New ArgumentNullException("execute")
        End If

        _execute = execute
        _canExecute = canExecute
    End Sub

#End Region

#Region "ICommand Members"

    <DebuggerStepThrough()>
    Public Function CanExecute(ByVal parameter As Object) As Boolean Implements ICommand.CanExecute
        Return If(_canExecute Is Nothing, True, _canExecute(parameter))
    End Function

    Public Custom Event CanExecuteChanged As EventHandler Implements ICommand.CanExecuteChanged
        AddHandler(ByVal value As EventHandler)
            AddHandler CommandManager.RequerySuggested, value
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
            RemoveHandler CommandManager.RequerySuggested, value
        End RemoveHandler
        RaiseEvent(ByVal sender As System.Object, ByVal e As System.EventArgs)
        End RaiseEvent
    End Event

    Public Sub Execute(ByVal parameter As Object) Implements ICommand.Execute
        _execute(parameter)
    End Sub

#End Region

End Class

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

    Private _currentUsername As String = "Donald J Trump"
    Private _targetUsername As String = "Franz Gruber"
    Private _synced As Boolean = False
    Private _keys As List(Of String)
    Private _memesDT As DataTable
    Private _columnFilterGenericString As String
    Private _bitFields As List(Of String)
    Private _columnFilterTable As DataTable
    Private _currentColumnFilterText As String
    Private _currentColumnFilter As String = ALL_VALUE
    Private _currentCustomFilterText As String
    Private _apiKey As String = "31E2B259-343B-4752-8930-5D02AE1F352A"
    Public Sub New()
        '"MemeKey":1,"Ticker":"SWAG","Name":"Swag Baby","FullImageURL":"http://i.imgur.com/SYluBOh.gif","LZValue":0.0000,
        '"CurrentSharePrice":0.0000,"Likes":27,"Dislikes":8,"UserName":"OriginalScrubLord",
        '"CreateDate""/Date(1450187533410)/","Headwinds":0,"Tailwinds":0,"RND":0,"Marketing":0,"CurrentOrders":[],
        '"CurrentOwners":[{"QtyOwned":9500,"userKey":1,"UserName":"OriginalScrubLord"},{"QtyOwned":500,"userKey":1133,
        '"UserName":"Twincam"}]}
        _memesDT = New DataTable
        _memesDT.Columns.Add("MemeKey", GetType(String))
        _memesDT.Columns.Add("Ticker", GetType(String))
        _memesDT.Columns.Add("Name", GetType(String))
        _memesDT.Columns.Add("FullImageURL", GetType(String))
        _memesDT.Columns.Add("LZValue", GetType(Double))
        _memesDT.Columns.Add("CurrentSharePrice", GetType(Double))
        _memesDT.Columns.Add("Likes", GetType(Integer))
        _memesDT.Columns.Add("Dislikes", GetType(Integer))
        _memesDT.Columns.Add("NetLikes", GetType(Integer))
        _memesDT.Columns.Add("UserName", GetType(String))
        _memesDT.Columns.Add("CreateDate", GetType(Date))
        _memesDT.Columns.Add("Headwinds", GetType(Integer))
        _memesDT.Columns.Add("Tailwinds", GetType(Integer))
        _memesDT.Columns.Add("NetWinds", GetType(Integer))
        _memesDT.Columns.Add("EPSEffect", GetType(Double))
        _memesDT.Columns.Add("RND", GetType(Integer))
        _memesDT.Columns.Add("Marketing", GetType(Integer))
        _memesDT.Columns.Add("CurrentOwners", GetType(List(Of CurrentOwner)))
        _memesDT.Columns.Add("CurrentOrders", GetType(List(Of CurrentOrder)))
        _memesDT.Columns.Add("ownersString", GetType(String))
        _memesDT.Columns.Add("YourValue", GetType(Double))
        _memesDT.Columns.Add("TargetValue", GetType(Double))

        CreateTableOfColumnFilters()
        FireKeyDownload(_apiKey)

    End Sub

#Region "Properties"
    Public ReadOnly Property RefreshAllText As String
        Get
            If _synced Then
                Return "Refresh All"
            Else
                Return "Download Memes"
            End If
        End Get
    End Property
    Public Property CurrentUsername As String
        Get
            Return _currentUsername
        End Get
        Set(value As String)
            _currentUsername = value
        End Set
    End Property

    Public Property TargetUsername As String
        Get
            Return _targetUsername
        End Get
        Set(value As String)
            _targetUsername = value
        End Set
    End Property

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

    Public Property CurrentCustomFilterText As String
        Get
            Return _currentCustomFilterText
        End Get
        Set(value As String)
            _currentCustomFilterText = value

            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("CurrentCustomFilterText"))
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
#End Region
#Region "API Methods"

    Public Async Function FireKeyDownload(apiKey As String) As Task(Of List(Of String))
        Dim keysPage As String = "http://www.lolzzz.net/LZAPI/GetMemeKeys?apikey=" & apiKey
        Dim k As String = Await DownloadPageAsync(keysPage)
        k = k.Substring(1, k.Length - 2)
        Keys = New List(Of String)(k.Split(","))
        Return Keys
    End Function

    Public Sub DownloadMemes(apiKey As String, keys As List(Of String))
        For Each key As String In keys
            FireMemeDownload(apiKey, key)
        Next
    End Sub

    Public Async Sub FireMemeDownload(apiKey As String, memekey As String)
        Dim getMemeByKeyPage As String = "http://www.lolzzz.net/LZAPI/GetMemeByKey?apikey=31E2B259-343B-4752-8930-5D02AE1F352A&memekey=" & memekey
        Dim memeJson As String = Await DownloadPageAsync(getMemeByKeyPage)
        Dim meme As Meme = JsonConvert.DeserializeObject(Of Meme)(memeJson)
        Dim foundMemes As DataRow() = _memesDT.Select("MemeKey = '" & meme.MemeKey.ToString & "'")
        For Each foundMeme As DataRow In foundMemes
            _memesDT.Rows.Remove(foundMeme)
        Next
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

#End Region

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
        dr("NetLikes") = m.Likes - m.Dislikes
        dr("UserName") = m.UserName
        dr("CreateDate") = m.CreateDate
        dr("Headwinds") = m.Headwinds
        dr("Tailwinds") = m.Tailwinds
        dr("NetWinds") = m.Tailwinds - m.Headwinds
        dr("EPSEffect") = 0.3 * (m.Tailwinds - m.Headwinds)
        dr("RND") = m.RND
        dr("Marketing") = m.Marketing
        dr("CurrentOwners") = m.CurrentOwners.OrderByDescending(Function(x) x.QtyOwned).ToList
        dr("CurrentOrders") = m.CurrentOrders.OrderByDescending(Function(x) x.Price).ToList
        dr("ownersString") = OwnersToString(m.CurrentOwners)
        dr("YourValue") = OwnersToShares(CurrentUsername, m.CurrentOwners) * m.LZValue
        dr("TargetValue") = OwnersToShares(TargetUsername, m.CurrentOwners) * m.LZValue
        Return dr
    End Function

    Public Function OwnersToString(m As CurrentOwner()) As String
        Dim ret As String = ""
        Dim o = From mo In m
                Order By mo.QtyOwned Descending, mo.UserName
                Select mo
        For i = 0 To o.Count - 1
            ret &= o(i).UserName & ","
        Next
        Return ret
    End Function

    Public Function OwnersToShares(ownerName As String, memeOwners As CurrentOwner()) As Integer
        For Each owner As CurrentOwner In memeOwners
            If owner.UserName = ownerName Then
                Return owner.QtyOwned
            End If
        Next
        Return 0
    End Function

#Region "Setup"
    Private Sub CreateTableOfColumnFilters()
        Dim newRow As DataRow
        Dim columnsDT As DataTable = New DataTable()

        columnsDT.Columns.Add(New DataColumn("ColumnName"))
        columnsDT.Columns.Add(New DataColumn("ID"))
        _columnFilterGenericString = ""
        _bitFields = New List(Of String)

        For Each dc As DataColumn In _memesDT.Columns
            If dc.ColumnName = "ownersString" Then
                Continue For
            End If
            newRow = columnsDT.NewRow
            newRow("ID") = dc.ColumnName

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
#End Region

#Region "Filter Methods"

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
                ElseIf _currentColumnFilter = "CurrentOwners" Then
                    columnFilterString &= String.Format(" ownersString LIKE '%{1}%' ", New String() {_currentColumnFilter, fixedColumnFilterText})
                Else
                    columnFilterString &= String.Format(" Convert([{0}], 'System.String') LIKE '%{1}%' ", New String() {_currentColumnFilter, fixedColumnFilterText})
                End If
            End If

            AppendFilterCriteria(columnFilterString, filter)
        End If

        AppendFilterCriteria(CurrentCustomFilterText, filter)

        MemesDV.RowFilter = filter

        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("MemesDV"))
    End Sub

    Private Sub AppendFilterCriteria(ByVal filterCondition As String, ByRef resultingFilterString As String)
        If resultingFilterString.Length > 0 Then
            resultingFilterString &= " AND "
        End If

        resultingFilterString &= filterCondition
    End Sub
#End Region

#Region "Commands"
#Region "Apply Custom Filter"

    Private _applyFilterCommand As RelayCommand

    Public ReadOnly Property ApplyFilterCommand As ICommand
        Get
            If _applyFilterCommand Is Nothing Then
                _applyFilterCommand = New RelayCommand(AddressOf ApplyFilters)
            End If
            Return _applyFilterCommand
        End Get
    End Property

#End Region
#Region "Open All"

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
#End Region

#Region "Refresh"

    Private _refreshCommand As RelayCommand

    Public ReadOnly Property RefreshCommand As ICommand
        Get
            If _refreshCommand Is Nothing Then
                _refreshCommand = New RelayCommand(AddressOf Refresh)
            End If
            Return _refreshCommand
        End Get
    End Property

    Private Async Sub Refresh(arg As Object)
        _synced = True
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(NameOf(RefreshAllText)))
        _memesDT.Rows.Clear()
        Await FireKeyDownload(_apiKey)

        DownloadMemes(_apiKey, Keys)
    End Sub

    Private _refreshViewedCommand As RelayCommand

    Public ReadOnly Property RefreshViewedCommand As ICommand
        Get
            If _refreshViewedCommand Is Nothing Then
                _refreshViewedCommand = New RelayCommand(AddressOf RefreshViewed)
            End If
            Return _refreshViewedCommand
        End Get
    End Property

    Private Sub RefreshViewed(arg As Object)
        For mk = 0 To MemesDV.Count - 1
            FireMemeDownload(_apiKey, MemesDV.Item(mk)("MemeKey"))
        Next
    End Sub

End Class
#End Region
#End Region

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
    Public Property Value As Double
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

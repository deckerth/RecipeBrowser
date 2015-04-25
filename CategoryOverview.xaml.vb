' Die Elementvorlage für die Seite "Elemente" ist unter http://go.microsoft.com/fwlink/?LinkId=234233 dokumentiert.

''' <summary>
''' Eine Seite, auf der eine Auflistung von Elementvorschauen angezeigt wird.  In der geteilten Anwendung wird diese Seite
''' verwendet, um eine der verfügbaren Gruppen anzuzeigen und auszuwählen.
''' </summary>
Public NotInheritable Class CategoryOverview
    Inherits Page

    ''' <summary>
    ''' NavigationHelper wird auf jeder Seite zur Unterstützung bei der Navigation verwendet und 
    ''' Verwaltung der Prozesslebensdauer
    ''' </summary>
    Public ReadOnly Property NavigationHelper As Common.NavigationHelper
        Get
            Return Me._navigationHelper
        End Get
    End Property
    Private _navigationHelper As Common.NavigationHelper

    ''' <summary>
    ''' Dies kann in ein stark typisiertes Anzeigemodell geändert werden.
    ''' </summary>
    Public ReadOnly Property DefaultViewModel As Common.ObservableDictionary
        Get
            Return Me._defaultViewModel
        End Get
    End Property
    Private _defaultViewModel As New Common.ObservableDictionary()    

    Public Sub New()
        InitializeComponent()
        Me._navigationHelper = New Common.NavigationHelper(Me)
        AddHandler Me._navigationHelper.LoadState, AddressOf NavigationHelper_LoadState
    End Sub

    ''' <summary>
    ''' Füllt die Seite mit Inhalt auf, der bei der Navigation übergeben wird.  Gespeicherte Zustände werden ebenfalls
    ''' bereitgestellt, wenn eine Seite aus einer vorherigen Sitzung neu erstellt wird.
    ''' </summary>
    ''' <param name="sender">
    ''' Die Quelle des Ereignisses, normalerweise <see cref="NavigationHelper"/>
    ''' </param>
    ''' <param name="e">Ereignisdaten, die die Navigationsparameter bereitstellen, die an
    ''' <see cref="Frame.Navigate"/> übergeben wurde, als diese Seite ursprünglich angefordert wurde und
    ''' ein Wörterbuch des Zustands, der von dieser Seite während einer früheren
    ''' beibehalten wurde.  Der Zustand ist beim ersten Aufrufen einer Seite NULL.</param>
    Private Async Sub NavigationHelper_LoadState(sender As Object, e As Common.LoadStateEventArgs)
        ' TODO: Me.DefaultViewModel("Items") eine bindbare Auflistung von Elementen zuweisen
        Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

        Me.DefaultViewModel("Items") = categories.Folders

        Dim searchSuggestions = New Windows.ApplicationModel.Search.LocalContentSuggestionSettings()
        searchSuggestions.Enabled = True
        Dim rootFolder = Await categories.GetStorageFolderAsync("")
        searchSuggestions.Locations.Add(rootFolder)
        RecipeSearchBox.SetLocalContentSuggestionSettings(searchSuggestions)
        ChangeRootFolderButton.Label = App.Texts.GetString("ChangeRootFolder")
        backButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed
        itemGridView.SelectedItem = Nothing

    End Sub

#Region "NavigationHelper-Registrierung"

    ''' Die in diesem Abschnitt bereitgestellten Methoden werden einfach verwendet, um
    ''' damit NavigationHelper auf die Navigationsmethoden der Seite reagieren kann.
    ''' 
    ''' Platzieren Sie seitenspezifische Logik in Ereignishandlern für  
    ''' <see cref="Common.NavigationHelper.LoadState"/>
    ''' and <see cref="Common.NavigationHelper.SaveState"/>.
    ''' Der Navigationsparameter ist in der LoadState-Methode verfügbar 
    ''' zusätzlich zum Seitenzustand, der während einer früheren Sitzung beibehalten wurde.

    Protected Overrides Sub OnNavigatedTo(e As NavigationEventArgs)
        _navigationHelper.OnNavigatedTo(e)
    End Sub

    Protected Overrides Sub OnNavigatedFrom(e As NavigationEventArgs)
        _navigationHelper.OnNavigatedFrom(e)
    End Sub

#End Region

    Private Sub CategorySelected(sender As Object, e As ItemClickEventArgs) Handles itemGridView.ItemClick

        If e.ClickedItem IsNot Nothing Then
            Dim folders = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)
            If folders IsNot Nothing Then
                Dim category = (DirectCast(e.ClickedItem, RecipeFolder))
                Me.Frame.Navigate(GetType(RecipesPage), category.Name)
            End If
        End If

    End Sub

    Private Sub ShowFavorites(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(RecipesPage), Favorites.FolderName)
    End Sub

    Private Sub SearchBox_QuerySubmitted(sender As SearchBox, args As SearchBoxQuerySubmittedEventArgs)


        Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

        categories.SearchResultsFolder.SetSearchParameter("", args.QueryText)
        Me.Frame.Navigate(GetType(RecipesPage), SearchResults.FolderName)

    End Sub

    Private Async Sub ChangeRootFolder_Click(sender As Object, e As RoutedEventArgs)

        Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

        Await categories.ChangeRootFolder()

        NavigationHelper_LoadState(Nothing, Nothing)

    End Sub

    Private Sub NewCategory_Click(sender As Object, e As RoutedEventArgs) Handles NewCategory.Click

        Me.Frame.Navigate(GetType(DefineCategoryPage))

    End Sub

    Private Async Sub Edit_Category_Click(sender As Object, e As RoutedEventArgs) Handles EditCategory.Click

        If itemGridView.SelectedItem Is Nothing Then
            Dim msg = New Windows.UI.Popups.MessageDialog(App.Texts.GetString("SelectACategory"))
            Await msg.ShowAsync()
            Return
        End If

        Dim selectedCategory = DirectCast(itemGridView.SelectedItem, RecipeFolder)
        Me.Frame.Navigate(GetType(DefineCategoryPage), selectedCategory.Name)

    End Sub

End Class

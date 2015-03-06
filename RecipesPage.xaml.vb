Imports Windows.UI.Popups

' Die Elementvorlage "Geteilte Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=234234 dokumentiert.

''' <summary>
''' Eine Seite, auf der ein Gruppentitel, eine Liste mit Elementen innerhalb der Gruppe sowie Details für das
''' derzeit ausgewählte Element angezeigt werden.
''' </summary>
Public NotInheritable Class RecipesPage
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
        AddHandler Me._navigationHelper.SaveState, AddressOf NavigationHelper_SaveState
        AddHandler Me.itemListView.SelectionChanged, AddressOf ItemListView_SelectionChanged
        Me.NavigationHelper.GoBackCommand = New Common.RelayCommand(AddressOf Me.GoBack, AddressOf Me.CanGoBack)

        AddHandler Window.Current.SizeChanged, AddressOf Winow_SizeChanged
        Me.InvalidateVisualState()
    End Sub

    ''' <summary>
    ''' Wird aufgerufen, wenn ein Element innerhalb der Liste ausgewählt wird.
    ''' </summary>
    ''' <param name="sender">Die GridView, die das ausgewählte Element anzeigt.</param>
    ''' <param name="e">Ereignisdaten, die beschreiben, wie die Auswahl geändert wurde.</param>
    Private Async Sub ItemListView_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        Dim list = DirectCast(sender, Selector)
        Dim selectedItem = DirectCast(list.SelectedItem, Recipe)
        If selectedItem IsNot Nothing Then
            Await selectedItem.LoadRecipeAsync()
            RenderPageControl(selectedItem)
            RecipeViewer.Source = selectedItem.RenderedPage
        End If

        If Me.UsingLogicalPageNavigation() Then
            Me.InvalidateVisualState()
        End If
    End Sub

    ''' <summary>
    ''' Füllt die Seite mit Inhalt auf, der bei der Navigation übergeben wird.  Gespeicherte Zustände werden ebenfalls
    ''' bereitgestellt, wenn eine Seite aus einer vorherigen Sitzung neu erstellt wird.
    ''' </summary>
    ''' 
    ''' Die Quelle des Ereignisses, normalerweise <see cref="NavigationHelper"/>
    ''' 
    ''' <param name="e">Ereignisdaten, die die Navigationsparameter bereitstellen, die an
    ''' <see cref="Frame.Navigate"/> übergeben wurde, als diese Seite ursprünglich angefordert wurde und
    ''' ein Wörterbuch des Zustands, der von dieser Seite während einer früheren
    ''' beibehalten wurde.  Der Zustand ist beim ersten Aufrufen einer Seite NULL.</param>
    ''' 

    Private CurrentRecipeFolder As RecipeFolder

    Private Async Sub NavigationHelper_LoadState(sender As Object, e As Common.LoadStateEventArgs)
        ' TODO: Me.DefaultViewModel("Group") eine bindbare Gruppe zuweisen
        ' TODO: Me.DefaultViewModel("Items") eine Auflistung von bindbaren Elementen zuweisen
        Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

        Dim category = DirectCast(e.NavigationParameter, String)

        RemoveFromFavorites.IsEnabled = False
        AddToFavorites.IsEnabled = False
        ShowFavorites.IsEnabled = False
        RecipeSearchBox.IsEnabled = False
        backButton.IsEnabled = False
        changeSortOrder.IsEnabled = False
        OpenFile.IsEnabled = False

        pageControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed

        LoadProgress.Visibility = Windows.UI.Xaml.Visibility.Visible
        CurrentRecipeFolder = Await categories.GetFolderAsync(category)
        LoadProgress.Visibility = Windows.UI.Xaml.Visibility.Collapsed

        Me.DefaultViewModel("Group") = CurrentRecipeFolder
        Me.DefaultViewModel("Items") = CurrentRecipeFolder.Recipes
        pageTitle.Text = category

        If category = Favorites.FolderName Then
            RemoveFromFavorites.Visibility = Windows.UI.Xaml.Visibility.Visible
            AddToFavorites.Visibility = Windows.UI.Xaml.Visibility.Collapsed
            ShowFavorites.Visibility = Windows.UI.Xaml.Visibility.Collapsed
            RecipeSearchBox.Visibility = Windows.UI.Xaml.Visibility.Collapsed
            RemoveFromFavorites.IsEnabled = True
        Else
            RemoveFromFavorites.Visibility = Windows.UI.Xaml.Visibility.Collapsed
            AddToFavorites.IsEnabled = True
            ShowFavorites.IsEnabled = True
        End If

        OpenFile.IsEnabled = True
        backButton.IsEnabled = True
        changeSortOrder.IsEnabled = True

        If category = SearchResults.FolderName Then
            RecipeSearchBox.QueryText = categories.SearchResultsFolder.LastSearchString
        Else
            RecipeSearchBox.IsEnabled = True
        End If

        If CurrentRecipeFolder.Folder IsNot Nothing Then
            Dim searchSuggestions = New Windows.ApplicationModel.Search.LocalContentSuggestionSettings()
            searchSuggestions.Enabled = True
            searchSuggestions.Locations.Add(CurrentRecipeFolder.Folder)
            RecipeSearchBox.SetLocalContentSuggestionSettings(searchSuggestions)
        End If

        If e.PageState Is Nothing Then
            ' Wenn es sich hierbei um eine neue Seite handelt, das erste Element automatisch auswählen, außer wenn
            ' logische Seitennavigation verwendet wird (weitere Informationen in der #Region zur logischen Seitennavigation unten).
            If Not Me.UsingLogicalPageNavigation() AndAlso Me.itemsViewSource.View IsNot Nothing Then
                Me.itemsViewSource.View.MoveCurrentToFirst()
            End If
        Else

            ' Den zuvor gespeicherten Zustand wiederherstellen, der dieser Seite zugeordnet ist
            If e.PageState.ContainsKey("SelectedItem") AndAlso Me.itemsViewSource.View IsNot Nothing Then
                ' TODO: Me.itemsViewSource.View.MoveCurrentTo() mit dem ausgewählten
                '       Element aufrufen, wie durch den Wert von pageState("SelectedItem") angegeben
                ' Den zuvor gespeicherten Zustand wiederherstellen, der dieser Seite zugeordnet ist
                Dim selectedItem = Await categories.GetRecipeAsync(category, DirectCast(e.PageState("SelectedItem"), String))
                Me.itemsViewSource.View.MoveCurrentTo(selectedItem)
                If selectedItem IsNot Nothing Then
                    Await selectedItem.LoadRecipeAsync()
                    RenderPageControl(selectedItem)
                    RecipeViewer.Source = selectedItem.RenderedPage
                End If

            End If
        End If
    End Sub

    Private Sub RenderPageControl(ByRef CurrentRecipe As Recipe)

        If CurrentRecipe Is Nothing Then
            pageControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed
        Else
            If CurrentRecipe.NoOfPages > 1 Then
                pageNumber.Text = CurrentRecipe.CurrentPage.ToString + "/" + CurrentRecipe.NoOfPages.ToString
                pageControl.Visibility = Windows.UI.Xaml.Visibility.Visible
                If CurrentRecipe.CurrentPage = 1 Then
                    prevPage.IsEnabled = False
                Else
                    prevPage.IsEnabled = True

                End If
                If CurrentRecipe.CurrentPage = CurrentRecipe.NoOfPages Then
                    nextPage.IsEnabled = False
                Else
                    nextPage.IsEnabled = True
                End If
            Else
                pageControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed
            End If
        End If

    End Sub

    ''' <summary>
    ''' Behält den dieser Seite zugeordneten Zustand bei, wenn die Anwendung angehalten oder
    ''' die Seite im Navigationscache verworfen wird.  Die Werte müssen den Serialisierungsanforderungen
    ''' von <see cref="Common.SuspensionManager.SessionState"/> entsprechen.
    ''' </summary>
    ''' <param name="sender">
    ''' Die Quelle des Ereignisses, normalerweise <see cref="NavigationHelper"/>
    ''' </param>
    ''' <param name="e">Ereignisdaten, die ein leeres Wörterbuch zum Auffüllen bereitstellen 
    ''' serialisierbarer Zustand.</param>
    Private Sub NavigationHelper_SaveState(sender As Object, e As Common.SaveStateEventArgs)
        If Me.itemsViewSource.View IsNot Nothing Then
            Dim selectedItem As Object = Me.itemsViewSource.View.CurrentItem
            ' TODO: Einen serialisierbaren Navigationsparameter ableiten und ihn
            '       pageState("SelectedItem")
            If selectedItem IsNot Nothing Then
                Dim itemTitle = (DirectCast(selectedItem, Recipe)).Name
                e.PageState("SelectedItem") = itemTitle
            End If

        End If
    End Sub

#Region "Logische Seitennavigation"

    ' Die geteilte Seite ist so entworfen, dass wenn Fenster nicht genug Raum hat, um sowohl
    ' die Liste als auch die Details anzuzeigen, nur jeweils ein Bereich angezeigt wird.
    '
    ' All dies wird mit einer einzigen physischen Seite implementiert, die zwei logische Seiten darstellen
    ' kann.  Mit dem nachfolgenden Code wird dieses Ziel erreicht, ohne dass der Benutzer aufmerksam gemacht wird auf den
    ' Unterschied.

    Private Const MinimumWidthForSupportingTwoPanes As Integer = 768

    ''' <summary>
    ''' Wird aufgerufen, um zu bestimmen, ob die Seite als eine logische Seite oder zwei agieren soll.
    ''' </summary>
    ''' <returns>"True", wenn der fragliche Ansichtszustand Hochformat oder angedockt ist, "false"
    ''' in anderen Fällen.</returns>
    Private Function UsingLogicalPageNavigation() As Boolean
        Return Window.Current.Bounds.Width < MinimumWidthForSupportingTwoPanes
    End Function

    ''' <summary>
    ''' Mit der Änderung der Fenstergröße aufgerufen
    ''' </summary>
    ''' <param name="sender">Das aktuelle Fenster</param>
    ''' <param name="e">Ereignisdaten, die die neue Größe des Fensters beschreiben</param>
    Private Sub Winow_SizeChanged(sender As Object, e As Windows.UI.Core.WindowSizeChangedEventArgs)
        Me.InvalidateVisualState()
    End Sub

    Private Sub InvalidateVisualState()
        Dim visualState As String = DetermineVisualState()
        VisualStateManager.GoToState(Me, visualState, False)
        Me.NavigationHelper.GoBackCommand.RaiseCanExecuteChanged()
    End Sub

    Private Function CanGoBack() As Boolean
        If Me.UsingLogicalPageNavigation() AndAlso Me.itemListView.SelectedItem IsNot Nothing Then
            Return True
        Else
            Return Me.NavigationHelper.CanGoBack()
        End If
    End Function
    Private Sub GoBack()
        If Me.UsingLogicalPageNavigation() AndAlso Me.itemListView.SelectedItem IsNot Nothing Then
            ' Wenn die logische Seitennavigation wirksam ist und ein ausgewähltes Element vorliegt, werden die
            ' Details dieses Elements aktuell angezeigt.  Beim Aufheben der Auswahl wird die
            ' Elementliste wieder aufgerufen.  Aus Sicht des Benutzers ist dies eine logische
            ' Rückwärtsnavigation.
            Me.itemListView.SelectedItem = Nothing
        Else
            Me.NavigationHelper.GoBack()
        End If
    End Sub

    ''' <summary>
    ''' Wird aufgerufen, um den Namen des visuellen Zustands zu bestimmen, der dem Ansichtszustand einer Anwendung
    ''' entspricht.
    ''' </summary>
    ''' <returns>Der Name des gewünschten visuellen Zustands.  Dieser ist identisch mit dem Namen des
    ''' Ansichtszustands, außer wenn ein ausgewähltes Element im Hochformat und in der angedockten Ansicht vorliegt, wobei
    ''' diese zusätzliche logische Seite durch Hinzufügen des Suffix _Detail dargestellt wird.</returns>
    ''' <remarks></remarks>
    Private Function DetermineVisualState() As String

        ' ACHTUNG: Das hat gefehlt !!!
        If (Not UsingLogicalPageNavigation()) Then
            Return "PrimaryView"
        End If

        ' Den Aktivierungszustand der Schaltfläche "Zurück" aktualisieren, wenn der Ansichtszustand geändert wird
        Dim logicalPageBack As Boolean = Me.UsingLogicalPageNavigation() AndAlso Me.itemListView.SelectedItem IsNot Nothing

        If logicalPageBack Then Return "SinglePane_Detail"
        Return "SinglePane"
    End Function

#End Region

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

    Private Sub SortNameAscending_Click(sender As Object, e As RoutedEventArgs)

        CurrentRecipeFolder.SetSortOrder(RecipeFolder.SortOrder.ByNameAscending)
        Me.DefaultViewModel("Group") = CurrentRecipeFolder
        Me.DefaultViewModel("Items") = CurrentRecipeFolder.Recipes

    End Sub

    Private Sub SortDateDecending_Click(sender As Object, e As RoutedEventArgs)

        CurrentRecipeFolder.SetSortOrder(RecipeFolder.SortOrder.ByDateDescending)
        Me.DefaultViewModel("Group") = CurrentRecipeFolder
        Me.DefaultViewModel("Items") = CurrentRecipeFolder.Recipes

    End Sub

    Private Sub DisableControls()

        AddToFavorites.IsEnabled = False
        ShowFavorites.IsEnabled = False
        backButton.IsEnabled = False
        actionProgress.IsActive = True
        RecipeSearchBox.IsEnabled = False
        nextPage.IsEnabled = False
        prevPage.IsEnabled = False

    End Sub

    Private Sub EnableControls(ByRef currentRecipe As Recipe)

        AddToFavorites.IsEnabled = True
        ShowFavorites.IsEnabled = True
        backButton.IsEnabled = True
        actionProgress.IsActive = False
        If CurrentRecipeFolder.Name <> SearchResults.FolderName Then
            RecipeSearchBox.IsEnabled = True
        End If
        RenderPageControl(currentRecipe)
    End Sub

    Private Async Sub AddToFavorites_Click(sender As Object, e As RoutedEventArgs) Handles AddToFavorites.Click

        If Me.itemsViewSource.View IsNot Nothing Then
            Dim selectedItem As Object = Me.itemsViewSource.View.CurrentItem
            If selectedItem IsNot Nothing Then
                DisableControls()
                Dim recipe = DirectCast(selectedItem, Recipe)
                Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)
                Await categories.FavoriteFolder.AddRecipeAsync(recipe)
                EnableControls(recipe)
            End If
        End If

    End Sub


    Private Sub ShowFavorites_Click(sender As Object, e As RoutedEventArgs) Handles ShowFavorites.Click
        Me.Frame.Navigate(GetType(RecipesPage), Favorites.FolderName)
    End Sub

    Private Sub RemoveFromFavorites_Click(sender As Object, e As RoutedEventArgs) Handles RemoveFromFavorites.Click
        If Me.itemsViewSource.View IsNot Nothing Then
            Dim selectedItem As Object = Me.itemsViewSource.View.CurrentItem
            If selectedItem IsNot Nothing Then
                Dim recipe = DirectCast(selectedItem, Recipe)
                Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)
                categories.FavoriteFolder.DeleteRecipe(recipe)
                RecipeViewer.Source = Nothing
                RenderPageControl(Nothing)
            End If
        End If

    End Sub

    Private Sub SearchBox_QuerySubmitted(sender As SearchBox, args As SearchBoxQuerySubmittedEventArgs)

        Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

        categories.SearchResultsFolder.SetSearchParameter(CurrentRecipeFolder.Name, args.QueryText)
        Me.Frame.Navigate(GetType(RecipesPage), SearchResults.FolderName)

    End Sub

    Private Async Sub OpenRecipe_Click(sender As Object, e As RoutedEventArgs)

        If Me.itemsViewSource.View IsNot Nothing Then
            Dim selectedItem As Object = Me.itemsViewSource.View.CurrentItem
            If selectedItem IsNot Nothing Then
                Dim recipe = DirectCast(selectedItem, Recipe)
                DisableControls()
                Await Windows.System.Launcher.LaunchFileAsync(recipe.File)
                EnableControls(recipe)
            End If
        End If

    End Sub

    Private Cancelled As Boolean

    Private Async Sub DeleteRecipe_Click(sender As Object, e As RoutedEventArgs) Handles deleteRecipe.Click

        Dim messageDialog = New Windows.UI.Popups.MessageDialog(App.Texts.GetString("DoYouWantToDelete"))

        ' Add buttons and set their callbacks
        messageDialog.Commands.Add(New UICommand(App.Texts.GetString("Yes"), Sub(command)
                                                                                 Cancelled = False
                                                                             End Sub))

        messageDialog.Commands.Add(New UICommand(App.Texts.GetString("No"), Sub(command)
                                                                                Cancelled = True
                                                                            End Sub))

        ' Set the command that will be invoked by default
        messageDialog.DefaultCommandIndex = 1

        ' Set the command to be invoked when escape is pressed
        messageDialog.CancelCommandIndex = 1

        Await messageDialog.ShowAsync()

        If Cancelled Then
            Return
        End If

        Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

        If Me.itemsViewSource.View IsNot Nothing Then
            Dim selectedItem As Object = Me.itemsViewSource.View.CurrentItem
            If selectedItem IsNot Nothing Then
                RecipeViewer.Source = Nothing
                DisableControls()
                Dim recipe = DirectCast(selectedItem, Recipe)
                Await categories.DeleteRecipeAsync(recipe)
                EnableControls(Nothing)
            End If
        End If

    End Sub

    Private Async Sub GotoPreviousPage(sender As Object, e As RoutedEventArgs) Handles prevPage.Click

        If Me.itemsViewSource.View IsNot Nothing Then
            Dim selectedItem As Object = Me.itemsViewSource.View.CurrentItem
            If selectedItem IsNot Nothing Then
                Dim recipe = DirectCast(selectedItem, Recipe)
                DisableControls()
                Await recipe.PreviousPage()
                RecipeViewer.Source = recipe.RenderedPage
                EnableControls(recipe)
            End If
        End If

    End Sub

    Private Async Sub GotoNextPage(sender As Object, e As RoutedEventArgs) Handles nextPage.Click

        If Me.itemsViewSource.View IsNot Nothing Then
            Dim selectedItem As Object = Me.itemsViewSource.View.CurrentItem
            If selectedItem IsNot Nothing Then
                Dim recipe = DirectCast(selectedItem, Recipe)
                DisableControls()
                Await recipe.NextPage()
                RecipeViewer.Source = recipe.RenderedPage
                EnableControls(recipe)
            End If
        End If
    End Sub

End Class

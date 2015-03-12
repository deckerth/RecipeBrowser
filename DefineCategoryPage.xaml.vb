Imports Windows.UI.Popups

' Die Elementvorlage "Standardseite" ist unter http://go.microsoft.com/fwlink/?LinkId=234237 dokumentiert.

''' <summary>
''' Eine Standardseite mit Eigenschaften, die die meisten Anwendungen aufweisen.
''' </summary>
Public NotInheritable Class DefineCategoryPage
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
    Private Sub NavigationHelper_LoadState(sender As Object, e As Common.LoadStateEventArgs)

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


    Private SelectedImage As Windows.Storage.StorageFile

    Private Async Sub LoadCategoryImage_Click(sender As Object, e As RoutedEventArgs)

        Dim openPicker = New Windows.Storage.Pickers.FileOpenPicker()
        openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary
        openPicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail

        ' Filter to include a sample subset of file types.
        openPicker.FileTypeFilter.Clear()
        openPicker.FileTypeFilter.Add(".png")

        ' Open the file picker.
        Dim file = Await openPicker.PickSingleFileAsync()

        ' file is null if user cancels the file picker.
        If file IsNot Nothing Then

            ' Open a stream for the selected file.
            Dim fileStream = Await file.OpenAsync(Windows.Storage.FileAccessMode.Read)

            ' Set the image source to the selected bitmap.
            Dim BitmapImage = New Windows.UI.Xaml.Media.Imaging.BitmapImage()

            BitmapImage.SetSource(fileStream)
            SelectedImage = file
            CategoryImage.Source = BitmapImage
        End If

    End Sub

    Class StringContainer
        Public content As New String("")
    End Class

    Private Function CategoryNameIsValid(ByRef errorMessage As StringContainer) As Boolean

        If CategoryName.Text Is Nothing Or CategoryName.Text = "" Then
            errorMessage.content = "Der Kategoriename darf nicht leer sein"
            Return False
        End If

        Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

        If categories.GetFolder(CategoryName.Text) IsNot Nothing Then
            errorMessage.content = "Die angegebene Kategorie existiert bereits"
            Return False
        End If

        errorMessage.content = ""
        Return True

    End Function

    Private Async Sub SaveCategory_Click(sender As Object, e As RoutedEventArgs) Handles SaveCategory.Click

        Dim errorMessage As New StringContainer()

        If CategoryNameIsValid(errorMessage) Then
            Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)
            Await categories.CreateCategoryAsync(CategoryName.Text, SelectedImage)
            NavigationHelper.GoBack()
        Else
            Dim messageDialog = New Windows.UI.Popups.MessageDialog(errorMessage.content)
            Await messageDialog.ShowAsync()
            Return
        End If

    End Sub

    Private saveNecessary As Boolean

    Private Sub CategoryName_TextChanged(sender As Object, e As TextChangedEventArgs) Handles CategoryName.TextChanged

        Dim errorMessage As New StringContainer()

        If CategoryNameIsValid(errorMessage) Then
            CategoryName.SetValue(BorderBrushProperty, New SolidColorBrush(Windows.UI.Colors.Black))
            SaveCategory.IsEnabled = True
        Else
            CategoryName.SetValue(BorderBrushProperty, New SolidColorBrush(Windows.UI.Colors.Red))
            SaveCategory.IsEnabled = False
        End If

        ErrorMessageDisplay.Text = errorMessage.content

        saveNecessary = True
    End Sub

    Private saveRequested As Boolean
    Private cancelled As Boolean

    Private Async Sub GoBack_Click(sender As Object, e As RoutedEventArgs) Handles backButton.Click

        If saveNecessary And CategoryName.Text <> "" Then
            Dim messageDialog = New Windows.UI.Popups.MessageDialog("Wollen Sie die Änderungen speichern?")
            ' Add buttons and set their callbacks
            messageDialog.Commands.Add(New UICommand(App.Texts.GetString("Yes"), Sub(command)
                                                                                     saveRequested = True
                                                                                 End Sub))
            messageDialog.Commands.Add(New UICommand(App.Texts.GetString("No"), Sub(command)
                                                                                    saveRequested = False
                                                                                End Sub))
            messageDialog.Commands.Add(New UICommand(App.Texts.GetString("Cancel"), Sub(command)
                                                                                        saveRequested = False
                                                                                        cancelled = True
                                                                                    End Sub))

            ' Set the command that will be invoked by default
            messageDialog.DefaultCommandIndex = 0
            ' Set the command to be invoked when escape is pressed
            messageDialog.CancelCommandIndex = 2

            Await messageDialog.ShowAsync()

            If cancelled Then
                Return
            End If

            If saveRequested Then
                SaveCategory_Click(sender, e)
                Return
            End If
        End If

        NavigationHelper.GoBack()

    End Sub
End Class

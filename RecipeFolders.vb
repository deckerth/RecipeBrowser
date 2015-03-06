Imports Windows.UI.Popups

Public Class RecipeFolders

    Private _Folders As New ObservableCollection(Of RecipeFolder)()
    Public ReadOnly Property Folders As ObservableCollection(Of RecipeFolder)
        Get
            Return _Folders
        End Get
    End Property

    Public Property FavoriteFolder As Favorites
    Public Property SearchResultsFolder As SearchResults

    Private initialized As Boolean

    Public Function ContentLoaded() As Boolean
        Return initialized
    End Function

    Private rootFolder As Windows.Storage.StorageFolder

    Public Async Function GetFolderAsync(name As String) As Task(Of RecipeFolder)

        Dim folder = GetFolder(name)

        If folder IsNot Nothing AndAlso Not folder.ContentLoaded Then
            Await folder.LoadAsync()
        End If

        Return folder

    End Function


    Public Function GetFolder(name As String) As RecipeFolder

        If name = FavoriteFolder.Name Then
            Return FavoriteFolder
        ElseIf name = SearchResults.FolderName Then
            Return SearchResultsFolder
        Else
            Dim matches = _Folders.Where(Function(otherFolder) otherFolder.Name.Equals(name))
            If matches.Count() = 1 Then
                Dim folder = matches.First()
                Return folder
            End If
        End If

        Return Nothing

    End Function

    Public Async Function GetRootFolderAsync() As Task(Of Windows.Storage.StorageFolder)

        Dim localSettings = Windows.Storage.ApplicationData.Current.LocalSettings

        Dim mruToken = localSettings.Values("RootFolder")

        If String.IsNullOrEmpty(mruToken) Then
            Return Nothing
        Else
            Try
                Return Await Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList.GetFolderAsync(mruToken)
            Catch ex As Exception
                Return Nothing
            End Try
        End If

    End Function


    Public Async Function GetStorageFolderAsync(name As String) As Task(Of Windows.Storage.StorageFolder)

        If name.Equals("") Then
            Dim recipes = rootFolder
            Return recipes
        Else
            Dim matches = _Folders.Where(Function(otherFolder) otherFolder.Name.Equals(name))
            If matches.Count() = 1 Then
                Dim folder = matches.First()
                If Not folder.ContentLoaded Then
                    Await folder.LoadAsync()
                End If
                Return folder.Folder
            End If
        End If

        Return Nothing

    End Function

    Public Async Function GetRecipeAsync(category As String, title As String) As Task(Of Recipe)

        Dim folder = GetFolder(category)
        If folder IsNot Nothing Then
            Return Await folder.GetRecipeAsync(category, title)
        End If
        Return Nothing

    End Function

    Public Async Function LoadAsync() As Task

        rootFolder = Await GetRootFolderAsync()

        If rootFolder Is Nothing Then
            Return
        End If

        Dim images As Windows.Storage.StorageFolder
        Dim folders As IReadOnlyCollection(Of Windows.Storage.StorageFolder)

        Try
            images = Await rootFolder.GetFolderAsync("_folders")
        Catch ex As Exception
        End Try

        Try
            folders = Await rootFolder.GetFoldersAsync()
        Catch ex As Exception
            Return
        End Try

        ' Remove all temporary files
        Try
            Dim tempFolder = Windows.Storage.ApplicationData.Current.LocalFolder
            Dim files = Await tempFolder.GetFilesAsync()
            For Each file In files
                If file.Name.ToUpper.EndsWith(".PDF") Then
                    Await file.DeleteAsync()
                End If
            Next
        Catch ex As Exception
        End Try

        _Folders.Clear()

        For Each folder In folders
            If Not folder.DisplayName.StartsWith("_") Then
                Dim category = New RecipeFolder

                category.Name = folder.DisplayName
                category.Folder = folder

                If images IsNot Nothing Then
                    Try
                        Dim categoryImage = Await images.GetFileAsync(folder.DisplayName + ".png")
                        If categoryImage IsNot Nothing Then
                            ' Open a stream for the selected file.
                            Dim fileStream = Await categoryImage.OpenAsync(Windows.Storage.FileAccessMode.Read)
                            ' Set the image source to the selected bitmap.
                            category.Image = New Windows.UI.Xaml.Media.Imaging.BitmapImage()
                            Await category.Image.SetSourceAsync(fileStream)
                        End If
                    Catch
                    End Try
                End If

                _Folders.Add(category)
            End If
        Next

        FavoriteFolder = New Favorites
        SearchResultsFolder = New SearchResults

        initialized = True
    End Function

    Async Function ChangeRootFolder() As Task

        Dim localSettings = Windows.Storage.ApplicationData.Current.LocalSettings

        Dim mruToken = localSettings.Values("RootFolder")
        Dim folder As Windows.Storage.StorageFolder

        Dim openPicker = New Windows.Storage.Pickers.FolderPicker()
        openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
        openPicker.CommitButtonText = App.Texts.GetString("OpenRootFolder")
        openPicker.FileTypeFilter.Clear()
        openPicker.FileTypeFilter.Add("*")
        openPicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail
        folder = Await openPicker.PickSingleFolderAsync()
        If folder IsNot Nothing Then
            ' Add picked file to MostRecentlyUsedList.
            mruToken = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList.Add(folder)
            localSettings.Values("RootFolder") = mruToken
        Else
            Return
        End If

        Await LoadAsync()

        If initialized Then
            SearchResultsFolder.Clear()
        End If

    End Function

    Async Function DeleteRecipeAsync(recipe As Recipe) As Task

        Dim failed As Boolean

        Try
            Await recipe.File.DeleteAsync()
        Catch ex As Exception
            failed = True
        End Try

        If failed Then
            Dim dialog = New Windows.UI.Popups.MessageDialog(App.Texts.GetString("UnableToDelete"))
            Await dialog.ShowAsync()
            Return
        End If

        Dim folder = GetFolder(recipe.Categegory)
        folder.DeleteRecipe(recipe)
        If Not FavoriteFolder.ContentLoaded Then
            Await FavoriteFolder.LoadAsync()
        End If
        FavoriteFolder.DeleteRecipe(recipe)
        SearchResultsFolder.DeleteRecipe(recipe)

    End Function

End Class

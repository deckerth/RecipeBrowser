Public Class Favorites
    Inherits RecipeFolder

    Public Shared FolderName As String = App.Texts.GetString("FavoritesFolder")

    Public Sub New()
        Me.Name = FolderName
    End Sub

    Public Async Function AddRecipeAsync(ByVal newRecipe As Recipe) As Task
        If Not _ContentLoaded Then
            Await LoadAsync()
        End If
        If GetRecipe(newRecipe.Categegory, newRecipe.Name) Is Nothing Then
            _RecipeList.Add(newRecipe)
            ApplySortOrder()

            Dim roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings
            Dim recipeList = roamingSettings.CreateContainer("Favorites", Windows.Storage.ApplicationDataCreateDisposition.Always)
            Dim recipeComposite = New Windows.Storage.ApplicationDataCompositeValue()
            recipeComposite("Folder") = newRecipe.Categegory
            recipeComposite("Recipe") = newRecipe.Name
            recipeList.Values(Guid.NewGuid().ToString()) = recipeComposite
        End If
    End Function

    Public Overrides Function DeleteRecipe(ByRef recipeToDelete As Recipe) As Boolean

        If Not MyBase.DeleteRecipe(recipeToDelete) Then
            Return False
        End If

        Dim roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings
        Dim recipeList = roamingSettings.CreateContainer("Favorites", Windows.Storage.ApplicationDataCreateDisposition.Always)
        Dim index As String
        For Each item In recipeList.Values
            If item.Value("Recipe") = recipeToDelete.Name Then
                index = item.Key
                Exit For
            End If
        Next
        If index IsNot Nothing Then
            recipeList.Values.Remove(index)
        End If

        Return True
    End Function

    Public Overrides Async Function LoadAsync() As Task

        Dim roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings
        Dim allFolders = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

        Dim recipeList = roamingSettings.CreateContainer("Favorites", Windows.Storage.ApplicationDataCreateDisposition.Always)

        _RecipeList.Clear()

        For Each item In recipeList.Values
            Dim recipeComposite = item.Value

            If recipeComposite("Folder").Equals(SearchResults.FolderName) Then
                ' Delete this entry
                recipeList.Values.Remove(item.Key)
            Else
                Dim newRecipe As New Recipe

                newRecipe = Await allFolders.GetRecipeAsync(recipeComposite("Folder"), recipeComposite("Recipe"))
                If newRecipe IsNot Nothing Then
                    _RecipeList.Add(newRecipe)
                End If
            End If
        Next

        ApplySortOrder()

        _ContentLoaded = True
    End Function

    Public Sub RenameCategory(ByRef OldName As String, ByRef NewName As String)

        Dim roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings
        Dim storedRecords = roamingSettings.CreateContainer("Favorites", Windows.Storage.ApplicationDataCreateDisposition.Always)
        Dim toRename As New List(Of String)

        For Each item In storedRecords.Values
            If item.Value("Folder") = OldName Then
                toRename.Add(item.Key)
            End If
        Next

        For Each key In toRename
            Dim recipeComposite = New Windows.Storage.ApplicationDataCompositeValue()
            recipeComposite("Folder") = NewName
            recipeComposite("Recipe") = storedRecords.Values(key)("Recipe")
            storedRecords.Values.Remove(key)
            storedRecords.Values(key) = recipeComposite
        Next

        Invalidate()

    End Sub


End Class

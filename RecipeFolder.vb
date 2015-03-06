Imports Windows.Globalization.DateTimeFormatting

Public Class RecipeFolder

    Public Property Name As String
    Public Property Folder As Windows.Storage.StorageFolder
    Public Property Image As BitmapImage

    Public Enum SortOrder
        ByNameAscending
        ByDateDescending
    End Enum

    Protected _Recipes As New ObservableCollection(Of Recipe)()

    Public ReadOnly Property Recipes As ObservableCollection(Of Recipe)
        Get
            Return _Recipes
        End Get
    End Property

    Protected _ContentLoaded As Boolean
    Protected _SortOrder As SortOrder = SortOrder.ByNameAscending
    Protected _RecipeList As New List(Of Recipe)

    Public Sub SetSortOrder(ByVal order As SortOrder)
        If order <> _SortOrder Then
            _SortOrder = order
            ApplySortOrder()
        End If
    End Sub

    Public Function ContentLoaded() As Boolean
        Return _ContentLoaded
    End Function

    Async Function LoadRecipeAsync(file As Windows.Storage.StorageFile) As Task(Of Recipe)

        Dim _recipe = New Recipe

        _recipe.Name = file.Name.Remove(file.Name.Length - 4)  ' delete suffix .pdf

        If Name = SearchResults.FolderName Then
            Dim parent = Await file.GetParentAsync()
            _recipe.Categegory = parent.Name
        Else
            _recipe.Categegory = Name
        End If

        Dim properties = Await file.GetBasicPropertiesAsync()
        _recipe.CreationDate = _recipe.Categegory + ", " + DateTimeFormatter.ShortDate.Format(properties.ItemDate.DateTime)
        _recipe.CreationDateTime = properties.ItemDate.DateTime
        _recipe.File = file

        Return _recipe

    End Function

    Protected Async Function SetUpFolderFromFileListAsync(fileList As IReadOnlyList(Of Windows.Storage.StorageFile)) As Task

        _Recipes.Clear()
        _RecipeList.Clear()

        For Each file In fileList
            Dim _recipe = Await LoadRecipeAsync(file)
            _RecipeList.Add(_recipe)
        Next

        ApplySortOrder()

        _ContentLoaded = True

    End Function


    Public Overridable Async Function LoadAsync() As Task

        _Recipes.Clear()
        _RecipeList.Clear()

        Dim files = Await Folder.GetFilesAsync()

        Await SetUpFolderFromFileListAsync(files)

    End Function

    Public Function GetRecipe(category As String, title As String) As Recipe

        Dim matches = _Recipes.Where(Function(otherRecipe) otherRecipe.Name.Equals(title) And otherRecipe.Categegory.Equals(category))
        If matches.Count() = 1 Then
            Return matches.First()
        End If
        Return Nothing

    End Function

    Public Async Function GetRecipeAsync(category As String, title As String) As Task(Of Recipe)

        If ContentLoaded() Then
            Return GetRecipe(category, title)
        Else
            Dim file As Windows.Storage.StorageFile
            Try
                file = Await Folder.GetFileAsync(title + ".pdf")
            Catch ex As Exception
                Return Nothing
            End Try
            If file IsNot Nothing Then
                Return Await LoadRecipeAsync(file)
            End If
        End If

        Return Nothing

    End Function

    Protected Sub ApplySortOrder()
        Dim _comparer As IComparer(Of Recipe)
        If _SortOrder = SortOrder.ByNameAscending Then
            _comparer = New RecipeComparer_NameAscending
        Else
            _comparer = New RecipeComparer_DateDescending
        End If
        _RecipeList.Sort(_comparer)

        _Recipes.Clear()

        For Each item In _RecipeList
            _Recipes.Add(item)
        Next
    End Sub

    Public Overridable Function DeleteRecipe(ByRef recipeToDelete As Recipe) As Boolean

        If Not ContentLoaded() Then
            Return False
        End If

        If _RecipeList.Contains(recipeToDelete) Then
            _Recipes.Remove(recipeToDelete)
            _RecipeList.Remove(recipeToDelete)
            Return True
        Else
            Dim copy = GetRecipe(recipeToDelete.Categegory, recipeToDelete.Name)
            If copy IsNot Nothing Then
                Return DeleteRecipe(copy)
            End If
        End If
        Return False

    End Function

End Class

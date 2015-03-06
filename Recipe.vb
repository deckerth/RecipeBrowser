﻿Public Class Recipe

    Public Property Categegory As String
    Public Property Name As String
    Public Property CreationDate As String
    Public Property CreationDateTime As DateTime
    Public Property NoOfPages As Integer
    Public Property CurrentPage As Integer
    Public Property RenderedPageNumber As Integer

    Public Property File As Windows.Storage.StorageFile
    Public ReadOnly Property RenderedPage As BitmapImage
        Get
            Return _RenderedPage
        End Get
    End Property

    Private _RenderedPage As BitmapImage
    Private _RenderedPages As New List(Of BitmapImage)
    Private _Document As Windows.Data.Pdf.PdfDocument

    Private _PageRendererRunning As Boolean

    Public Async Function RenderPageAsync() As Task

        If _Document Is Nothing Or RenderedPageNumber = CurrentPage - 1 Or _PageRendererRunning Then
            Return
        End If

        RenderedPageNumber = CurrentPage - 1

        If _RenderedPages.Count >= CurrentPage Then
            _RenderedPage = _RenderedPages.Item(RenderedPageNumber)
            Return
        End If

        _PageRendererRunning = True

        Dim errorOccured As Boolean = False
        Dim permissionDenied As Boolean = False

        Try
            Dim page = _Document.GetPage(RenderedPageNumber)

            Await page.PreparePageAsync()

            Dim filename = Guid.NewGuid().ToString() + ".png"
            Dim tempFolder = Windows.Storage.ApplicationData.Current.LocalFolder
            Dim tempFile As Windows.Storage.StorageFile = Await tempFolder.CreateFileAsync(filename, Windows.Storage.CreationCollisionOption.ReplaceExisting)
            Dim tempStream = Await tempFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite)

            Await page.RenderToStreamAsync(tempStream)
            Await tempStream.FlushAsync()
            tempStream.Dispose()
            page.Dispose()

            Dim renderedPicture = Await Windows.Storage.ApplicationData.Current.LocalFolder.GetFileAsync(filename)

            If renderedPicture IsNot Nothing Then

                ' Open a stream for the selected file.
                Dim fileStream = Await renderedPicture.OpenAsync(Windows.Storage.FileAccessMode.Read)

                ' Set the image source to the selected bitmap.
                _RenderedPage = New Windows.UI.Xaml.Media.Imaging.BitmapImage()

                Await RenderedPage.SetSourceAsync(fileStream)

                _RenderedPages.Add(_RenderedPage)
            End If
        Catch ex1 As System.UnauthorizedAccessException
            permissionDenied = True
            Exit Try
        Catch
            errorOccured = True
            Exit Try
        End Try

        _PageRendererRunning = False

        'If errorOccured Then
        '    Dim popup = New Windows.UI.Popups.MessageDialog("Das Rezept konnte nicht geladen werden.")
        '    Await popup.ShowAsync()
        '    Return Nothing
        'ElseIf permissionDenied Then
        '    Dim popup = New Windows.UI.Popups.MessageDialog("Der Zugriff auf das Rezept wurde verweigert.")
        '    Await popup.ShowAsync()
        '    Return Nothing
        'End If

    End Function

    Public Async Function LoadRecipeAsync() As Task

        If RenderedPage IsNot Nothing Then
            Return
        End If
        Try
            _Document = Await Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(File)
            NoOfPages = _Document.PageCount
            CurrentPage = 1
            RenderedPageNumber = -1 ' Force read
            Await RenderPageAsync()
        Catch ex As Exception
            NoOfPages = 0
            CurrentPage = 0
        End Try

    End Function


    Public Async Function PreviousPage() As Task

        If CurrentPage > 1 Then
            CurrentPage = CurrentPage - 1
            Await RenderPageAsync()
        End If

    End Function

    Public Async Function NextPage() As Task

        If CurrentPage < NoOfPages Then
            CurrentPage = CurrentPage + 1
            Await RenderPageAsync()
        End If

    End Function

End Class

Public Class RecipeComparer_NameAscending
    Implements IComparer(Of Recipe)

    Public Function Compare(ByVal x As Recipe, ByVal y As Recipe) As Integer Implements IComparer(Of Recipe).Compare

        If x Is Nothing Then
            If y Is Nothing Then
                ' If x is Nothing and y is Nothing, they're
                ' equal. 
                Return 0
            Else
                ' If x is Nothing and y is not Nothing, y
                ' is greater. 
                Return -1
            End If
        Else
            ' If x is not Nothing...
            '
            If y Is Nothing Then
                ' ...and y is Nothing, x is greater.
                Return 1
            Else
                ' ...and y is not Nothing, compare the string
                Return x.Name.CompareTo(y.Name)
            End If
        End If
    End Function
End Class

Public Class RecipeComparer_DateDescending
    Implements IComparer(Of Recipe)

    Public Function Compare(ByVal x As Recipe, ByVal y As Recipe) As Integer Implements IComparer(Of Recipe).Compare

        If x Is Nothing Then
            If y Is Nothing Then
                ' If x is Nothing and y is Nothing, they're
                ' equal. 
                Return 0
            Else
                ' If x is Nothing and y is not Nothing, y
                ' is greater. 
                Return -1
            End If
        Else
            ' If x is not Nothing...
            '
            If y Is Nothing Then
                ' ...and y is Nothing, x is greater.
                Return 1
            Else
                ' ...and y is not Nothing, compare the string
                Return -1 * x.CreationDateTime.CompareTo(y.CreationDateTime)
            End If
        End If
    End Function
End Class

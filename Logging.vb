Imports Windows.Foundation.Diagnostics

Public Class Logging
    Implements IDisposable

    Private Session As FileLoggingSession
    Private Channel As LoggingChannel


    Public Function IsEnabled() As Boolean
        Return Session IsNot Nothing
    End Function

    Public Sub Initialize()

        Dim settings = Windows.Storage.ApplicationData.Current.LocalSettings

        Dim active = settings.Values("LoggingActive")

        If active IsNot Nothing AndAlso active.Equals(Boolean.TrueString) Then
            If Not IsEnabled() Then
                Session = New FileLoggingSession("RecipeBrowserLog")
                Channel = New LoggingChannel("Events")
                Session.AddLoggingChannel(Channel)
            End If
        Else
            If Session IsNot Nothing Then
                Session.Dispose()
                Session = Nothing
            End If
            If Channel IsNot Nothing Then
                Channel.Dispose()
                Channel = Nothing
            End If
        End If

    End Sub

    Public Sub SetActive(ByRef isActive As Boolean)

        Dim settings = Windows.Storage.ApplicationData.Current.LocalSettings

        settings.Values("LoggingActive") = isActive.ToString

        Initialize()

    End Sub

    Public Sub Write(ByRef Message As String)

        If Channel IsNot Nothing Then
            Channel.LogMessage(Message)
        End If

    End Sub


    Public Sub New()

        Initialize()

    End Sub

#Region "IDisposable Support"
    Private disposedValue As Boolean ' So ermitteln Sie überflüssige Aufrufe

    ' IDisposable
    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not Me.disposedValue Then
            ' TODO: Verwalteten Zustand löschen (verwaltete Objekte).
            If disposing Then
                If Channel IsNot Nothing Then
                    Channel.Dispose()
                End If
                If Session IsNot Nothing Then
                    Session.Dispose()
                End If
            End If
        End If
        Me.disposedValue = True
    End Sub


    ' Dieser Code wird von Visual Basic hinzugefügt, um das Dispose-Muster richtig zu implementieren.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Ändern Sie diesen Code nicht. Fügen Sie oben in Dispose(disposing As Boolean) Bereinigungscode ein.
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub
#End Region

End Class

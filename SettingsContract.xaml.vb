Imports Windows.UI.Core

' Die Elementvorlage "Einstellungs-Flyout" ist unter http://go.microsoft.com/fwlink/?LinkId=273769 dokumentiert.

Public NotInheritable Class SettingsContract
    Inherits SettingsFlyout

    Public Sub New()
        Me.InitializeComponent()
        LoggingEnabledSwitch.IsOn = App.Logger.IsEnabled()
    End Sub


    Private Sub LoggingEnabledToggled(sender As Object, e As RoutedEventArgs) Handles LoggingEnabledSwitch.Toggled

        App.Logger.SetActive(LoggingEnabledSwitch.IsOn)

    End Sub

End Class

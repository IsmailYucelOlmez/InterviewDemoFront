using System.Windows;
using CommunicationApp.Helpers;
using CommunicationApp.Models;

namespace CommunicationApp.Views;

public partial class SettingsWindow : Window
{
    private AppSettings _settings = null!;

    public SettingsWindow()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        _settings = ConfigHelper.GetSettings();

        ServerIpTextBox.Text = _settings.ServerSettings.ServerIp;
        ServerPortTextBox.Text = _settings.ServerSettings.ServerPort.ToString();

        DbServerTextBox.Text = _settings.DatabaseSettings.Server;
        DbNameTextBox.Text = _settings.DatabaseSettings.Database;
        DbUserIdTextBox.Text = _settings.DatabaseSettings.UserId;
        DbPasswordBox.Password = _settings.DatabaseSettings.Password;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!int.TryParse(ServerPortTextBox.Text, out var port))
            {
                MessageBox.Show("Geçerli bir port numarası girin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _settings.ServerSettings.ServerIp = ServerIpTextBox.Text.Trim();
            _settings.ServerSettings.ServerPort = port;
            // Hub URL'inin sonunda / olmamalı
            var hubUrl = $"http://{_settings.ServerSettings.ServerIp}:{port}/hub";
            if (hubUrl.EndsWith("/"))
            {
                hubUrl = hubUrl.TrimEnd('/');
            }
            _settings.ServerSettings.HubUrl = hubUrl;

            _settings.DatabaseSettings.Server = DbServerTextBox.Text.Trim();
            _settings.DatabaseSettings.Database = DbNameTextBox.Text.Trim();
            _settings.DatabaseSettings.UserId = DbUserIdTextBox.Text.Trim();
            _settings.DatabaseSettings.Password = DbPasswordBox.Password;

            ConfigHelper.SaveSettings(_settings);
            ConfigHelper.ReloadSettings();

            MessageBox.Show("Ayarlar kaydedildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ayarlar kaydedilirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}


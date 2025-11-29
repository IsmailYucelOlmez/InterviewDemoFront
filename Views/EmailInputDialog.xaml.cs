using System.Text.RegularExpressions;
using System.Windows;

namespace CommunicationApp.Views;

public partial class EmailInputDialog : Window
{
    public string Email { get; private set; } = string.Empty;

    public EmailInputDialog()
    {
        InitializeComponent();
        EmailTextBox.Focus();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        var email = EmailTextBox.Text.Trim();
        
        if (string.IsNullOrWhiteSpace(email))
        {
            MessageBox.Show("E-posta adresi boş olamaz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!IsValidEmail(email))
        {
            MessageBox.Show("Geçerli bir e-posta adresi girin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Email = email;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return regex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }
}


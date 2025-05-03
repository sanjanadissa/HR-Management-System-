using Microsoft.Data.SqlClient;
using System.Net;
using System.Net.Mail;
using System.Windows;

namespace WpfApp1
{
    public partial class ForgotPasswordDialog : Window
    {
        public ForgotPasswordDialog()
        {
            InitializeComponent();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Please enter a username.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Get user's email and password from database
                (string email, string password) = GetUserCredentials(username);

                if (email == null || password == null)
                {
                    MessageBox.Show("Username not found in the system.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Send email with password
                SendPasswordEmail(email, username, password);

                DialogBox dialog = new DialogBox("Your password has been sent to the email associated with your account.");
                dialog.Owner = this;
                dialog.ShowDialog();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private (string email, string password) GetUserCredentials(string username)
        {
            string email = null;
            string password = null;

            using (SqlConnection connection = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT email, password FROM employees WHERE username = @username",
                    connection);
                cmd.Parameters.AddWithValue("@username", username);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        email = reader["email"].ToString();
                        password = reader["password"].ToString();
                    }
                }
            }

            return (email, password);
        }

        private void SendPasswordEmail(string recipientEmail, string username, string password)
        {
            try
            {
                SmtpClient smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("sddsdonz@gmail.com", "psof vsxi zazt ukbx"),
                    EnableSsl = true
                };

                MailMessage mailMessage = new MailMessage
                {
                    From = new MailAddress("sddsdonz@gmail.com"),
                    Subject = "HRM System - Password Recovery",
                    Body = $"Dear User,\n\nYou requested your password for the HRM System.\n\nUsername: {username}\nPassword: {password}\n\nFor security reasons, please keep this information confidential.\n\nBest regards,\nHRM Team",
                    IsBodyHtml = false
                };

                mailMessage.To.Add(recipientEmail);
                smtpClient.Send(mailMessage);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to send email: {ex.Message}");
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (UsernameTextBox.Text == "")
            {
                UsernamePlaceholder.Visibility = Visibility.Collapsed;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                UsernamePlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void UsernameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // You can add any additional text change logic here if needed
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Net;
using System.Reflection;
using System.Net.Mail;
using System.Net;


namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for AddByAdmin.xaml
    /// </summary>
    public partial class AddByAdmin : Window
    {
        SqlConnection connection = new SqlConnection(DatabaseHelper.ConnectionString);

        private string firstName;
        private string lastName;
        private string address;
        private string birthday;
        private string gender;
        private string phone;
        public AddByAdmin()
        {
            InitializeComponent();
            LoadDepartments();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow loginWindow = new MainWindow();
            loginWindow.Show();

            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void btnMin_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).Close();
        }

        private void LoadDepartments()
        {
            try
            {
                using var conn = new SqlConnection(DatabaseHelper.ConnectionString);
                using var cmd = new SqlCommand("SELECT DISTINCT name FROM departments", conn);
                conn.Open();
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    var departmentName = rdr["name"].ToString();
                    DepartmentComboBox.Items.Add(new ComboBoxItem { Content = departmentName });
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading departments: " + ex.Message);
            }
        }

        // Submit button click handler: Process the registration.
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            // Get Step 1 controls
            firstName = FirstNameStep1.Text.Trim();
            lastName = LastNameStep1.Text.Trim();
            address = AddressStep1.Text.Trim();    // if you named it
            birthday = BirthdayStep1.Text.Trim();    // if you named it
            gender = (GenderStep1.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
            phone = PhoneStep1.Text.Trim();

            // 1️⃣ All required?
            if (string.IsNullOrEmpty(firstName) ||
                string.IsNullOrEmpty(lastName) ||
                string.IsNullOrEmpty(address) ||
                string.IsNullOrEmpty(birthday) ||
                string.IsNullOrEmpty(gender))

            {
                MessageBox.Show("Please fill in all fields in Step 1.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2️⃣ Birthday parse & age check (16–60)
            DateTime dob;
            var formats = new[] { "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy" };
            if (!DateTime.TryParseExact(birthday, formats, null, System.Globalization.DateTimeStyles.None, out dob))
            {
                MessageBox.Show("Please enter a valid birthday (e.g. 1985-07-24 or 07/24/1985).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int age = (int)((DateTime.Now - dob).TotalDays / 365.25);
            if (age < 16 || age > 60)
            {
                MessageBox.Show("Age must be between 16 and 60 years.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3️⃣ Phone must be exactly 10 digits


            // All good → move to step 2
            RegistrationStepOne.Visibility = Visibility.Collapsed;
            RegistrationStepTwo.Visibility = Visibility.Visible;
        }


        // Back button click handler: Return to Step One.
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            RegistrationStepTwo.Visibility = Visibility.Collapsed;
            RegistrationStepOne.Visibility = Visibility.Visible;
        }

        // Submit button click handler: Process the registration.
        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            // Gather Step 2 values
            string department = DepartmentComboBox.SelectedItem is ComboBoxItem cdi ? cdi.Content.ToString() : "";
            string position = PositionComboBox.SelectedItem is ComboBoxItem cpi ? cpi.Content.ToString() : "";
            string joinDate = JoinedDatePicker.Text.Trim();
            string email = EmailAddressTextBox.Text.Trim();
            string phone = PhoneStep1.Text.Trim(); // re-use the phone from Step 1

            // 1️⃣ All required?
            if (string.IsNullOrEmpty(department) ||
                string.IsNullOrEmpty(position) ||
                string.IsNullOrEmpty(joinDate) ||
                string.IsNullOrEmpty(email) || string.IsNullOrEmpty(phone))
            {
                MessageBox.Show("Please fill in all fields in Step 2.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2️⃣ Join-date parse & at least 16 years after DOB
            if (!DateTime.TryParse(joinDate, out DateTime jd))
            {
                MessageBox.Show("Please enter a valid join date (e.g. 2023-08-15).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DateTime.TryParse(birthday, out DateTime dob);  // we already validated
            if ((jd - dob).TotalDays < 16 * 365.25)
            {
                MessageBox.Show("Join date must be at least 16 years after birthday.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3️⃣ Email format
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show("Please enter a valid email address.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^\d{10}$"))
            {
                MessageBox.Show("Phone number must be exactly 10 digits.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // ✅ Generate a secure random password
            string password = GenerateRandomPassword(12); // 12-character password

            // All validation passed → insert into database
            try
            {
                connection.Open();
                using var cmd = new SqlCommand(
                    "INSERT INTO employees (firstname, lastname, address, birthday, gender, phonenumber, department, position, joindate, email, password) " +
                    "VALUES (@FirstName,@LastName,@Address,@Birthday,@Gender,@Phone,@Department,@Position,@JoinDate,@Email,@Password)",
                    connection);

                cmd.Parameters.AddWithValue("@FirstName", firstName);
                cmd.Parameters.AddWithValue("@LastName", lastName);
                cmd.Parameters.AddWithValue("@Address", address);
                cmd.Parameters.AddWithValue("@Birthday", DateTime.Parse(birthday));
                cmd.Parameters.AddWithValue("@Gender", gender);
                cmd.Parameters.AddWithValue("@Phone", phone);
                cmd.Parameters.AddWithValue("@Department", department);
                cmd.Parameters.AddWithValue("@Position", position);
                cmd.Parameters.AddWithValue("@JoinDate", DateTime.Parse(joinDate));
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Password", password);

                cmd.ExecuteNonQuery();
                MessageBox.Show("Employee added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                SendCredentialsEmail(email, email, password);
                MessageBox.Show("Employee username and password has been sent to their email", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // reset UI
                ClearAllFields();
                RegistrationStepTwo.Visibility = Visibility.Collapsed;
                RegistrationStepOne.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                connection.Close();
            }
        }

        private void SendCredentialsEmail(string recipientEmail, string username, string password)
        {
            try
            {
                var fromAddress = new MailAddress("sddsdonz@gmail.com", "HR Department");
                var toAddress = new MailAddress(recipientEmail);
                const string fromPassword = "psof vsxi zazt ukbx";  // Use app password if Gmail/Outlook
                const string subject = "Your Employee Account Credentials";
                string body = $"Dear {firstName} {lastName},\n\n" +
                              $"Your employee account has been created successfully.\n\n" +
                              $"Username: {username}\n" +
                              $"Password: {password}\n\n" +
                              $"Please change your password after your first login.\n\n" +
                              $"Best regards,\nHR Department";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",       // or smtp.office365.com for Outlook
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword),
                    Timeout = 20000
                };

                using var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body
                };
                smtp.Send(message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Email could not be sent: " + ex.Message, "Email Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        // 🔐 Helper: Generate a strong random password (letters, digits, special chars)
        private static string GenerateRandomPassword(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                                        .Select(s => s[random.Next(s.Length)]).ToArray());
        }



        // Helper method to clear all form fields
        private void ClearAllFields()
        {
            // Clear Step 1 Fields
            FirstNameStep1.Text = "";
            LastNameStep1.Text = "";
            AddressStep1.Text = "";
            BirthdayStep1.Text = "";
            GenderStep1.SelectedIndex = 0;
            PhoneStep1.Text = "";

            // Clear Step 2 Fields
            DepartmentComboBox.SelectedIndex = 0;
            PositionComboBox.SelectedIndex = 0;
            JoinedDatePicker.Text = "";
            EmailAddressTextBox.Text = "";

        }

        // Helper method to find child controls when they're not directly accessible
        private T FindFirstChildByType<T>(DependencyObject parent, int index) where T : DependencyObject
        {
            if (parent == null)
                return null;

            int currentIndex = 0;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is T && currentIndex++ == index)
                    return (T)child;

                T result = FindFirstChildByType<T>(child, index - currentIndex);
                if (result != null)
                    return result;
            }

            return null;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }


    }
}
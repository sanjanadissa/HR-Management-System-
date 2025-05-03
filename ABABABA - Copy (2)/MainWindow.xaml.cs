using Microsoft.Data.SqlClient;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfApp1.EmployeeRole;
using WpfApp1.Hrmanger;

namespace WpfApp1;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {

    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {

    }

    private void TextBox_TextChanged_1(object sender, TextChangedEventArgs e)
    {

    }

    private void Login_Click_1(object sender, RoutedEventArgs e)
    {
        string username = UsernameTextBox.Text;
        string password = PasswordBox.Password;

        SqlConnection connection = new SqlConnection(DatabaseHelper.ConnectionString);

        try
        {
            connection.Open();
            string query = "SELECT * FROM employees WHERE username = @username AND password = @password";
            SqlCommand cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@password", password);

            SqlDataReader reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                string role = reader["position"].ToString().ToLower();

              
                User loggedInUser = new User
                {
                    Id = Convert.ToInt32(reader["id"]), 
                    Firstname = reader["firstname"].ToString(),
                    Lastname = reader["lastname"].ToString(),
                    Address = reader["address"].ToString(),

                    Birthday = reader["birthday"].ToString(),
                    Gender = reader["gender"].ToString(),
                    Phonenumber = reader["phonenumber"].ToString(),
                    Department = reader["department"].ToString(),
                    Email = reader["email"].ToString(),
                    Joindate = reader["joindate"].ToString(),
                    Username = reader["username"].ToString(),
                    Salary = reader["basicsalary"].ToString(),

                    Role = role
                };

                reader.Close();
                connection.Close();

                DialogBox dialog = new DialogBox("Login successful! Welcome");
                dialog.Owner = this;
                dialog.ShowDialog();

                // Open appropriate dashboard
                switch (role.ToLower())
                {
                    case "admin":
                        new HRDashboardWindow(loggedInUser).Show();
                        break;
                    case "manager":
                        new HrDashboard(loggedInUser).Show();
                        break;
                    default:
                        new EmployeeDashboard(loggedInUser).Show();
                        break;
                }

                this.Close();
            }
            else
            {
                reader.Close();
                DialogBox dialog = new DialogBox("Invalid username or password.");
                dialog.Owner = this;
                dialog.ShowDialog();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Database Error: " + ex.Message);
        }
        finally
        {
            connection.Close();
        }
    }


    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
        SignupWindow signUpWindow = new SignupWindow();
        signUpWindow.Show();

        this.Close();
    }

    private void TextBox_TextChanged_2(object sender, TextChangedEventArgs e)
    {

        if (string.IsNullOrEmpty(UsernameTextBox.Text))
        {
            contentPanel.Visibility = Visibility.Visible;
        }
        else
        {
            contentPanel.Visibility = Visibility.Collapsed;
        }
    }
    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        contentPanelpass.Visibility = string.IsNullOrEmpty(PasswordBox.Password) ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void MinimizeWindow(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void CloseWindow(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void ForgotPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        ForgotPasswordDialog dialog = new ForgotPasswordDialog();
        dialog.Owner = this;
        dialog.ShowDialog();
    }
}
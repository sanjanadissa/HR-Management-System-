using Microsoft.Data.SqlClient;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1.Hrmanger
{
    /// <summary>
    /// Interaction logic for HRHome.xaml
    /// </summary>
    public partial class HRHome : UserControl
    {
        private User _user;
        public HRHome(User user)
        {
            InitializeComponent();
            _user = user;
            LoadUserData();
            LoadDashboardStats();
        }
        private void LoadUserData()
        {
            txtName.Text = CapitalizeFirstLetter(_user.Firstname);

            txtUsername.Text = _user.Firstname + " " + _user.Lastname;
            txtId.Text = _user.Id.ToString();
            txtPosition.Text = _user.Role;
            txtDep.Text = _user.Department;
            txtPhone.Text = _user.Phonenumber;
            txtAddr.Text = _user.Address;

            txSalary.Text = _user.Salary;
            txtEmail.Text = _user.Email;
        }
        private string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }

        private void LoadDashboardStats()
        {
            using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();

                // Total Employees
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM employees", conn))
                {
                    int totalEmployees = (int)cmd.ExecuteScalar();
                    txtTotal.Text = totalEmployees.ToString();
                }

                // Leaves for current month
                string leavesTable = $"leaves_{_user.Id}";
                string currentMonth = DateTime.Now.ToString("MMMM"); // e.g., "April"
                int currentYear = DateTime.Now.Year;

                string query = $@"
            SELECT 
                ISNULL(casual_leaves, 0) + 
                ISNULL(sick_leaves, 0) + 
                ISNULL(annual_leaves, 0) + 
                ISNULL(paternity_leaves, 0) + 
                ISNULL(maternity_leaves, 0) + 
                ISNULL(bereavement_leaves, 0) + 
                ISNULL(short_leaves, 0) AS TotalLeaves
            FROM {leavesTable}
            WHERE [year] = @year AND [month] = @month";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@year", currentYear);
                    cmd.Parameters.AddWithValue("@month", currentMonth);

                    object result = cmd.ExecuteScalar();
                    txtLeaves.Text = result != null ? result.ToString() : "0";
                }

                // Calculate % of month passed
                DateTime today = DateTime.Today;
                int totalDays = DateTime.DaysInMonth(today.Year, today.Month);
                double percent = (today.Day / (double)totalDays) * 100;
                txtPayroll.Text = $"{percent:F0}%";
            }
        }

        private void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            AddByAdmin addWindow = new AddByAdmin();
            addWindow.Show();
        }

        private void SearchEmployee_Click(object sender, RoutedEventArgs e)
        {
            var searchEmployeesControl = new SearchEmployeesWindow();

            // Find the MainContentArea in the parent Window
            var parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                var mainContentArea = parentWindow.FindName("MainContentArea") as ContentControl;
                if (mainContentArea != null)
                {
                    // Set the ContentControl's content to the new SearchEmployeesWindow
                    mainContentArea.Content = searchEmployeesControl;
                }
            }
        }

        private void EmployeePerfomance_Click(object sender, RoutedEventArgs e)
        {
            var editEmployeesControl = new AttendenceWindow(_user);

            // Find the MainContentArea in the parent Window
            var parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                var mainContentArea = parentWindow.FindName("MainContentArea") as ContentControl;
                if (mainContentArea != null)
                {
                    // Set the ContentControl's content to the new SearchEmployeesWindow
                    mainContentArea.Content = editEmployeesControl;
                }
            }
        }

        private void ChangeEmployee_Click(object sender, RoutedEventArgs e)
        {
            var editEmployeesControl = new ModifyEmployee();

            // Find the MainContentArea in the parent Window
            var parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                var mainContentArea = parentWindow.FindName("MainContentArea") as ContentControl;
                if (mainContentArea != null)
                {
                    // Set the ContentControl's content to the new SearchEmployeesWindow
                    mainContentArea.Content = editEmployeesControl;
                }
            }
        }
    }
}

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
using System.Windows.Threading;

namespace WpfApp1.EmployeeRole
{
    /// <summary>
    /// Interaction logic for EmployeeHome.xaml
    /// </summary>
    public partial class EmployeeHome : UserControl
    {
        private User _user;
        DispatcherTimer timer = new DispatcherTimer();

        public EmployeeHome(User user)
        {
            InitializeComponent();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
            _user = user;
            LoadUserData();
            LoadDashboardStats();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            txtDate.Text = DateTime.Now.ToString("dd MMMM yyyy"); // e.g., Monday, 01 May 2025
            txtTime.Text = DateTime.Now.ToString("hh:mm:ss tt");
            txtDay.Text = DateTime.Now.ToString("dddd");
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

        private void AddDepartment_Click(object sender, RoutedEventArgs e)
        {
            AddDepartment addWindow = new AddDepartment();
            addWindow.Show();
        }

        private void RemoveDepartment_Click(object sender, RoutedEventArgs e)
        {
            RemoveDepartment addWindow = new RemoveDepartment();
            addWindow.Show();
        }

        private void RemoveEmployee_Click(object sender, RoutedEventArgs e)
        {
            Removeemp addWindow = new Removeemp();
            addWindow.Show();
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

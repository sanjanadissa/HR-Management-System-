using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using WpfApp1.Data;
using WpfApp1.Model;

namespace WpfApp1.Hrmanger
{
    /// <summary>
    /// Interaction logic for HREmployeeManagement.xaml
    /// </summary>
    public partial class HREmployeeManagement : UserControl
    {
        public HREmployeeManagement()
        {
            InitializeComponent();
            LoadMembersData();
        }

        private void LoadMembersData()
        {
            try
            {
                // Fetch employees from the database and map to members
                var members = EmployeeDataAccess.GetEmployeesAsMembers();

                // Convert the list to an ObservableCollection for binding
                var observableMembers = new ObservableCollection<Member>(members);

                // Assign the collection to the DataGrid's ItemsSource
                membersDataGrid.ItemsSource = observableMembers;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading employees: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();

                // Total Employees
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM employees", conn))
                {
                    int totalEmployees = (int)cmd.ExecuteScalar();
                    txtTotal.Text = totalEmployees.ToString() + " Employees";
                }
            }
        }

        private void searchButton_Click(object sender, RoutedEventArgs e)
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

        private void editButton_Click(object sender, RoutedEventArgs e)
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

        private void membersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void addNewByAdminButton_Click(object sender, RoutedEventArgs e)
        {
            AddByAdmin addWindow = new AddByAdmin();
            addWindow.Show();


        }
    }
}

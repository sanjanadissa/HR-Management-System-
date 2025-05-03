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
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for Removeemp.xaml
    /// </summary>
    public partial class Removeemp : Window
    {
        private SqlConnection connection = new SqlConnection(DatabaseHelper.ConnectionString);
        private int currentEmployeeId;
        public Removeemp()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }


        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(EmployeeIdTextBox.Text.Trim(), out int empId))
            {
                MessageBox.Show("Please enter a valid numeric Employee ID.");
                return;
            }

            currentEmployeeId = empId;

            using var conn = new SqlConnection(connection.ConnectionString);
            conn.Open();

            var cmd = new SqlCommand("SELECT * FROM employees WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", empId);

            var reader = cmd.ExecuteReader();

            EmployeeDetailsPanel.Children.Clear();

            if (reader.Read())
            {
                AddDetail("Full Name", reader["firstname"] + " " + reader["lastname"]);
                AddDetail("Birthday", reader["birthday"]?.ToString());
                AddDetail("Address", reader["address"]?.ToString());
                AddDetail("Joined Date", reader["joindate"]?.ToString());
                AddDetail("Basic Salary", reader["basicsalary"]?.ToString());
                AddDetail("Department", reader["department"]?.ToString());
                AddDetail("Email", reader["email"]?.ToString());
                AddDetail("Gender", reader["gender"]?.ToString());
                AddDetail("Department", reader["department"]?.ToString());
                AddDetail("Position", reader["position"]?.ToString());

                RemoveButton.IsEnabled = true;
            }
            else
            {
                MessageBox.Show("No employee found with that ID.", "Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                RemoveButton.IsEnabled = false;
            }

            reader.Close();
        }

        private void AddDetail(string label, string value)
        {
            var stack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
            stack.Children.Add(new TextBlock { Text = label + ": ", FontWeight = FontWeights.Bold, Width = 100 });
            stack.Children.Add(new TextBlock { Text = value });
            EmployeeDetailsPanel.Children.Add(stack);
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show($"Are you sure you want to remove employee ID {currentEmployeeId}?",
                                         "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                using var conn = new SqlConnection(connection.ConnectionString);
                conn.Open();

                using var transaction = conn.BeginTransaction();

                try
                {
                    // 1. Delete from employees
                    var deleteCmd = new SqlCommand("DELETE FROM employees WHERE id = @id", conn, transaction);
                    deleteCmd.Parameters.AddWithValue("@id", currentEmployeeId);
                    deleteCmd.ExecuteNonQuery();

                    // 2. Drop leaves table
                    var dropLeavesCmd = new SqlCommand($"DROP TABLE IF EXISTS leaves_{currentEmployeeId}", conn, transaction);
                    dropLeavesCmd.ExecuteNonQuery();

                    // 3. Drop salary table
                    var dropSalaryCmd = new SqlCommand($"DROP TABLE IF EXISTS salary_{currentEmployeeId}", conn, transaction);
                    dropSalaryCmd.ExecuteNonQuery();

                    transaction.Commit();

                    MessageBox.Show("Employee and associated data removed successfully.");
                    EmployeeDetailsPanel.Children.Clear();
                    EmployeeIdTextBox.Clear();
                    RemoveButton.IsEnabled = false;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show("Error during removal: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

    }
}

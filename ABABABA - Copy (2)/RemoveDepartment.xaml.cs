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
using Microsoft.Data.SqlClient;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for RemoveDepartment.xaml
    /// </summary>
    public partial class RemoveDepartment : Window
    {
        public RemoveDepartment()
        {
            InitializeComponent();
            LoadDepartments();
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

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).Close();
        }

        private void DepartmentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RemoveButton == null)
                return;

            if (DepartmentComboBox.SelectedIndex > 0)
            {
                RemoveButton.IsEnabled = true;
            }
            else
            {
                RemoveButton.IsEnabled = false;
            }
        }


        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (DepartmentComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string departmentName = selectedItem.Content.ToString();

                var result = MessageBox.Show($"Are you sure you want to remove '{departmentName}'?",
                                             "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var conn = new SqlConnection(DatabaseHelper.ConnectionString);
                        conn.Open();
                        using var cmd = new SqlCommand("DELETE FROM departments WHERE name = @name", conn);
                        cmd.Parameters.AddWithValue("@name", departmentName);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Department removed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            DepartmentComboBox.Items.Remove(selectedItem);
                            DepartmentComboBox.SelectedIndex = 0;
                        }
                        else
                        {
                            MessageBox.Show("No department was removed. It may not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error removing department: " + ex.Message);
                    }
                }
            }
        }

    }
}

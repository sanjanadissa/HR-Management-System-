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
using WpfApp1.View.HR_Role.UserControls;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for SearchEmployeesWindow.xaml
    /// </summary>
    public partial class SearchEmployeesWindow : UserControl
    {
        public SearchEmployeesWindow()
        {
            InitializeComponent();
            LoadDepartments();
        }



        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
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


        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            // 1) Build filter dictionary from non-empty inputs
            var filters = new Dictionary<string, object>();
            if (int.TryParse(IdTextBox.Text.Trim(), out var id)) filters["id"] = id;
            if (!string.IsNullOrWhiteSpace(JoinDateTextBox.Text)) filters["joindate"] = DateTime.Parse(JoinDateTextBox.Text.Trim());
            if (!string.IsNullOrWhiteSpace(FirstNameTextBox.Text)) filters["firstname"] = FirstNameTextBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(LastNameTextBox.Text)) filters["lastname"] = LastNameTextBox.Text.Trim();
            if (PositionComboBox.SelectedIndex > 0)
                filters["position"] = ((ComboBoxItem)PositionComboBox.SelectedItem).Content.ToString();
            if (DepartmentComboBox.SelectedIndex > 0)
                filters["department"] = ((ComboBoxItem)DepartmentComboBox.SelectedItem).Content.ToString();
            if (GenderComboBox.SelectedIndex > 0)
                filters["gender"] = ((ComboBoxItem)GenderComboBox.SelectedItem).Content.ToString();
            if (!string.IsNullOrWhiteSpace(EmailTextBox.Text)) filters["email"] = EmailTextBox.Text.Trim();

            // 2) Build SQL
            var sql = "SELECT * FROM employees WHERE 1=1";
            foreach (var key in filters.Keys)
                sql += $" AND {key} = @{key}";

            // 3) Execute
            var results = new List<Dictionary<string, object>>();
            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                foreach (var kv in filters)
                    cmd.Parameters.AddWithValue("@" + kv.Key, kv.Value);

                conn.Open();
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < rdr.FieldCount; i++)
                        row[rdr.GetName(i)] = rdr.GetValue(i);
                    results.Add(row);
                }
            }

            EmployeeDetailsContainer.Items.Clear();

            if (results.Count == 0)
            {
                EmployeeDetailsContainer.Items.Add(new TextBlock { Text = "No records found.", Foreground = Brushes.Red });
                return;
            }

            // Columns to exclude from display
            var exclude = new HashSet<string>(filters.Keys, StringComparer.OrdinalIgnoreCase);

            // 4) Display each matching employee
            foreach (var row in results)
            {
                var panel = new StackPanel { Background = (Brush)new BrushConverter().ConvertFromString("#F2F2F2") };

                var bordered = new Border { Padding = new Thickness(5), Child = panel }; foreach (var kv in row)
                {
                    if (exclude.Contains(kv.Key)) continue;
                    panel.Children.Add(new TextBlock { Text = $"{kv.Key}: {kv.Value}" });
                }
                EmployeeDetailsContainer.Items.Add(bordered);
            }

            // 5) If exactly one, also fetch leaves & salary
            if (results.Count == 1 && results[0].TryGetValue("id", out var objId))
            {
                int empId = Convert.ToInt32(objId);
                var now = DateTime.Now;
                string month = now.ToString("MMMM");
                int year = now.Year;

                // Leaves
                string lt = $"leaves_{empId}";
                try
                {
                    using var conn = new SqlConnection(DatabaseHelper.ConnectionString);
                    using var cmd = new SqlCommand($@"SELECT * FROM {lt} WHERE [year]=@y AND [month]=@m", conn);
                    cmd.Parameters.AddWithValue("@y", year);
                    cmd.Parameters.AddWithValue("@m", month);
                    conn.Open();
                    using var rdr = cmd.ExecuteReader();
                    if (rdr.Read())
                    {
                        var sp = new StackPanel { Margin = new Thickness(5) };
                        sp.Children.Add(new TextBlock { Text = "Remaining Leaves:", FontWeight = FontWeights.Bold });
                        void add(string name, string col) => sp.Children.Add(new TextBlock { Text = $"{name}: {rdr[col]}" });
                        add("Casual", "casual_leaves");
                        add("Sick", "sick_leaves");
                        add("Annual", "annual_leaves");
                        string gender = results[0]["gender"].ToString().ToLower();
                        if (gender == "male") add("Paternity", "paternity_leaves");
                        if (gender == "female") add("Maternity", "maternity_leaves");
                        add("Bereavement", "bereavement_leaves");
                        add("Short", "short_leaves");

                        EmployeeDetailsContainer.Items.Add(sp);
                    }
                }
                catch { /* missing leaves table → skip */ }

                // Salary
                string st = $"salary_{empId}";
                try
                {
                    using var conn2 = new SqlConnection(DatabaseHelper.ConnectionString);
                    using var cmd2 = new SqlCommand($@"SELECT total FROM {st} WHERE [year]=@y AND [month]=@m", conn2);
                    cmd2.Parameters.AddWithValue("@y", year);
                    cmd2.Parameters.AddWithValue("@m", month);
                    conn2.Open();
                    var tot = cmd2.ExecuteScalar();
                    if (tot != null)
                    {
                        EmployeeDetailsContainer.Items.Add(new TextBlock { Text = $"Salary ({month} {year}): {tot:C}", FontWeight = FontWeights.Bold });
                    }
                }
                catch { /* missing salary table → skip */ }
            }
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            // Create new instance of the EmployeeManagement UserControl
            var employeeManagementControl = new EmployeeManagement();

            // Find the MainContentArea in the parent Window
            var parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                var mainContentArea = parentWindow.FindName("MainContentArea") as ContentControl;
                if (mainContentArea != null)
                {
                    // Set the ContentControl's content back to EmployeeManagement
                    mainContentArea.Content = employeeManagementControl;
                }
            }

        }
    }
}

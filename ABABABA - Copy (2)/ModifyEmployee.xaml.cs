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
    /// Interaction logic for ModifyEmployee.xaml
    /// </summary>
    public partial class ModifyEmployee : UserControl
    {
        private Dictionary<string, object> _original = new();
        private List<string> _allDepartments = new List<string>();
        private List<string> _allGenders = new List<string> { "Male", "Female" };


        public ModifyEmployee()
        {
            InitializeComponent();
            LoadDepartments();

            SetInputsEnabled(false);

            // hook up Edit buttons
            FirstNameEditButton.Click += (s, e) => EnableControl(FirstNameTextBox);
            LastNameEditButton.Click += (s, e) => EnableControl(LastNameTextBox);
            AddressEditButton.Click += (s, e) => EnableControl(AddressTextBox);
            PhoneEditButton.Click += (s, e) => EnableControl(PhoneTextBox);
            BirthdayEditButton.Click += (s, e) => EnableControl(BirthdayTextBox);
            SalaryEditButton.Click += (s, e) => EnableControl(BasicSalaryTextBox);
            EmailEditButton.Click += (s, e) => EnableControl(EmailTextBox);
            PasswordEditButton.Click += (s, e) => EnableControl(PasswordTextBox);

            // confirm on combo-change
            DepartmentComboBox.SelectionChanged += Combo_Confirm;
            PositionComboBox.SelectionChanged += Combo_Confirm;
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
                    var name = rdr["name"].ToString();
                    _allDepartments.Add(name);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading departments: " + ex.Message);
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(EmployeeIdTextBox.Text.Trim(), out int id))
            {
                MessageBox.Show("Invalid ID", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using var conn = new SqlConnection(DatabaseHelper.ConnectionString);
            using var cmd = new SqlCommand("SELECT * FROM employees WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            conn.Open();
            using var r = cmd.ExecuteReader();
            if (!r.Read())
            {
                MessageBox.Show($"No employee with ID {id}", "Not found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Unsubscribe to prevent firing confirmation dialog
            DepartmentComboBox.SelectionChanged -= Combo_Confirm;
            PositionComboBox.SelectionChanged -= Combo_Confirm;

            // Load values and set originals
            _original["firstname"] = FirstNameTextBox.Text = r["firstname"].ToString();
            _original["lastname"] = LastNameTextBox.Text = r["lastname"].ToString();
            _original["address"] = AddressTextBox.Text = r["address"].ToString();
            _original["phonenumber"] = PhoneTextBox.Text = r["phonenumber"].ToString();
            _original["birthday"] = BirthdayTextBox.Text = ((DateTime)r["birthday"]).ToString("yyyy-MM-dd");
            _original["basicsalary"] = BasicSalaryTextBox.Text = r["basicsalary"].ToString();
            _original["email"] = EmailTextBox.Text = r["email"].ToString();
            _original["password"] = PasswordTextBox.Text = r["password"].ToString();

            // Department
            var currentDept = r["department"].ToString();
            _original["department"] = DepartmentComboBox.Text = currentDept;

            DepartmentComboBox.Items.Clear();
            DepartmentComboBox.Items.Add(new ComboBoxItem
            {
                Content = currentDept,
                IsEnabled = false,
                IsSelected = true
            });
            foreach (var dept in _allDepartments.Where(d => d != currentDept))
            {
                DepartmentComboBox.Items.Add(new ComboBoxItem { Content = dept });
            }

            // Position
            var currentPos = r["position"].ToString();
            _original["position"] = PositionComboBox.Text = currentPos;

            var allPosItems = PositionComboBox.Items
                .OfType<ComboBoxItem>()
                .Select(i => i.Content.ToString())
                .Where(p => p != "Select Position")
                .ToList();


            PositionComboBox.Items.Clear();
            PositionComboBox.Items.Add(new ComboBoxItem
            {
                Content = currentPos,
                IsEnabled = false,
                IsSelected = true
            });
            foreach (var pos in allPosItems.Where(p => p != currentPos))
            {
                PositionComboBox.Items.Add(new ComboBoxItem { Content = pos });
            }

            r.Close();

            // Re-subscribe after UI is ready
            DepartmentComboBox.SelectionChanged += Combo_Confirm;
            PositionComboBox.SelectionChanged += Combo_Confirm;

            // Enable controls for editing
            SetInputsEnabled(false);
            UpdateButton.IsEnabled = true;
            DepartmentComboBox.IsEnabled = true;
            PositionComboBox.IsEnabled = true;
        }


        private void Combo_Confirm(object sender, SelectionChangedEventArgs e)
        {
            if (!_original.ContainsKey(((ComboBox)sender).Name.ToLower().Replace("combobox", "")))
                return;
            if (MessageBox.Show("Are you sure?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes)
            {
                // revert
                var combo = (ComboBox)sender;
                combo.SelectionChanged -= Combo_Confirm;
                combo.Text = _original[combo.Name.Replace("ComboBox", "").ToLower()].ToString();
                combo.SelectionChanged += Combo_Confirm;
            }
        }

        private void EnableControl(Control c)
        {
            c.IsEnabled = true;
            c.Focus();
        }

        private void SetInputsEnabled(bool on)
        {
            foreach (var c in new Control[]{
                FirstNameTextBox,
                LastNameTextBox,
                AddressTextBox,
                PhoneTextBox,
                BirthdayTextBox,
                BasicSalaryTextBox,
                EmailTextBox,
                PasswordTextBox
            })
            {
                c.IsEnabled = on;
            }
            UpdateButton.IsEnabled = on;
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure?", "Confirm update", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            var updates = new List<string>();
            var cmd = new SqlCommand();
            cmd.Connection = new SqlConnection(DatabaseHelper.ConnectionString);

            void AddIfChanged(string col, string newVal)
            {
                var orig = _original[col]?.ToString() ?? "";
                if (orig != newVal)
                {
                    updates.Add($"{col} = @{col}");
                    cmd.Parameters.AddWithValue("@" + col, newVal);
                }
            }

            AddIfChanged("firstname", FirstNameTextBox.Text.Trim());
            AddIfChanged("lastname", LastNameTextBox.Text.Trim());
            AddIfChanged("address", AddressTextBox.Text.Trim());
            AddIfChanged("phonenumber", PhoneTextBox.Text.Trim());
            AddIfChanged("department", DepartmentComboBox.Text);
            AddIfChanged("position", PositionComboBox.Text);
            AddIfChanged("birthday", BirthdayTextBox.Text.Trim());
            AddIfChanged("basicsalary", BasicSalaryTextBox.Text.Trim());
            AddIfChanged("email", EmailTextBox.Text.Trim());
            AddIfChanged("password", PasswordTextBox.Text.Trim());

            if (updates.Count == 0)
            {
                MessageBox.Show("No changes to update.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            cmd.CommandText = $"UPDATE employees SET {string.Join(", ", updates)} WHERE id=@id";
            cmd.Parameters.AddWithValue("@id", int.Parse(EmployeeIdTextBox.Text.Trim()));

            cmd.Connection.Open();
            cmd.ExecuteNonQuery();
            cmd.Connection.Close();

            MessageBox.Show("Update successful.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            SetInputsEnabled(false);
        }
    }
}

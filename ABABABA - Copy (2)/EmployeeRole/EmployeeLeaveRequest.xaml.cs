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

namespace WpfApp1.EmployeeRole
{
    /// <summary>
    /// Interaction logic for EmployeeLeaveRequest.xaml
    /// </summary>
    public partial class EmployeeLeaveRequest : UserControl
    {
        private SqlConnection connection = new SqlConnection(DatabaseHelper.ConnectionString);
        private User _user;

        private int employeeId; // Replace actual logged-in employee ID
        private string gender;
        private string fullName;
        private Dictionary<string, int> remainingLeaves = new Dictionary<string, int>();
        public EmployeeLeaveRequest(User user)
        {
            InitializeComponent();
            _user = user;
            employeeId = _user.Id;
            LoadEmployeeLeaveInfo();
        }

        //private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    if (e.LeftButton == MouseButtonState.Pressed)
        //        DragMove();
        //}


        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void LeaveTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSubmitButtonState();
        }

        private void LeaveCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSubmitButtonState();
        }

        private void UpdateSubmitButtonState()
        {
            bool isLeaveSelected = LeaveTypeComboBox.SelectedItem != null;
            bool isDateSelected = LeaveCalendar.SelectedDates != null && LeaveCalendar.SelectedDates.Count > 0;

            SubmitButton.IsEnabled = isLeaveSelected && isDateSelected;
        }


        private void LoadEmployeeLeaveInfo()
        {
            using (SqlConnection conn = new SqlConnection(connection.ConnectionString))
            {
                conn.Open();

                SqlCommand cmdEmp = new SqlCommand("SELECT firstname, lastname, gender FROM employees WHERE id = @id", conn);
                cmdEmp.Parameters.AddWithValue("@id", employeeId);
                SqlDataReader readerEmp = cmdEmp.ExecuteReader();
                if (readerEmp.Read())
                {
                    fullName = readerEmp["firstname"] + " " + readerEmp["lastname"];
                    gender = readerEmp["gender"].ToString().ToLower();
                }
                readerEmp.Close();

                int currentYear = DateTime.Now.Year;
                string currentMonth = DateTime.Now.ToString("MMMM");
                string leaveTable = $"leaves_{employeeId}";
                string sql = $"SELECT * FROM {leaveTable} WHERE [year]=@y AND [month]=@m";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@y", currentYear);
                cmd.Parameters.AddWithValue("@m", currentMonth);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    MessageBox.Show("Leave data for this month is not available.", "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 3) assign directly into your TextBoxes:
                txtCasualLeaves.Text = reader["casual_leaves"].ToString();
                txtSickLeaves.Text = reader["sick_leaves"].ToString();
                txtAnnualLeaves.Text = reader["annual_leaves"].ToString();
                txtShortLeaves.Text = reader["short_leaves"].ToString();
                txtBereavementLeaves.Text = reader["bereavement_leaves"].ToString();

                // gender-specific:
                if (gender == "male")
                {
                    txtPaternityLeaves.Text = reader["paternity_leaves"].ToString();
                    PaternityLeaveBorder.Visibility = Visibility.Visible;
                    MaternityLeaveBorder.Visibility = Visibility.Collapsed;
                }
                else
                {
                    txtMaternityLeaves.Text = reader["maternity_leaves"].ToString();
                    PaternityLeaveBorder.Visibility = Visibility.Collapsed;
                    MaternityLeaveBorder.Visibility = Visibility.Visible;
                }

                // populate the ComboBox from these same values if you still need it:
                LeaveTypeComboBox.ItemsSource = new List<string>
        {
            "Casual Leave",
            "Sick Leave",
            "Annual Leave",
            gender == "male" ? "Paternity Leave" : "Maternity Leave",
            "Bereavement Leave",
            "Short Leave"
        };
            }
        }

        private void SubmitLeaveRequest_Click(object sender, RoutedEventArgs e)
        {
            string selectedType = LeaveTypeComboBox.SelectedItem as string;
            var selectedDates = LeaveCalendar.SelectedDates;

            if (selectedType == null || selectedDates.Count == 0)
            {
                MessageBox.Show("Please select a leave type and dates.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (selectedDates.Any(d => d.Date <= DateTime.Today))
            {
                MessageBox.Show("Leave request can only be made for future dates.", "Invalid Dates",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 1) Group dates by year+month
            var byMonth = selectedDates
                .GroupBy(d => new { d.Year, Month = d.ToString("MMMM") })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToList();

            string leaveTable = $"leaves_{employeeId}";

            var overages = new List<string>();

            try
            {
                connection.Open();

                // Ensure the leavereqs table exists
                using (SqlCommand createTableCmd = new SqlCommand(@"
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'leavereqs')
BEGIN
    CREATE TABLE leavereqs (
        id INT IDENTITY(1,1) PRIMARY KEY,
        employee_id INT NOT NULL,
        name NVARCHAR(100),
        leavetype NVARCHAR(50),
        numberofleaves INT,
        datesofleaves NVARCHAR(MAX)
    )
END
", connection))
                {
                    createTableCmd.ExecuteNonQuery();
                }


                foreach (var m in byMonth)
                {
                    // a) Ensure the row exists
                    int matLeaves = gender == "female" ? 15 : 0;
                    int patLeaves = gender == "male" ? 10 : 0;

                    using var cmdEnsure = new SqlCommand($@"
                                IF NOT EXISTS(SELECT 1 FROM {leaveTable} 
                                              WHERE [year]=@y AND [month]=@m)
                                BEGIN
                                    INSERT INTO {leaveTable}([year],[month],
                                        sick_leaves, casual_leaves, short_leaves,
                                        annual_leaves, paternity_leaves, maternity_leaves, bereavement_leaves)
                                    VALUES(@y,@m, 7,2,5,15,@pat,@mat,3)
                                END
                                ", connection);
                    cmdEnsure.Parameters.AddWithValue("@y", m.Year);
                    cmdEnsure.Parameters.AddWithValue("@m", m.Month);
                    cmdEnsure.Parameters.AddWithValue("@pat", patLeaves);
                    cmdEnsure.Parameters.AddWithValue("@mat", matLeaves);
                    cmdEnsure.ExecuteNonQuery();


                    // b) Get remaining leaves for selected type
                    string col = selectedType.ToLower() switch
                    {
                        "sick leave" => "sick_leaves",
                        "casual leave" => "casual_leaves",
                        "short leave" => "short_leaves",
                        "annual leave" => "annual_leaves",
                        "paternity leave" => "paternity_leaves",
                        "maternity leave" => "maternity_leaves",
                        "bereavement leave" => "bereavement_leaves",
                        _ => throw new InvalidOperationException("Unknown leave type")
                    };

                    using var cmdGet = new SqlCommand($@"
SELECT {col}
  FROM {leaveTable}
 WHERE [year]=@y AND [month]=@m
", connection);
                    cmdGet.Parameters.AddWithValue("@y", m.Year);
                    cmdGet.Parameters.AddWithValue("@m", m.Month);
                    int remaining = Convert.ToInt32(cmdGet.ExecuteScalar() ?? 0);

                    if (m.Count > remaining)
                    {
                        overages.Add($"{m.Month} {m.Year}: requested {m.Count}, only {remaining} left");
                    }
                }

                if (overages.Any())
                {
                    var msg =
                        "The following month(s) exceed your balance:\n\n" +
                        string.Join("\n", overages) +
                        "\n\nProceed anyway and let it go negative?";
                    if (MessageBox.Show(msg, "Warning: Exceeding Balance", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                         != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                // 2) Insert leave request
                using var cmdIns = new SqlCommand(@"
INSERT INTO leavereqs(employee_id,name,leavetype,numberofleaves,datesofleaves)
 VALUES(@emp,@name,@type,@count,@dates)
", connection);
                cmdIns.Parameters.AddWithValue("@emp", employeeId);
                cmdIns.Parameters.AddWithValue("@name", fullName);
                cmdIns.Parameters.AddWithValue("@type", selectedType);
                cmdIns.Parameters.AddWithValue("@count", selectedDates.Count);
                cmdIns.Parameters.AddWithValue("@dates",
                    string.Join(", ", selectedDates.Select(d => d.ToString("yyyy-MM-dd"))));
                cmdIns.ExecuteNonQuery();

                MessageBox.Show("Leave request submitted.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadEmployeeLeaveInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                connection.Close();
            }
        }
    }
}

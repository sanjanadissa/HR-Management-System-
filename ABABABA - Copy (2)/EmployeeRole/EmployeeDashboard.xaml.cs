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
using WpfApp1.Hrmanger;

namespace WpfApp1.EmployeeRole
{
    /// <summary>
    /// Interaction logic for EmployeeDashboard.xaml
    /// </summary>
    public partial class EmployeeDashboard : Window
    {
        private User _user;
        private int employeeId;
        public EmployeeDashboard(User user)
        {
            InitializeComponent();
            _user = user;
            employeeId = _user.Id;
            LoadUserData();
            LoadHome();
        }

        private void LoadHome()
        {
            MainContentArea.Content = new EmployeeHome(_user);
        }

        private void LoadAttendanceAndLeave()
        {
            MainContentArea.Content = new EmployeeLeaveRequest(_user);
        }

        private void LoadPayrollManagement()
        {
            MainContentArea.Content = new EmployeePayroll(_user);
        }

        private void LoadPerformanceAndReports()
        {
            MainContentArea.Content = new EmployeeAttendance(_user);
        }

        private void LoadReport()
        {

        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MaximizeOrRestoreWindow(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // Event handlers for menu buttons
        //private void LogOutButton_Click(object sender, RoutedEventArgs e)
        //{
        //    SessionManager.Logout();

        //    var loginWindow = new LoginWindow(App.AuthServiceInstance);

        //    loginWindow.Show();

        //    this.Close();
        //}

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            LoadHome();
        }

        private void AttendanceAndLeaveButton_Click(object sender, RoutedEventArgs e)
        {
            LoadAttendanceAndLeave();
        }

        private void PayrollManagementButton_Click(object sender, RoutedEventArgs e)
        {
            LoadPayrollManagement();
        }

        private void PerformanceAndReportsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadPerformanceAndReports();
        }
        private void reportButton_Click(object sender, RoutedEventArgs e)
        {
            LoadReport();
        }

        private void LoadUserData()
        {
            txtFullname.Text = CapitalizeFirstLetter(_user.Firstname) + " " + CapitalizeFirstLetter(_user.Lastname);
            txtPosition.Text = CapitalizeFirstLetter(_user.Role);
            txtCharacter.Text = GetFirstCharacter(_user.Firstname);

            circleBackground.Fill = GetRandomBrush();
        }
        private string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }

        private string GetFirstCharacter(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "";

            return name.Trim().Substring(0, 1).ToUpper();
        }

        private Brush GetRandomBrush()
        {
            var random = new Random();
            byte r = (byte)random.Next(100, 256);
            byte g = (byte)random.Next(100, 256);
            byte b = (byte)random.Next(100, 256);
            return new SolidColorBrush(Color.FromRgb(r, g, b));
        }

        private void ArriveButton_Click(object sender, RoutedEventArgs e)
        {
            // Perform arrival logic here (e.g., timestamp, DB, etc.)

            ArriveButton.Visibility = Visibility.Collapsed;
            LogoutButton.Visibility = Visibility.Visible;

            var today = DateTime.Today;
            var now = DateTime.Now.TimeOfDay;
            string atbl = $"attendance_{employeeId}";
            string stbl = $"salary_{employeeId}";

            using var conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                // 1) Upsert attendance row for today
                new SqlCommand($@"
IF EXISTS(SELECT 1 FROM {atbl} WHERE date = @date)
    UPDATE {atbl} 
       SET arrival_time = @time
     WHERE date = @date;
ELSE
    INSERT INTO {atbl} (date, arrival_time, exit_time, ot_hours, late)
    VALUES (@date, @time, NULL, 0, 0);
", conn, tx)
                .WithParams(("@date", today), ("@time", now))
                .ExecuteNonQuery();

                // 2) If late (>9AM), flag and update salary.lateness += 75
                if (now > TimeSpan.FromHours(9))
                {
                    new SqlCommand($@"UPDATE {atbl} SET late = 1 WHERE date = @date;", conn, tx)
                        .WithParams(("@date", today))
                        .ExecuteNonQuery();

                    EnsureSalaryRow(conn, tx, stbl, today);

                    new SqlCommand($@"
UPDATE {stbl}
   SET lateness = ISNULL(lateness,0) + 75
 WHERE [year]  = @year 
   AND month   = @month;
", conn, tx)
                    .WithParams(
                       ("@year", today.Year),
                       ("@month", today.ToString("MMMM"))
                    )
                    .ExecuteNonQuery();
                }

                tx.Commit();
                LogoutButton.IsEnabled = true;
                MessageBox.Show("Arrival recorded.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                tx.Rollback();
                MessageBox.Show("Error recording arrival:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Perform logout logic here

            LogoutButton.Visibility = Visibility.Collapsed;
            ArriveButton.Visibility = Visibility.Visible;

            var today = DateTime.Today;
            var now = DateTime.Now.TimeOfDay;
            string atbl = $"attendance_{employeeId}";
            string stbl = $"salary_{employeeId}";

            using var conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                // 1) Upsert exit_time
                new SqlCommand($@"
IF EXISTS(SELECT 1 FROM {atbl} WHERE date = @date)
    UPDATE {atbl} 
       SET exit_time = @time
     WHERE date = @date;
ELSE
    INSERT INTO {atbl} (date, arrival_time, exit_time, ot_hours, late)
    VALUES (@date, NULL, @time, 0, 0);
", conn, tx)
                .WithParams(("@date", today), ("@time", now))
                .ExecuteNonQuery();

                // 2) If overtime (>5PM), compute hours & update both tables
                var threshold = TimeSpan.FromHours(17);
                if (now > threshold)
                {
                    decimal ot = Math.Round((decimal)(now - threshold).TotalHours, 2);


                    new SqlCommand($@"UPDATE {atbl} SET ot_hours = @ot WHERE date = @date;", conn, tx)
                        .WithParams(("@ot", ot), ("@date", today))
                        .ExecuteNonQuery();

                    EnsureSalaryRow(conn, tx, stbl, today);

                    // add OT pay = ot_hours * 75
                    new SqlCommand($@"
UPDATE {stbl}
   SET OT = ISNULL(OT,0) + @pay
 WHERE [year] = @year 
   AND month  = @month;
", conn, tx)
                    .WithParams(
                       ("@pay", ot * 75m),
                       ("@year", today.Year),
                       ("@month", today.ToString("MMMM"))
                    )
                    .ExecuteNonQuery();
                }

                tx.Commit();
                MessageBox.Show("Exit recorded.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                tx.Rollback();
                MessageBox.Show("Error recording exit:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Application.Current.Shutdown();

        }

        private void EnsureSalaryRow(SqlConnection conn, SqlTransaction tx, string salaryTable, DateTime date)
        {
            string month = date.ToString("MMMM");
            int year = date.Year;

            // 1) Grab the “template” data from any existing row
            var cmdFetch = new SqlCommand($@"
SELECT TOP 1 salary_no, epf_no, basic, allowonces, epf 
  FROM {salaryTable}", conn, tx);
            using var rdr = cmdFetch.ExecuteReader();
            if (!rdr.Read())
                throw new InvalidOperationException($"No rows found in {salaryTable} to copy salary metadata.");

            string salaryNo = rdr.GetString(0);
            string epfNo = rdr.GetString(1);
            decimal basic = rdr.GetDecimal(2);
            decimal allow = rdr.GetDecimal(3);
            decimal epf = rdr.GetDecimal(4);
            rdr.Close();

            // 2) Insert if missing
            new SqlCommand($@"
IF NOT EXISTS(
    SELECT 1 FROM {salaryTable} 
     WHERE [year]=@year AND month=@month
)
BEGIN
    INSERT INTO {salaryTable} (
        salary_no, epf_no, year, month, 
        basic, allowonces, OT, earlyout, lateness, timeoff, epf, total
    ) VALUES (
        @sn, @en, @year, @month, 
        @basic, @allow, 0, 0, 0, 0, @epf, 0
    )
END
", conn, tx)
            .WithParams(
                ("@sn", salaryNo),
                ("@en", epfNo),
                ("@year", year),
                ("@month", month),
                ("@basic", basic),
                ("@allow", allow),
                ("@epf", epf)
            )
            .ExecuteNonQuery();
        }
    }
}

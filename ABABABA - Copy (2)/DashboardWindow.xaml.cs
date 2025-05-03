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
    public partial class DashboardWindow : UserControl
    {
        private User _user;
        private int employeeId;
        public DashboardWindow(User user)
        {
            InitializeComponent();
            _user = user;
            employeeId = user.Id;
            ExitButton.IsEnabled = false;
        }

        private void ArrivedButton_Click(object sender, RoutedEventArgs e)
        {
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
                ExitButton.IsEnabled = true;
                MessageBox.Show("Arrival recorded.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                tx.Rollback();
                MessageBox.Show("Error recording arrival:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
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

using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfApp1
{
    public partial class ApproveLeaveRequestsWindow : UserControl
    {
        SqlConnection connection = new SqlConnection(DatabaseHelper.ConnectionString);
        ObservableCollection<LeaveRequest> leaveRequests = new ObservableCollection<LeaveRequest>();

        public ApproveLeaveRequestsWindow()
        {
            InitializeComponent();
            LoadLeaveRequests();
        }





        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void LoadLeaveRequests()
        {
            try
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("SELECT * FROM leavereqs", connection);
                SqlDataReader reader = cmd.ExecuteReader();

                leaveRequests.Clear();
                while (reader.Read())
                {
                    leaveRequests.Add(new LeaveRequest
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        EmployeeId = Convert.ToInt32(reader["employee_id"]),
                        EmployeeName = reader["name"].ToString(),
                        LeaveType = reader["leavetype"].ToString(),
                        RequestedDays = Convert.ToInt32(reader["numberofleaves"]),
                        LeaveDates = reader["datesofleaves"].ToString()
                    });
                }

                reader.Close();
                LeaveRequestsDataGrid.ItemsSource = leaveRequests;
                NoLeaveRequestsText.Visibility = leaveRequests.Count == 0
                            ? Visibility.Visible
                            : Visibility.Collapsed;
            }
            finally
            {
                connection.Close();
            }
        }

        private void ApproveRow_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var request = (LeaveRequest)button.DataContext;

            if (MessageBox.Show("Approve this leave request?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes)
                return;

            ApproveRequest(request);
        }

        private void RejectRow_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var request = (LeaveRequest)button.DataContext;

            if (MessageBox.Show("Are you sure you want to reject this leave request?", "Confirm Rejection", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                != MessageBoxResult.Yes)
                return;

            RejectRequest(request);
        }

        private void ApproveRequest(LeaveRequest request)
        {
            var req = request;
            try
            {
                connection.Open();

                // Map the leave type to its column
                string leaveColumn = req.LeaveType.ToLower() switch
                {
                    "sick leave" => "sick_leaves",
                    "annual leave" => "annual_leaves",
                    "paternity leave" => "paternity_leaves",
                    "maternity leave" => "maternity_leaves",
                    "bereavement leave" => "bereavement_leaves",
                    "short leave" => "short_leaves",
                    "casual leave" => "casual_leaves",
                    _ => throw new InvalidOperationException("Unknown leave type")
                };

                // 1) Parse each individual date
                var dates = req.LeaveDates
                               .Split(',')
                               .Select(s => DateTime.Parse(s.Trim()))
                               .ToList();

                // 2) Group & count by (year, monthName)
                var counts = dates
                    .GroupBy(d => (d.Year, Month: d.ToString("MMMM")))
                    .ToDictionary(g => g.Key, g => g.Count());

                string table = $"leaves_{req.EmployeeId}";

                // 3) Ensure each (year,month) row exists
                foreach (var key in counts.Keys)
                {
                    var ensureCmd = new SqlCommand($@"
                IF NOT EXISTS(
                  SELECT 1 FROM {table}
                   WHERE [year]=@y AND [month]=@m
                )
                  INSERT INTO {table}([year],[month]) VALUES(@y,@m)
            ", connection);
                    ensureCmd.Parameters.AddWithValue("@y", key.Year);
                    ensureCmd.Parameters.AddWithValue("@m", key.Month);
                    ensureCmd.ExecuteNonQuery();
                }

                // 4) For each bucket, subtract exactly that many days
                // 4) Update balances
                if (req.LeaveType.ToLower() is "annual leave" or "paternity leave" or "maternity leave" or "bereavement leave")
                {
                    // For each distinct year, subtract from ALL months in that year
                    foreach (var year in counts.Keys.Select(k => k.Year).Distinct())
                    {
                        foreach (string month in System.Globalization.DateTimeFormatInfo.InvariantInfo.MonthNames.Where(m => !string.IsNullOrEmpty(m)))
                        {
                            var updCmd = new SqlCommand($@"
UPDATE {table}
   SET {leaveColumn} = {leaveColumn} - @days
 WHERE [year]=@y AND [month]=@m
", connection);
                            updCmd.Parameters.AddWithValue("@days", req.RequestedDays);
                            updCmd.Parameters.AddWithValue("@y", year);
                            updCmd.Parameters.AddWithValue("@m", month);
                            updCmd.ExecuteNonQuery();
                        }
                    }
                }
                else
                {
                    // Default behavior: deduct from specific months
                    foreach (var kv in counts)
                    {
                        var (year, month) = kv.Key;
                        int days = kv.Value;

                        var updCmd = new SqlCommand($@"
UPDATE {table}
   SET {leaveColumn} = {leaveColumn} - @days
 WHERE [year]=@y AND [month]=@m
", connection);
                        updCmd.Parameters.AddWithValue("@days", days);
                        updCmd.Parameters.AddWithValue("@y", year);
                        updCmd.Parameters.AddWithValue("@m", month);
                        updCmd.ExecuteNonQuery();
                    }
                }


                // 5) Finally delete the request
                var delCmd = new SqlCommand("DELETE FROM leavereqs WHERE id=@id", connection);
                delCmd.Parameters.AddWithValue("@id", req.Id);
                delCmd.ExecuteNonQuery();

                MessageBox.Show("Leave approved and balance updated.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                leaveRequests.Remove(req);
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


        private void RejectRequest(LeaveRequest request)
        {
            var selectedRequest = request;
            try
            {
                connection.Open();
                SqlCommand deleteCmd = new SqlCommand("DELETE FROM leavereqs WHERE id = @id", connection);
                deleteCmd.Parameters.AddWithValue("@id", selectedRequest.Id);
                deleteCmd.ExecuteNonQuery();

                MessageBox.Show("Leave request rejected.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                leaveRequests.Remove(selectedRequest);
            }
            finally
            {
                connection.Close();
            }
        }

    }

    public class LeaveRequest
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string LeaveType { get; set; }
        public int RequestedDays { get; set; }
        public string LeaveDates { get; set; }
    }
}

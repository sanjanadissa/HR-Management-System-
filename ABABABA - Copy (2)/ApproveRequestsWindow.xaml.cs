using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Net;
using System.Net.Mail;

namespace WpfApp1
{
    public partial class ApproveRequestsWindow : UserControl
    {
        SqlConnection connection = new SqlConnection(DatabaseHelper.ConnectionString);
        ObservableCollection<AppRequest> requests = new ObservableCollection<AppRequest>();

        public ApproveRequestsWindow()
        {
            InitializeComponent();
            LoadRequests();
        }

        private (decimal basic, decimal allowance) GetSalaryComponents(string department, string position)
        {
            string dept = department.ToLower();
            string pos = position.ToLower();

            return (dept, pos) switch
            {
                ("it", "manager") => (60000, 10000),
                ("it", "developer") => (50000, 8000),
                ("hr", "manager") => (50000, 7000),
                ("hr", "executive") => (40000, 6000),
                ("design", "designer") => (45000, 5000),
                _ => (30000, 3000) // default values
            };
        }

        private void LoadRequests()
        {
            try
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("SELECT * FROM appreq", connection);
                SqlDataReader reader = cmd.ExecuteReader();

                requests.Clear();
                while (reader.Read())
                {
                    requests.Add(new AppRequest
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        FirstName = reader["firstname"].ToString(),
                        LastName = reader["lastname"].ToString(),
                        Address = reader["address"].ToString(),
                        Birthday = Convert.ToDateTime(reader["birthday"]),
                        Gender = reader["gender"].ToString(),
                        PhoneNumber = reader["phonenumber"].ToString(),
                        Department = reader["department"].ToString(),
                        Position = reader["position"].ToString(),
                        JoinDate = Convert.ToDateTime(reader["joindate"]),
                        Email = reader["email"].ToString(),
                        Password = reader["password"].ToString()
                    });
                }

                reader.Close();
                RequestsDataGrid.ItemsSource = requests;
                NoRequestsText.Visibility = requests.Count == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading requests: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }

        private void ApproveRow_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var request = (AppRequest)button.DataContext;

            if (MessageBox.Show($"Approve request for {request.FirstName} {request.LastName}?",
                    "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes)
                return;

            ApproveRequest(request);
        }

        private void RejectRow_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var request = (AppRequest)button.DataContext;

            if (MessageBox.Show($"Reject request for {request.FirstName} {request.LastName}?",
                    "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                != MessageBoxResult.Yes)
                return;

            RejectRequest(request);
        }


        private void ApproveRequest(AppRequest request)
        {
            var selectedRequest = request;
            try
            {
                connection.Open();

                var (basicSalary, allowance) = GetSalaryComponents(selectedRequest.Department, selectedRequest.Position);

                SqlCommand insertCmd = new SqlCommand(@"
            INSERT INTO employees (firstname, lastname, address, birthday, gender, phonenumber, department, position, joindate, email, password, basicsalary) 
            VALUES (@fn, @ln, @addr, @bd, @gender, @phone, @dept, @pos, @join, @email, @pwd, @salary);
            SELECT CAST(SCOPE_IDENTITY() AS INT);", connection);

                insertCmd.Parameters.AddWithValue("@fn", selectedRequest.FirstName);
                insertCmd.Parameters.AddWithValue("@ln", selectedRequest.LastName);
                insertCmd.Parameters.AddWithValue("@addr", selectedRequest.Address);
                insertCmd.Parameters.AddWithValue("@bd", selectedRequest.Birthday);
                insertCmd.Parameters.AddWithValue("@gender", selectedRequest.Gender);
                insertCmd.Parameters.AddWithValue("@phone", selectedRequest.PhoneNumber);
                insertCmd.Parameters.AddWithValue("@dept", selectedRequest.Department);
                insertCmd.Parameters.AddWithValue("@pos", selectedRequest.Position);
                insertCmd.Parameters.AddWithValue("@join", selectedRequest.JoinDate);
                insertCmd.Parameters.AddWithValue("@email", selectedRequest.Email);
                insertCmd.Parameters.AddWithValue("@pwd", selectedRequest.Password);
                insertCmd.Parameters.AddWithValue("@salary", basicSalary);

                int newEmployeeId = Convert.ToInt32(insertCmd.ExecuteScalar());
                string currentYear = DateTime.Now.Year.ToString();
                string formattedId = newEmployeeId.ToString("D4");

                string username = $"{selectedRequest.FirstName.ToLower()}{selectedRequest.Department.ToLower()}{formattedId}";

                SqlCommand updateCmd = new SqlCommand("UPDATE employees SET username = @username WHERE id = @id", connection);
                updateCmd.Parameters.AddWithValue("@username", username);
                updateCmd.Parameters.AddWithValue("@id", newEmployeeId);
                updateCmd.ExecuteNonQuery();

                string salaryNo = $"SAL-{formattedId}-{currentYear}";
                string epfNo = $"EPF-{formattedId}-{currentYear}";

                DateTime joinDate = selectedRequest.JoinDate;
                string leaveTableName = $"leaves_{newEmployeeId}";
                string salaryTableName = $"salary_{newEmployeeId}";

                // 1. Create the dynamic leave table
                SqlCommand createLeaveTableCmd = new SqlCommand($@"
            CREATE TABLE {leaveTableName} (
                year INT,
                month VARCHAR(20),
                casual_leaves INT DEFAULT 0,
                sick_leaves INT DEFAULT 0,
                annual_leaves INT DEFAULT 0,
                paternity_leaves INT DEFAULT 0,
                maternity_leaves INT DEFAULT 0,
                bereavement_leaves INT DEFAULT 0,
                short_leaves INT DEFAULT 0,
                PRIMARY KEY (year, month)
            )", connection);
                createLeaveTableCmd.ExecuteNonQuery();

                var now = DateTime.Now;
                var oneYearAgo = now.AddYears(-1);

                DateTime leaveStart = selectedRequest.JoinDate < oneYearAgo
                    ? new DateTime(oneYearAgo.Year, oneYearAgo.Month, 1)
                    : new DateTime(selectedRequest.JoinDate.Year, selectedRequest.JoinDate.Month, 1);
                DateTime leaveEnd = new DateTime(now.Year, now.Month, 1);

                while (leaveStart <= leaveEnd)
                {
                    string month = leaveStart.ToString("MMMM");
                    int year = leaveStart.Year;

                    SqlCommand insertLeaveCmd = new SqlCommand($@"
        INSERT INTO {leaveTableName} (year, month, casual_leaves, sick_leaves, annual_leaves, paternity_leaves, maternity_leaves, bereavement_leaves, short_leaves)
        VALUES (@year, @month, @casual, @sick, @annual, @pat, @mat, @bereavement, @short)", connection);

                    int matLeave = selectedRequest.Gender.ToLower() == "female" ? 5 : 0;
                    int patLeave = selectedRequest.Gender.ToLower() == "male" ? 1 : 0;

                    insertLeaveCmd.Parameters.AddWithValue("@year", year);
                    insertLeaveCmd.Parameters.AddWithValue("@month", month);
                    insertLeaveCmd.Parameters.AddWithValue("@casual", 2);
                    insertLeaveCmd.Parameters.AddWithValue("@sick", 7);
                    insertLeaveCmd.Parameters.AddWithValue("@annual", 15);
                    insertLeaveCmd.Parameters.AddWithValue("@pat", patLeave > 0 ? 10 : 0);
                    insertLeaveCmd.Parameters.AddWithValue("@mat", matLeave > 0 ? 14 : 0);
                    insertLeaveCmd.Parameters.AddWithValue("@bereavement", 3);
                    insertLeaveCmd.Parameters.AddWithValue("@short", 5);

                    insertLeaveCmd.ExecuteNonQuery();

                    leaveStart = leaveStart.AddMonths(1);
                }

                // 3. Create the salary record table for the new employee
                SqlCommand salaryTableCmd = new SqlCommand($@"
                CREATE TABLE {salaryTableName} (
                        salary_no VARCHAR(20),
                        epf_no VARCHAR(50),
                        year INT,
                        month VARCHAR(20),
                        basic DECIMAL(10,2),
                        allowonces DECIMAL(10,2) DEFAULT 0,
                        OT DECIMAL(10,2) DEFAULT 0,
                        earlyout DECIMAL(10,2) DEFAULT 0,
                        lateness DECIMAL(10,2) DEFAULT 0,
                        timeoff DECIMAL(10,2) DEFAULT 0,
                        epf DECIMAL(10,2),
                        total DECIMAL(10,2),
                        PRIMARY KEY (year, month)
                        )", connection);
                salaryTableCmd.ExecuteNonQuery();

                // 4. Create attendance table for the employee
                SqlCommand attendanceTableCmd = new SqlCommand($@"
                        CREATE TABLE attendance_{newEmployeeId} (
                            date DATE PRIMARY KEY,
                            arrival_time TIME NULL,
                            exit_time TIME NULL,
                            ot_hours DECIMAL(10,2) DEFAULT 0,
                            late DECIMAL(10,2) DEFAULT 0
                        )", connection);
                attendanceTableCmd.ExecuteNonQuery();

                // 4. Insert salary records from the start of the year or join date to the current month
                DateTime salaryStart = selectedRequest.JoinDate < oneYearAgo
                    ? new DateTime(oneYearAgo.Year, oneYearAgo.Month, 1)
                    : new DateTime(selectedRequest.JoinDate.Year, selectedRequest.JoinDate.Month, 1);
                DateTime salaryEnd = new DateTime(now.Year, now.Month, 1).AddMonths(-1);  // Up to the current month

                while (salaryStart <= salaryEnd)
                {
                    int year = salaryStart.Year;
                    string monthName = salaryStart.ToString("MMMM");

                    decimal basic = basicSalary;
                    decimal epf = basic * 0.08m;
                    decimal total = basic + allowance - epf;

                    SqlCommand insertSalaryCmd = new SqlCommand($@"
                                INSERT INTO {salaryTableName} 
                                (salary_no, epf_no, year, month, basic, allowonces, OT, earlyout, lateness, timeoff, epf, total)
                                VALUES (@salaryNo, @epfNo, @year, @month, @basic, @allowance, @ot, @early, @late, @timeoff, @epf, @total)", connection);

                    insertSalaryCmd.Parameters.AddWithValue("@salaryNo", salaryNo);
                    insertSalaryCmd.Parameters.AddWithValue("@epfNo", epfNo);
                    insertSalaryCmd.Parameters.AddWithValue("@year", year);
                    insertSalaryCmd.Parameters.AddWithValue("@month", monthName);
                    insertSalaryCmd.Parameters.AddWithValue("@basic", basic);
                    insertSalaryCmd.Parameters.AddWithValue("@allowance", allowance);
                    insertSalaryCmd.Parameters.AddWithValue("@ot", 0);
                    insertSalaryCmd.Parameters.AddWithValue("@early", 0);
                    insertSalaryCmd.Parameters.AddWithValue("@late", 0);
                    insertSalaryCmd.Parameters.AddWithValue("@timeoff", 0);
                    insertSalaryCmd.Parameters.AddWithValue("@epf", epf);
                    insertSalaryCmd.Parameters.AddWithValue("@total", total);

                    insertSalaryCmd.ExecuteNonQuery();

                    salaryStart = salaryStart.AddMonths(1);
                }

                // Delete the request after successful approval
                SqlCommand deleteCmd = new SqlCommand("DELETE FROM appreq WHERE id = @id", connection);
                deleteCmd.Parameters.AddWithValue("@id", selectedRequest.Id);
                deleteCmd.ExecuteNonQuery();

                SendEmail(selectedRequest.Email, username, selectedRequest.Password);
                MessageBox.Show("Request approved and employee added.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                requests.Remove(selectedRequest);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during approval: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }


        private void RejectRequest(AppRequest request)
        {
            var selectedRequest = request;
            try
            {
                connection.Open();

                SqlCommand deleteCmd = new SqlCommand("DELETE FROM appreq WHERE id = @id", connection);
                deleteCmd.Parameters.AddWithValue("@id", selectedRequest.Id);
                deleteCmd.ExecuteNonQuery();

                SendRejectionEmail(selectedRequest.Email, selectedRequest.FirstName);
                MessageBox.Show("Request rejected.", "Rejected", MessageBoxButton.OK, MessageBoxImage.Information);
                requests.Remove(selectedRequest);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during rejection: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }

        private void SendEmail(string recipientEmail, string username, string password)
        {
            try
            {
                SmtpClient smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("sddsdonz@gmail.com", "psof vsxi zazt ukbx"), // paste your real 16-char app password here
                    EnableSsl = true
                };

                MailMessage mailMessage = new MailMessage
                {
                    From = new MailAddress("sddsdonz@gmail.com"),
                    Subject = "Welcome to HRM System",
                    Body = $"Dear User,\n\nYour account has been successfully created.\n\nUsername: {username}\nPassword: {password}\n\nPlease use these credentials to log in to the system.\n\nBest regards,\nHRM Team",
                    IsBodyHtml = false
                };

                mailMessage.To.Add(recipientEmail);
                smtpClient.Send(mailMessage);

                MessageBox.Show("An email with user login credentials has been sent successfully.", "Email Sent", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send email. Error: {ex.Message}", "Email Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SendRejectionEmail(string recipientEmail, string firstName)
        {
            try
            {
                SmtpClient smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("sddsdonz@gmail.com", "psof vsxi zazt ukbx"),
                    EnableSsl = true
                };

                MailMessage mailMessage = new MailMessage
                {
                    From = new MailAddress("sddsdonz@gmail.com"),
                    Subject = "HRM Account Request Rejected",
                    Body = $"Dear {firstName},\n\nWe regret to inform you that your account request has been rejected.\n\nBest regards,\nHRM Team",
                    IsBodyHtml = false
                };

                mailMessage.To.Add(recipientEmail);
                smtpClient.Send(mailMessage);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send rejection email. Error: {ex.Message}", "Email Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class AppRequest
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }
        public DateTime Birthday { get; set; }
        public string Gender { get; set; }
        public string PhoneNumber { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        public DateTime JoinDate { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}

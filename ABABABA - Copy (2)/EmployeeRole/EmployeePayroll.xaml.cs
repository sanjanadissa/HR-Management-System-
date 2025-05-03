using Microsoft.Data.SqlClient;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// Interaction logic for EmployeePayroll.xaml
    /// </summary>
    public partial class EmployeePayroll : UserControl
    {
        SqlConnection connection = new SqlConnection(DatabaseHelper.ConnectionString);
        private User _user;
        public int employeeId;
        public EmployeePayroll(User user)
        {
            InitializeComponent();
            _user = user;
            employeeId = _user.Id;
            this.Loaded += PayslipAdmn_Loaded;
        }

        private void PayslipAdmn_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAvailableMonthsAndYears(); // Now it's safe to access YearComboBox
        }


        private void LoadAvailableMonthsAndYears()
        {
            try
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand($"SELECT DISTINCT year, month FROM salary_{employeeId}", connection);
                SqlDataReader reader = cmd.ExecuteReader();

                List<string> months = new();
                List<string> years = new();

                while (reader.Read())
                {
                    string year = reader["year"].ToString();
                    string month = reader["month"].ToString();

                    if (!years.Contains(year)) years.Add(year);
                    if (!months.Contains(month)) months.Add(month);
                }
                reader.Close();

                // Sort years numerically
                foreach (var year in years.OrderBy(y => int.Parse(y)))
                {
                    YearComboBox.Items.Add(new ComboBoxItem { Content = year });
                }

                // Sort months using standard order (Jan, Feb, ..., Dec)
                string[] monthOrder = new[]
                {
                    "January", "February", "March", "April", "May", "June",
                    "July", "August", "September", "October", "November", "December"
                };

                foreach (var month in monthOrder)
                {
                    if (months.Contains(month))
                    {
                        MonthComboBox.Items.Add(new ComboBoxItem { Content = month });
                    }
                }
            }
            finally
            {
                connection.Close();
            }
        }


        //private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    if (e.LeftButton == MouseButtonState.Pressed)
        //        DragMove();
        //}


        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MonthComboBox.SelectedItem != null && YearComboBox.SelectedItem != null)
            {
                GenerateButton.IsEnabled = true;
                SaveAsPdfButton.IsEnabled = true;
            }
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (MonthComboBox.SelectedItem is ComboBoxItem selectedMonthItem &&
                YearComboBox.SelectedItem is ComboBoxItem selectedYearItem)
            {
                string month = selectedMonthItem.Content.ToString();
                int year = int.Parse(selectedYearItem.Content.ToString());

                GeneratePayslip(month, year);
            }
            else
            {
                MessageBox.Show("Please select both month and year.");
            }
        }
        private UIElement CreateHeader(string month, int year)
        {
            Grid headerGrid = new Grid
            {
                Margin = new Thickness(0, 10, 0, 10)
            };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition());
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition());

            TextBlock companyName = new TextBlock
            {
                Text = "Company Name",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            TextBlock payslipMonth = new TextBlock
            {
                Text = $"{month} {year}",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top
            };

            Grid.SetColumn(companyName, 0);
            Grid.SetColumn(payslipMonth, 1);

            headerGrid.Children.Add(companyName);
            headerGrid.Children.Add(payslipMonth);

            return headerGrid;
        }

        private UIElement CreateEmployeeInfo(string name, string dept, string pos, decimal basicSal, int empId, string salaryNo, string epfNo, int daysInMonth)
        {
            Grid infoGrid = new Grid
            {
                Margin = new Thickness(0, 10, 0, 10)
            };
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition());
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition());

            StackPanel leftPanel = new StackPanel();
            leftPanel.Children.Add(new TextBlock { Text = $"Employee ID: {empId}", FontWeight = FontWeights.Bold });
            leftPanel.Children.Add(new TextBlock { Text = $"Name: {name}" });
            leftPanel.Children.Add(new TextBlock { Text = $"Department: {dept}" });
            leftPanel.Children.Add(new TextBlock { Text = $"Basic Salary: {basicSal:C}" });
            leftPanel.Children.Add(new TextBlock { Text = $"Working Days: {daysInMonth}" });

            StackPanel rightPanel = new StackPanel();
            rightPanel.Children.Add(new TextBlock { Text = $"Position: {pos}", FontWeight = FontWeights.Bold });
            rightPanel.Children.Add(new TextBlock { Text = $"Salary No: {salaryNo}" });
            rightPanel.Children.Add(new TextBlock { Text = $"EPF No: {epfNo}" });

            Grid.SetColumn(leftPanel, 0);
            Grid.SetColumn(rightPanel, 1);

            infoGrid.Children.Add(leftPanel);
            infoGrid.Children.Add(rightPanel);

            return infoGrid;
        }

        private UIElement CreateEarningsDeductions(
    decimal basic, decimal allow, decimal ot,
    decimal epf, decimal earlyOut, decimal timeOff, decimal lateness,
    decimal gross, decimal totalDeductions)
        {
            Grid earningsGrid = new Grid
            {
                Margin = new Thickness(0, 10, 0, 10)
            };
            earningsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            earningsGrid.ColumnDefinitions.Add(new ColumnDefinition());

            // Earnings
            GroupBox earningsBox = new GroupBox { Header = "EARNINGS / INCOME", Margin = new Thickness(5) };
            StackPanel earningsPanel = new StackPanel();
            earningsPanel.Children.Add(new TextBlock { Text = $"Basic Pay: {basic:C}" });
            earningsPanel.Children.Add(new TextBlock { Text = $"Allowances: {allow:C}" });
            earningsPanel.Children.Add(new TextBlock { Text = $"OT: {ot:C}" });
            earningsPanel.Children.Add(new TextBlock { Text = $"Gross Pay: {gross:C}", FontWeight = FontWeights.Bold });
            earningsBox.Content = earningsPanel;

            // Deductions
            GroupBox deductionsBox = new GroupBox { Header = "DEDUCTIONS", Margin = new Thickness(5) };
            StackPanel deductionsPanel = new StackPanel();
            deductionsPanel.Children.Add(new TextBlock { Text = $"Employee EPF: {epf:C}" });
            deductionsPanel.Children.Add(new TextBlock { Text = $"Early Out: {earlyOut:C}" });
            deductionsPanel.Children.Add(new TextBlock { Text = $"Timeoff: {timeOff:C}" });
            deductionsPanel.Children.Add(new TextBlock { Text = $"Lateness: {lateness:C}" });
            deductionsPanel.Children.Add(new TextBlock { Text = $"Total Deduction: {totalDeductions:C}", FontWeight = FontWeights.Bold });
            deductionsBox.Content = deductionsPanel;

            Grid.SetColumn(earningsBox, 0);
            Grid.SetColumn(deductionsBox, 1);

            earningsGrid.Children.Add(earningsBox);
            earningsGrid.Children.Add(deductionsBox);

            return earningsGrid;
        }

        private UIElement CreateNetPay(decimal netPay)
        {
            Border netPayBorder = new Border
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 10, 0, 10),
                Background = Brushes.LightGreen
            };

            TextBlock netPayText = new TextBlock
            {
                Text = $"NET PAY: {netPay:C}",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            netPayBorder.Child = netPayText;
            return netPayBorder;
        }


        private void GeneratePayslip(string month, int year)
        {
            string salaryTable = $"salary_{employeeId}";
            string leavesTable = $"leaves_{employeeId}";

            try
            {
                connection.Open();

                // Step 1: Get Leaves
                SqlCommand leaveCmd = new SqlCommand($"SELECT * FROM {leavesTable} WHERE month = @month AND year = @year", connection);
                leaveCmd.Parameters.AddWithValue("@month", month);
                leaveCmd.Parameters.AddWithValue("@year", year);
                var leaveReader = leaveCmd.ExecuteReader();

                int shortLeave = 0;
                int negativeLeaveTypes = 0;

                if (leaveReader.Read())
                {
                    shortLeave = Convert.ToInt32(leaveReader["short_leaves"]);
                    string[] leaveTypes = { "casual_leaves", "sick_leaves", "annual_leaves", "paternity_leaves", "maternity_leaves", "bereavement_leaves" };

                    foreach (var type in leaveTypes)
                    {
                        int val = Convert.ToInt32(leaveReader[type]);
                        if (val < 0) negativeLeaveTypes++;
                    }
                }
                leaveReader.Close();

                // Step 2: Update salary table if needed
                decimal earlyOut = shortLeave < 0 ? 100 * Math.Abs(shortLeave) : 0;
                decimal timeOff = negativeLeaveTypes > 0 ? 120 * negativeLeaveTypes : 0;

                SqlCommand updateCmd = new SqlCommand($@"
            UPDATE {salaryTable}
            SET earlyout = @earlyout, timeoff = @timeoff
            WHERE month = @month AND year = @year", connection);
                updateCmd.Parameters.AddWithValue("@earlyout", earlyOut);
                updateCmd.Parameters.AddWithValue("@timeoff", timeOff);
                updateCmd.Parameters.AddWithValue("@month", month);
                updateCmd.Parameters.AddWithValue("@year", year);
                updateCmd.ExecuteNonQuery();

                // Step 3: Retrieve updated salary
                SqlCommand salaryCmd = new SqlCommand($@"
            SELECT * FROM {salaryTable} WHERE month = @month AND year = @year", connection);
                salaryCmd.Parameters.AddWithValue("@month", month);
                salaryCmd.Parameters.AddWithValue("@year", year);

                var salaryReader = salaryCmd.ExecuteReader();
                if (salaryReader.Read())
                {
                    decimal basic = (decimal)salaryReader["basic"];
                    decimal allow = (decimal)salaryReader["allowonces"];
                    decimal ot = (decimal)salaryReader["OT"];
                    decimal epf = (decimal)salaryReader["epf"];
                    decimal late = (decimal)salaryReader["lateness"];
                    string salaryNo = salaryReader["salary_no"].ToString();
                    string epfNo = salaryReader["epf_no"].ToString();
                    salaryReader.Close();

                    decimal gross = basic + allow + ot;
                    decimal deductions = epf + earlyOut + timeOff + late;
                    decimal netPay = gross - deductions;

                    SqlCommand updateTotalCmd = new SqlCommand($@"
                            UPDATE {salaryTable}
                            SET total = @total
                            WHERE month = @month AND year = @year", connection);
                    updateTotalCmd.Parameters.AddWithValue("@total", netPay);
                    updateTotalCmd.Parameters.AddWithValue("@month", month);
                    updateTotalCmd.Parameters.AddWithValue("@year", year);
                    updateTotalCmd.ExecuteNonQuery();

                    // Step 4: Get Employee info
                    SqlCommand empCmd = new SqlCommand($"SELECT * FROM employees WHERE id = @id", connection);
                    empCmd.Parameters.AddWithValue("@id", employeeId);
                    var empReader = empCmd.ExecuteReader();

                    if (empReader.Read())
                    {
                        string name = $"{empReader["firstname"]} {empReader["lastname"]}";
                        string dept = empReader["department"].ToString();
                        string pos = empReader["position"].ToString();
                        decimal basicSal = (decimal)empReader["basicsalary"];
                        empReader.Close();

                        DateTime selectedDate = DateTime.ParseExact($"01-{month}-{year}", "dd-MMMM-yyyy", null);
                        int daysInMonth = DateTime.DaysInMonth(selectedDate.Year, selectedDate.Month);

                        // Step 5: Generate UI
                        PayslipText.Visibility = Visibility.Collapsed;
                        PayslipContainer.Children.Clear();

                        PayslipContainer.Children.Add(CreateHeader(month, year));
                        PayslipContainer.Children.Add(CreateEmployeeInfo(name, dept, pos, basicSal, employeeId, salaryNo, epfNo, daysInMonth));
                        PayslipContainer.Children.Add(CreateEarningsDeductions(basic, allow, ot, epf, earlyOut, timeOff, late, gross, deductions));
                        PayslipContainer.Children.Add(CreateNetPay(netPay));
                    }
                }
                else
                {
                    MessageBox.Show($"No salary data found for {month} {year}.", "Data Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    salaryReader.Close();
                    return;
                }
            }
            finally
            {
                connection.Close();
            }
        }

        private void SaveAsPdf()
        {
            RenderTargetBitmap rtb = new RenderTargetBitmap((int)PayslipContainer.ActualWidth, (int)PayslipContainer.ActualHeight, 96d, 96d, PixelFormats.Pbgra32);
            rtb.Render(PayslipContainer);

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using MemoryStream ms = new MemoryStream();
            encoder.Save(ms);
            ms.Position = 0;

            PdfDocument document = new PdfDocument();
            PdfPage page = document.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);
            XImage img = XImage.FromStream(ms);

            page.Width = img.PointWidth;
            page.Height = img.PointHeight;
            gfx.DrawImage(img, 0, 0);

            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "Payslip",
                DefaultExt = ".pdf",
                Filter = "PDF Document (.pdf)|.pdf"
            };

            if (dialog.ShowDialog() == true)
            {
                document.Save(dialog.FileName);
                MessageBox.Show("Payslip saved as PDF!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }


        private void SaveAsPdfButton_Click(object sender, RoutedEventArgs e)
        {
            SaveAsPdf();
        }
    }
}

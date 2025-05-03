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
    /// Interaction logic for EmployeeAttendance.xaml
    /// </summary>
    public partial class EmployeeAttendance : UserControl
    {
        private readonly SqlConnection connection = new SqlConnection(DatabaseHelper.ConnectionString);
        private User _user;

        public class AttendanceRecord
        {
            public DateTime Date { get; set; }
            public string DayOfWeek => Date.ToString("ddd");
            public TimeSpan? ArrivalTime { get; set; }
            public TimeSpan? ExitTime { get; set; }
            public decimal OTHours { get; set; }
            public bool IsLate { get; set; }
            public string Status { get; set; }
            public Brush StatusColor { get; set; }
            public string WorkHours => CalculateWorkHours();

            private string CalculateWorkHours()
            {
                if (!ArrivalTime.HasValue || !ExitTime.HasValue)
                    return "N/A";

                TimeSpan workTime = ExitTime.Value - ArrivalTime.Value;
                return workTime.TotalHours > 0 ? $"{workTime.Hours:D2}:{workTime.Minutes:D2}" : "N/A";
            }
        }
        public EmployeeAttendance(User user)
        {
            InitializeComponent();
            _user = user;
            EmployeeNameTextBlock.Text = "Your Performance";
            //txtSearch.Text = $"{_user.Firstname} {_user.Lastname}'s Performance";
            ClearPerformanceData();
            LoadAvailableMonthsAndYears();
        }

        private User GetEmployeeById(int employeeId)
        {
            try
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT id, firstname, lastname FROM employees WHERE id = @id", connection);
                cmd.Parameters.AddWithValue("@id", employeeId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new User
                        {
                            Id = reader.GetInt32(0),
                            Firstname = reader.GetString(1),
                            Lastname = reader.GetString(2)
                        };
                    }
                }
            }
            finally
            {
                connection.Close();
            }
            return null;
        }

        private void ClearPerformanceData()
        {
            DaysPresentTextBlock.Text = "0";
            DaysAbsentTextBlock.Text = "0";
            WorkingDaysTextBlock.Text = "0";
            AttendancePercentageTextBlock.Text = "0%";
            OnTimeDaysTextBlock.Text = "0";
            LateDaysTextBlock.Text = "0";
            AvgArrivalTextBlock.Text = "00:00";
            PunctualityPercentageTextBlock.Text = "0%";
            TotalHoursTextBlock.Text = "0";
            OvertimeHoursTextBlock.Text = "0";
            AvgExitTextBlock.Text = "00:00";
            OTDaysTextBlock.Text = "0";

            CalendarDaysGrid.Children.Clear();
            CalendarDaysGrid.RowDefinitions.Clear();
            CalendarDaysGrid.ColumnDefinitions.Clear();

            AttendanceListView.ItemsSource = null;

            ScoreTextBlock.Text = "0";
            AttendanceScoreTextBlock.Text = "0/30";
            PunctualityScoreTextBlock.Text = "0/40";
            ProductivityScoreTextBlock.Text = "0/30";
            RatingTextBlock.Text = "Rating: N/A";
            PerformanceCommentsTextBlock.Text = "No performance data available.";
        }

        private void LoadAvailableMonthsAndYears()
        {
            try
            {
                connection.Open();

                SqlCommand checkTableCmd = new SqlCommand(
                    $"SELECT CASE WHEN OBJECT_ID('attendance_{_user.Id}', 'U') IS NOT NULL THEN 1 ELSE 0 END",
                    connection);
                bool tableExists = (int)checkTableCmd.ExecuteScalar() == 1;

                if (!tableExists)
                {
                    YearComboBox.ItemsSource = null;
                    MonthComboBox.ItemsSource = null;
                    return;
                }

                SqlCommand cmd = new SqlCommand($@"
                    SELECT DISTINCT 
                        YEAR([date]) AS YearValue, 
                        MONTH([date]) AS MonthValue 
                    FROM attendance_{_user.Id}", connection);

                SqlDataReader reader = cmd.ExecuteReader();

                HashSet<int> yearSet = new();
                HashSet<int> monthSet = new();

                while (reader.Read())
                {
                    yearSet.Add(reader.GetInt32(0));
                    monthSet.Add(reader.GetInt32(1));
                }
                reader.Close();

                YearComboBox.ItemsSource = yearSet.OrderByDescending(y => y).Select(y => y.ToString()).ToList();

                var monthNames = System.Globalization.DateTimeFormatInfo.InvariantInfo.MonthNames;
                MonthComboBox.ItemsSource = monthSet
                    .OrderBy(m => m)
                    .Select(m => monthNames[m - 1])
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading available months: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                connection.Close();
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MonthComboBox.SelectedItem != null && YearComboBox.SelectedItem != null)
            {
                GenerateButton.IsEnabled = true;
            }
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (MonthComboBox.SelectedItem == null || YearComboBox.SelectedItem == null)
                return;

            string monthName = MonthComboBox.SelectedItem.ToString();
            int monthNumber = DateTime.ParseExact(monthName, "MMMM", null).Month;
            int year = int.Parse(YearComboBox.SelectedItem.ToString());

            GenerateAttendanceReport(year, monthNumber);
        }

        private void GenerateAttendanceReport(int year, int month)
        {
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var firstDayOfMonth = new DateTime(year, month, 1);
            var records = new List<AttendanceRecord>();

            try
            {
                connection.Open();
                string query = $@"
                    SELECT [date], [arrival_time], [exit_time], [ot_hours], [late]
                    FROM attendance_{_user.Id}
                    WHERE YEAR([date]) = @year AND MONTH([date]) = @month";

                using SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@year", year);
                cmd.Parameters.AddWithValue("@month", month);

                using SqlDataReader reader = cmd.ExecuteReader();

                var recordsByDate = new Dictionary<DateTime, AttendanceRecord>();

                while (reader.Read())
                {
                    DateTime date = reader.GetDateTime(0);
                    TimeSpan? arrival = reader.IsDBNull(1) ? null : (TimeSpan?)reader.GetTimeSpan(1);
                    TimeSpan? exit = reader.IsDBNull(2) ? null : (TimeSpan?)reader.GetTimeSpan(2);
                    decimal ot = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3);
                    decimal late = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4);

                    var record = new AttendanceRecord
                    {
                        Date = date,
                        ArrivalTime = arrival,
                        ExitTime = exit,
                        OTHours = ot,
                        IsLate = late > 0
                    };

                    if (!arrival.HasValue && !exit.HasValue)
                    {
                        record.Status = "Absent";
                        record.StatusColor = Brushes.Red;
                    }
                    else if (record.IsLate)
                    {
                        record.Status = "Late";
                        record.StatusColor = Brushes.Orange;
                    }
                    else
                    {
                        record.Status = "Present";
                        record.StatusColor = Brushes.Green;
                    }

                    recordsByDate[date] = record;
                    records.Add(record);
                }

                for (int day = 1; day <= daysInMonth; day++)
                {
                    var date = new DateTime(year, month, day);
                    if (!recordsByDate.ContainsKey(date))
                    {
                        var isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
                        var record = new AttendanceRecord
                        {
                            Date = date,
                            Status = isWeekend ? "Weekend" : "Absent",
                            StatusColor = isWeekend ? Brushes.Gray : Brushes.Red
                        };
                        records.Add(record);
                    }
                }

                records = records.OrderBy(r => r.Date).ToList();

                var workingDays = records.Count(r => r.Status != "Weekend");
                var daysPresent = records.Count(r => r.Status == "Present" || r.Status == "Late");
                var daysAbsent = records.Count(r => r.Status == "Absent");
                var lateDays = records.Count(r => r.Status == "Late");
                var onTimeDays = records.Count(r => r.Status == "Present");
                var otDays = records.Count(r => r.OTHours > 0);

                var attendancePercentage = workingDays > 0
                    ? Math.Round((double)daysPresent / workingDays * 100, 1)
                    : 0;

                var punctualityPercentage = daysPresent > 0
                    ? Math.Round((double)onTimeDays / daysPresent * 100, 1)
                    : 0;

                TimeSpan totalArrivalTime = TimeSpan.Zero;
                TimeSpan totalExitTime = TimeSpan.Zero;
                int arrivalCount = 0;
                int exitCount = 0;
                decimal totalOtHours = 0;
                double totalWorkHours = 0;

                foreach (var record in records)
                {
                    if (record.ArrivalTime.HasValue)
                    {
                        totalArrivalTime += record.ArrivalTime.Value;
                        arrivalCount++;
                    }

                    if (record.ExitTime.HasValue)
                    {
                        totalExitTime += record.ExitTime.Value;
                        exitCount++;
                    }

                    if (record.ArrivalTime.HasValue && record.ExitTime.HasValue)
                    {
                        var workTime = record.ExitTime.Value - record.ArrivalTime.Value;
                        if (workTime.TotalHours > 0)
                        {
                            totalWorkHours += workTime.TotalHours;
                        }
                    }

                    totalOtHours += record.OTHours;
                }

                var avgArrival = arrivalCount > 0
                    ? new TimeSpan(totalArrivalTime.Ticks / arrivalCount)
                    : TimeSpan.Zero;

                var avgExit = exitCount > 0
                    ? new TimeSpan(totalExitTime.Ticks / exitCount)
                    : TimeSpan.Zero;

                DaysPresentTextBlock.Text = daysPresent.ToString();
                DaysAbsentTextBlock.Text = daysAbsent.ToString();
                WorkingDaysTextBlock.Text = workingDays.ToString();
                AttendancePercentageTextBlock.Text = $"{attendancePercentage}%";

                OnTimeDaysTextBlock.Text = onTimeDays.ToString();
                LateDaysTextBlock.Text = lateDays.ToString();
                AvgArrivalTextBlock.Text = avgArrival.ToString(@"hh\:mm");
                PunctualityPercentageTextBlock.Text = $"{punctualityPercentage}%";

                TotalHoursTextBlock.Text = Math.Round(totalWorkHours, 1).ToString();
                OvertimeHoursTextBlock.Text = totalOtHours.ToString();
                AvgExitTextBlock.Text = avgExit.ToString(@"hh\:mm");
                OTDaysTextBlock.Text = otDays.ToString();

                GenerateCalendarGrid(firstDayOfMonth, daysInMonth, recordsByDate);
                AttendanceListView.ItemsSource = records;

                CalculateAndDisplayPerformance(
                    attendancePercentage,
                    punctualityPercentage,
                    totalWorkHours,
                    totalOtHours,
                    workingDays);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                connection.Close();
            }
        }

        private void GenerateCalendarGrid(DateTime firstDayOfMonth, int daysInMonth, Dictionary<DateTime, AttendanceRecord> recordsByDate)
        {
            CalendarDaysGrid.Children.Clear();
            CalendarDaysGrid.RowDefinitions.Clear();
            CalendarDaysGrid.ColumnDefinitions.Clear();

            int firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
            int totalDays = firstDayOfWeek + daysInMonth;
            int rows = (int)Math.Ceiling(totalDays / 7.0);

            for (int i = 0; i < rows; i++)
            {
                CalendarDaysGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });
            }

            for (int i = 0; i < 7; i++)
            {
                CalendarDaysGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            int currentDay = 1;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    if (row == 0 && col < firstDayOfWeek)
                    {
                        continue;
                    }

                    if (currentDay > daysInMonth)
                    {
                        break;
                    }

                    var date = new DateTime(firstDayOfMonth.Year, firstDayOfMonth.Month, currentDay);
                    var border = new Border
                    {
                        Margin = new Thickness(2),
                        CornerRadius = new CornerRadius(3),
                        BorderThickness = new Thickness(1),
                        BorderBrush = new SolidColorBrush(Colors.LightGray)
                    };

                    if (recordsByDate.TryGetValue(date, out var record))
                    {
                        switch (record.Status)
                        {
                            case "Present":
                                border.Background = new SolidColorBrush(Color.FromRgb(232, 255, 232));
                                break;
                            case "Late":
                                border.Background = new SolidColorBrush(Color.FromRgb(255, 240, 220));
                                break;
                            case "Absent":
                                border.Background = new SolidColorBrush(Color.FromRgb(255, 232, 232));
                                break;
                            default:
                                border.Background = new SolidColorBrush(Colors.WhiteSmoke);
                                break;
                        }
                    }
                    else
                    {
                        if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                        {
                            border.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                        }
                        else
                        {
                            border.Background = new SolidColorBrush(Color.FromRgb(255, 232, 232));
                        }
                    }

                    var grid = new Grid();
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    var dayText = new TextBlock
                    {
                        Text = currentDay.ToString(),
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 2, 0, 0)
                    };
                    Grid.SetRow(dayText, 0);
                    grid.Children.Add(dayText);

                    if (recordsByDate.TryGetValue(date, out var statusRecord))
                    {
                        var statusText = new TextBlock
                        {
                            Text = statusRecord.Status,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            FontSize = 10,
                            Foreground = statusRecord.StatusColor,
                            Margin = new Thickness(0, 0, 0, 2)
                        };
                        Grid.SetRow(statusText, 1);
                        grid.Children.Add(statusText);

                        if (statusRecord.ArrivalTime.HasValue)
                        {
                            var timeText = new TextBlock
                            {
                                Text = statusRecord.ArrivalTime.Value.ToString(@"hh\:mm"),
                                HorizontalAlignment = HorizontalAlignment.Center,
                                FontSize = 10,
                                Foreground = Brushes.DarkGray
                            };
                            Grid.SetRow(timeText, 1);
                            grid.Children.Add(timeText);
                        }
                    }
                    else if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                    {
                        var statusText = new TextBlock
                        {
                            Text = "Weekend",
                            HorizontalAlignment = HorizontalAlignment.Center,
                            FontSize = 10,
                            Foreground = Brushes.Gray,
                            Margin = new Thickness(0, 0, 0, 2)
                        };
                        Grid.SetRow(statusText, 1);
                        grid.Children.Add(statusText);
                    }
                    else
                    {
                        var statusText = new TextBlock
                        {
                            Text = "Absent",
                            HorizontalAlignment = HorizontalAlignment.Center,
                            FontSize = 10,
                            Foreground = Brushes.Red,
                            Margin = new Thickness(0, 0, 0, 2)
                        };
                        Grid.SetRow(statusText, 1);
                        grid.Children.Add(statusText);
                    }

                    border.Child = grid;
                    Grid.SetRow(border, row);
                    Grid.SetColumn(border, col);
                    CalendarDaysGrid.Children.Add(border);

                    currentDay++;
                }
            }
        }

        private void CalculateAndDisplayPerformance(
            double attendancePercentage,
            double punctualityPercentage,
            double totalWorkHours,
            decimal totalOtHours,
            int workingDays)
        {
            double attendanceScore = CalculateAttendanceScore(attendancePercentage);
            double punctualityScore = CalculatePunctualityScore(punctualityPercentage);
            double productivityScore = CalculateProductivityScore(totalWorkHours, totalOtHours, workingDays);

            double totalScore = attendanceScore + punctualityScore + productivityScore;
            int roundedScore = (int)Math.Round(totalScore);

            ScoreTextBlock.Text = roundedScore.ToString();
            AttendanceScoreTextBlock.Text = $"{attendanceScore:F1}/30";
            PunctualityScoreTextBlock.Text = $"{punctualityScore:F1}/40";
            ProductivityScoreTextBlock.Text = $"{productivityScore:F1}/30";

            string rating;
            Brush ratingColor;

            if (roundedScore >= 90)
            {
                rating = "Excellent";
                ratingColor = Brushes.Green;
                ScoreTextBlock.Foreground = Brushes.Green;
            }
            else if (roundedScore >= 80)
            {
                rating = "Very Good";
                ratingColor = Brushes.DarkGreen;
                ScoreTextBlock.Foreground = Brushes.DarkGreen;
            }
            else if (roundedScore >= 70)
            {
                rating = "Good";
                ratingColor = Brushes.DarkCyan;
                ScoreTextBlock.Foreground = Brushes.DarkCyan;
            }
            else if (roundedScore >= 60)
            {
                rating = "Satisfactory";
                ratingColor = Brushes.Orange;
                ScoreTextBlock.Foreground = Brushes.Orange;
            }
            else
            {
                rating = "Needs Improvement";
                ratingColor = Brushes.Red;
                ScoreTextBlock.Foreground = Brushes.Red;
            }

            RatingTextBlock.Text = $"Rating: {rating}";
            RatingTextBlock.Foreground = ratingColor;

            GeneratePerformanceComments(
                attendancePercentage,
                punctualityPercentage,
                totalWorkHours / workingDays,
                totalOtHours,
                rating);
        }

        private double CalculateAttendanceScore(double attendancePercentage)
        {
            return Math.Min(30, (attendancePercentage / 100) * 30);
        }

        private double CalculatePunctualityScore(double punctualityPercentage)
        {
            if (punctualityPercentage >= 95) return 40;
            if (punctualityPercentage >= 90) return 35;
            if (punctualityPercentage >= 85) return 30;
            if (punctualityPercentage >= 80) return 25;
            if (punctualityPercentage >= 70) return 20;
            if (punctualityPercentage >= 60) return 15;
            if (punctualityPercentage >= 50) return 10;
            return Math.Max(0, (punctualityPercentage / 100) * 10);
        }

        private double CalculateProductivityScore(double totalWorkHours, decimal totalOtHours, int workingDays)
        {
            if (workingDays == 0) return 0;

            double avgWorkHours = totalWorkHours / workingDays;
            double otContribution = Math.Min(10, (double)totalOtHours * 0.5);

            double workHoursScore;
            if (avgWorkHours >= 8.5) workHoursScore = 20;
            else if (avgWorkHours >= 8) workHoursScore = 18;
            else if (avgWorkHours >= 7.5) workHoursScore = 15;
            else if (avgWorkHours >= 7) workHoursScore = 12;
            else if (avgWorkHours >= 6) workHoursScore = 8;
            else workHoursScore = avgWorkHours;

            return Math.Min(30, workHoursScore + otContribution);
        }

        private void GeneratePerformanceComments(
            double attendancePercentage,
            double punctualityPercentage,
            double avgWorkHours,
            decimal totalOtHours,
            string rating)
        {
            var comments = new List<string>();

            if (attendancePercentage >= 95)
                comments.Add("Excellent attendance record demonstrates strong commitment to work responsibilities.");
            else if (attendancePercentage >= 85)
                comments.Add("Good attendance record with occasional absences.");
            else if (attendancePercentage >= 75)
                comments.Add("Satisfactory attendance, though improvement would be beneficial.");
            else
                comments.Add("Attendance requires improvement. Regular presence is essential for team productivity.");

            if (punctualityPercentage >= 95)
                comments.Add("Consistently punctual, showing excellent time management and reliability.");
            else if (punctualityPercentage >= 85)
                comments.Add("Generally punctual with occasional late arrivals.");
            else if (punctualityPercentage >= 75)
                comments.Add("Punctuality is satisfactory but has room for improvement.");
            else
                comments.Add("Frequent late arrivals are affecting performance. Please improve punctuality.");

            if (avgWorkHours >= 8.5)
                comments.Add("Demonstrates excellent productivity with consistent full workdays.");
            else if (avgWorkHours >= 8)
                comments.Add("Good productivity levels with standard working hours.");
            else if (avgWorkHours >= 7)
                comments.Add("Working hours are slightly below expectations. Consider optimizing daily productivity.");
            else
                comments.Add("Working hours are significantly below standard expectations. Focus on maintaining full productive days.");

            if (totalOtHours > 20)
                comments.Add("Shows exceptional dedication through significant overtime contribution.");
            else if (totalOtHours > 10)
                comments.Add("Good commitment shown through overtime work when required.");
            else if (totalOtHours > 0)
                comments.Add("Occasional overtime work demonstrates willingness to support when needed.");

            switch (rating)
            {
                case "Excellent":
                    comments.Add("Overall excellent performance that exceeds expectations and sets a high standard.");
                    break;
                case "Very Good":
                    comments.Add("Very good overall performance that consistently meets and often exceeds expectations.");
                    break;
                case "Good":
                    comments.Add("Good overall performance that generally meets expectations with some areas for improvement.");
                    break;
                case "Satisfactory":
                    comments.Add("Satisfactory performance that meets basic expectations but has significant room for growth.");
                    break;
                default:
                    comments.Add("Performance needs focused improvement in multiple areas to meet organizational standards.");
                    break;
            }

            PerformanceCommentsTextBlock.Text = string.Join("\n\n", comments);
        }
    }
}

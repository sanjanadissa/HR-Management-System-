using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using WpfApp1.Model;

namespace WpfApp1.ViewModel
{
    public static class EmployeeMapper
    {
        private static readonly Random _random = new Random();

        public static List<Member> MapEmployeesToMembers(List<Employee> employees)
        {
            return employees.Select(employee => new Member
            {
                Character = employee.FirstName.Length > 0 ? employee.FirstName[0].ToString() : "?", // Default to "?" if FirstName is empty
                BgColor = GetRandomBrush(),
                Name = $"{employee.FirstName} {employee.LastName}",
                Department = employee.Department,
                Role = employee.Role,
                Gender = employee.Gender,
                Salary = employee.Salary,
                Email = employee.Email,
                PhoneNumber = employee.PhoneNumber,
                Address = employee.Address,
                UserID = employee.UserId,
                JoinDate = DateOnly.FromDateTime(employee.JoiningDate)

            }).ToList();
        }

        private static Brush GetRandomBrush()
        {
            // Generate a random color
            var randomColor = Color.FromRgb((byte)_random.Next(256), (byte)_random.Next(256), (byte)_random.Next(256));
            return new SolidColorBrush(randomColor);
        }
    }
}

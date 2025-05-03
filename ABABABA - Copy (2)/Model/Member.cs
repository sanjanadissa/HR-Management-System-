using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WpfApp1.Model
{
    public class Member
    {
        public string Character { get; set; } // First character of the first name
        public Brush BgColor { get; set; }   // Random background color
        public string Name { get; set; }     // FirstName + LastName
        public string Department { get; set; }
        public string Position { get; set; }
        public string Role { get; set; }
        public string Gender { get; set; }
        public decimal Salary { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public decimal UserID { get; set; }
        public DateOnly JoinDate { get; set; }
    }
}

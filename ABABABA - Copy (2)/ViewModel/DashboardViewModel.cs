using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.ViewModel
{
    class DashboardViewModel
    {
        public User CurrentUser { get; private set; }

        public DashboardViewModel(User user)
        {
            // Assign the logged-in user to a property
            CurrentUser = user;

            // You can now use CurrentUser.Role, CurrentUser.Username, etc.
        }
    }
}

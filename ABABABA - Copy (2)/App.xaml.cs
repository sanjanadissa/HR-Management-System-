using System.Configuration;
using System.Data;
using System.Windows;

namespace WpfApp1;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Ensure the leave request table exists
        DatabaseHelper.InitializeDatabase();

        // Optionally start your main window
       
    }
}


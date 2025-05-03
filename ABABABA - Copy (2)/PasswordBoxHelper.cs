using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public static class PasswordBoxHelper
    {
        public static readonly DependencyProperty AttachProperty =
            DependencyProperty.RegisterAttached("Attach", typeof(bool), typeof(PasswordBoxHelper), new PropertyMetadata(false, Attach));

        public static bool GetAttach(DependencyObject dp) => (bool)dp.GetValue(AttachProperty);
        public static void SetAttach(DependencyObject dp, bool value) => dp.SetValue(AttachProperty, value);

        private static void Attach(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                passwordBox.PasswordChanged += (s, args) =>
                {
                    SetPassword(passwordBox, passwordBox.Password);
                };
            }
        }

        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.RegisterAttached("Password", typeof(string), typeof(PasswordBoxHelper), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static string GetPassword(DependencyObject dp) => (string)dp.GetValue(PasswordProperty);
        public static void SetPassword(DependencyObject dp, string value) => dp.SetValue(PasswordProperty, value);
    }
}

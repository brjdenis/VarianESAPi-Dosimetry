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

namespace Dosimetry
{
    /// <summary>
    /// Interaction logic for EditXMLPassword.xaml
    /// </summary>
    public partial class EditXMLPassword : Window
    {
        public string passwordCorrect = "";

        public EditXMLPassword()
        {
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            this.Background = Brushes.AliceBlue;
            EditXMLPasswordBox.Focus();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string passsword = EditXMLPasswordBox.Password;
            if (passsword == "varian")
            {
                passwordCorrect = "true";
                this.Close();
            }
            else
            {
                passwordCorrect = "false";
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void hasPasswordChanged(object sender, RoutedEventArgs e)
        {

            if (EditXMLPasswordBox.Password.ToString() == "varian")
            {
                EditXMLPasswordSucessLabel.Content = "\u2714";
                EditXMLPasswordSucessLabel.Foreground = Brushes.Green;
            }
            else
            {
                EditXMLPasswordSucessLabel.Content = "\u274C";
                EditXMLPasswordSucessLabel.Foreground = Brushes.Red;
            }
        }
    }
}

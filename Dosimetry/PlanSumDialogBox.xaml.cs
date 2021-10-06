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
    /// Interaction logic for PlanSumDialog.xaml
    /// </summary>
    public partial class PlanSumDialogBox : Window
    {

        private string selectedplansumId = "";

        public void AddComboList(List<string> plansums)
        {
            foreach (var p in plansums)
            {
                comboBox1.Items.Add(p);
            }
        }

        public PlanSumDialogBox()
        {
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (comboBox1.SelectedItem != null)
            {
                this.selectedplansumId = this.comboBox1.SelectedItem.ToString();
            }
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public String getComboSelection()
        {
            return this.selectedplansumId;
        }
    }
}

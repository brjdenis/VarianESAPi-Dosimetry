using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace Dosimetry
{
    /// <summary>
    /// Interaction logic for EQDCalculator.xaml
    /// </summary>
    public partial class EQDCalculator : Window
    {
        public EQDCalculator()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Background = Brushes.LightBlue;
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox txt = sender as TextBox;
            int ind = txt.CaretIndex;
            txt.Text = txt.Text.Replace(",", ".");
            txt.CaretIndex = ind;
        }

        private double ConvertToDouble(string number)
        {
            if (number == "")
            {
                return 0;
            }
            double value;
            bool canConvert = double.TryParse(number, out value);
            if (canConvert)
            {
                return value;
            }
            else
            {
                return Double.NaN;
            }
        }

        private void LostFocusCell(object sender, RoutedEventArgs e)
        {
            double n1 = ConvertToDouble(this.Tab1TextBoxColumn1Row1.Text);
            double n2 = ConvertToDouble(this.Tab1TextBoxColumn1Row2.Text);
            double n3 = ConvertToDouble(this.Tab1TextBoxColumn1Row3.Text);

            double d1 = ConvertToDouble(this.Tab1TextBoxColumn2Row1.Text);
            double d2 = ConvertToDouble(this.Tab1TextBoxColumn2Row2.Text);
            double d3 = ConvertToDouble(this.Tab1TextBoxColumn2Row3.Text);

            double ab1 = ConvertToDouble(this.Tab1TextBoxColumn4Row1.Text);
            double ab2 = ConvertToDouble(this.Tab1TextBoxColumn4Row2.Text);
            double ab3 = ConvertToDouble(this.Tab1TextBoxColumn4Row3.Text);

            List<double> n = new List<double>() { n1, n2, n3 };

            this.Tab1TextBlockColumn1Row4.Text = (n1 + n2 + n3).ToString(CultureInfo.InvariantCulture);

            this.Tab1TextBlockColumn3Row1.Text = (n1 * d1).ToString(CultureInfo.InvariantCulture);
            this.Tab1TextBlockColumn3Row2.Text = (n2 * d2).ToString(CultureInfo.InvariantCulture);
            this.Tab1TextBlockColumn3Row3.Text = (n3 * d3).ToString(CultureInfo.InvariantCulture);

            this.Tab1TextBlockColumn3Row4.Text = (n1 * d1 + n2 * d2 + n3 * d3).ToString(CultureInfo.InvariantCulture);

            double eqd1 = n1 * d1 * (ab1 + d1) / (ab1 + 2.0);
            double eqd2 = n2 * d2 * (ab2 + d2) / (ab2 + 2.0);
            double eqd3 = n3 * d3 * (ab3 + d3) / (ab3 + 2.0);
            this.Tab1TextBlockColumn5Row1.Text = eqd1.ToString("0.##", CultureInfo.InvariantCulture);
            this.Tab1TextBlockColumn5Row2.Text = eqd2.ToString("0.##", CultureInfo.InvariantCulture);
            this.Tab1TextBlockColumn5Row3.Text = eqd3.ToString("0.##", CultureInfo.InvariantCulture);
            this.Tab1TextBlockColumn5Row4.Text = (eqd1 + eqd2 + eqd3).ToString("0.##", CultureInfo.InvariantCulture);
        }

        private void LostFocusCell2(object sender, RoutedEventArgs e)
        {
            double n = ConvertToDouble(this.Tab2TextBoxColumn0Row1.Text);
            double ab = ConvertToDouble(this.Tab2TextBlockColumn3Row1.Text);
            double eqd = ConvertToDouble(this.Tab2TextBoxColumn4Row1.Text);

            double d = (-ab * n + Math.Sqrt(ab * ab * n * n + 4 * n * eqd * (ab + 2.0))) / (2 * n);
            double D = n * d;
            this.Tab2TextBoxColumn1Row1.Text = d.ToString("0.##", CultureInfo.InvariantCulture);
            this.Tab2TextBoxColumn2Row1.Text = D.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private void LostFocusCell3(object sender, RoutedEventArgs e)
        {
            double d = ConvertToDouble(this.Tab3TextBoxColumn1Row1.Text);
            double ab = ConvertToDouble(this.Tab3TextBlockColumn3Row1.Text);
            double eqd = ConvertToDouble(this.Tab3TextBoxColumn4Row1.Text);

            double n = eqd * (ab + 2.0) / (d * (ab + d));
            this.Tab3TextBlockColumn0Row1.Text = n.ToString("0.##", CultureInfo.InvariantCulture);
            this.Tab3TextBoxColumn2Row1.Text = (n*d).ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}

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

namespace Dosimetry
{
    /// <summary>
    /// Interaction logic for AdminSQL.xaml
    /// </summary>
    /// 

    public partial class AdminSQL : Window
    {
        public SqlQuery sql;

        public AdminSQL(SqlQuery sql)
        {
            InitializeComponent();
            this.sql = sql;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Background = Brushes.AliceBlue;
        }

        public class PatientID
        {
            public string patientID { get; set; }
            public string tablename { get; set; }
            public string datetime { get; set; }
            public string lastsaver { get; set; }
            public string normalization { get; set; }

            public override string ToString()
            {
                return this.patientID.ToString() + " -- " + this.tablename.ToString() + " -- " + this.datetime.ToString() + " -- " + this.lastsaver.ToString();
            }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string patientOLD = this.TextBoxOldID.Text;
            string patientNEW = this.TextBoxNewID.Text;
            //this.sql.ChangeID(patientOLD, patientNEW);
            try
            {
                this.sql.ChangeID(patientOLD, patientNEW);
                this.SuccesLabel.Content = "Sql transaction worked!";
                this.SuccesLabel.Foreground = Brushes.Green;
            }
            catch
            {
                this.SuccesLabel.Content = "Sql transaction did not work!";
                this.SuccesLabel.Foreground = Brushes.Red;
            }

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string searchString = this.PatientIDSearchTextBox.Text;

            List<List<string>> patientids = sql.GetPatientIDs(searchString);
            List<PatientID> patientidList = new List<PatientID>() { };

            foreach (var p in patientids)
            {
                PatientID temp = new PatientID()
                {
                    patientID = p[0],
                    tablename = p[1],
                    datetime = p[2],
                    lastsaver = p[3],
                    normalization = p[4]
                };
                patientidList.Add(temp);
            }

            ListCollectionView collectionView1 = new ListCollectionView(patientidList);
            collectionView1.GroupDescriptions.Add(new PropertyGroupDescription("PatientID"));
            this.DataGrid.ItemsSource = collectionView1;
        }
    }
}

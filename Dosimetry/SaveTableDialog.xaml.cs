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
using System.Globalization;

namespace Dosimetry
{
    /// <summary>
    /// Interaction logic for SaveTableDialog.xaml
    /// </summary>
    public partial class SaveTableDialog : Window
    {
        public SqlQuery sql;
        public string TableName;
        public string PatientID;
        public string LastSaver;
        public string DataGridOrgans;
        public string DataGridPTV1;
        public string DataGridPTV2;
        public string Normalization;

        public SaveTableDialog(SqlQuery sql, string TableName, string PatientID,
            string LastSaver, string DataGridOrgans, string DataGridPTV1, string DataGridPTV2, string Normalization)
        {
            this.sql = sql;
            this.TableName = TableName;
            this.PatientID = PatientID;
            this.LastSaver = LastSaver;
            this.DataGridOrgans = DataGridOrgans;
            this.DataGridPTV1 = DataGridPTV1;
            this.DataGridPTV2 = DataGridPTV2;
            this.Normalization = Normalization;

            InitializeComponent();

            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Background = Brushes.AliceBlue;
            this.TableNameTextBox.Focus();
            this.TableNameTextBox.Text = TableName;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.TableNameTextBox.Text == "")
            {
                MessageBox.Show("Name is empty.", "Error");
                return;
            }

            try
            {
                string datetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.CreateSpecificCulture("de-DE"));
                // check if it exists. If it does, update table. otherwise insert new row.
                if (this.sql.CanAddTable(this.PatientID, this.TableNameTextBox.Text))
                {
                    sql.AddNewTable(this.PatientID, this.TableNameTextBox.Text, datetime, this.LastSaver,
                        this.DataGridOrgans, this.DataGridPTV1, this.DataGridPTV2, this.Normalization);

                    this.Close();
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show("Do you really want to replace data in the table", "Warning", MessageBoxButton.YesNo);
                    switch (result)
                    {
                        case MessageBoxResult.Yes:
                            sql.UpdateTable(this.PatientID, this.TableNameTextBox.Text, datetime, this.LastSaver,
                                            this.DataGridOrgans, this.DataGridPTV1, this.DataGridPTV2, this.Normalization);
                            this.Close();
                            break;
                        case MessageBoxResult.No:
                            break;
                    }

                }
            }
            catch (Exception f)
            {
                MessageBox.Show("Error while saving.\n" + f.Message, "Error");
                this.Close();
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void IsTableNameAvailable(object sender, RoutedEventArgs e)
        {
            bool canadd = false;
            try
            {
                canadd = sql.CanAddTable(this.PatientID, this.TableNameTextBox.Text);
            }
            catch (Exception g)
            {
                MessageBox.Show("Cannot read from the database.\n" + g.Message, "Error");
            }

            if (canadd)
            {
                TableNameTextBoxSuccess.Content = "New";
                TableNameTextBoxSuccess.Foreground = Brushes.Green;
            }
            else
            {
                TableNameTextBoxSuccess.Content = "Already exists";
                TableNameTextBoxSuccess.Foreground = Brushes.Red;
            }


        }
    }
}

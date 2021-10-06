// Import dosimetry datagrid elements from protocol.
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
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
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace Dosimetry
{
    /// <summary>
    /// Interaction logic for ImportFromProtocol.xaml
    /// </summary>
    public partial class ImportFromProtocol : Window
    {
        public Dosimetry dosimetry;
        public MainWindow dosimetryPanel;
        public Dictionary<string, List<string>> protocolgroupsDisplay;
        public Dictionary<string, List<string>> protocolgroupsFull;

        public int selectedindex = 0;
        public int selectedindex2 = 0;

        public bool checkBoxInclude = false;

        public List<DataGridOrgans> DataGridOrgansList = new List<DataGridOrgans>() { };
        public ListCollectionView DataGridOrgansCollection { get; set; }

        public List<DataGridOrgans> DataGridOrgansListDialogResult = new List<DataGridOrgans>() { };



        public ImportFromProtocol(Dosimetry dosimetry, MainWindow dosimetryPanel)
        {
            this.dosimetry = dosimetry;
            this.dosimetryPanel = dosimetryPanel;
            var protocolgroups = this.dosimetry.GetProtocolGroup();

            this.protocolgroupsDisplay = protocolgroups.Item1;
            this.protocolgroupsFull = protocolgroups.Item2;

            this.MaxWidth = System.Windows.SystemParameters.FullPrimaryScreenWidth;
            this.MaxHeight = System.Windows.SystemParameters.FullPrimaryScreenHeight;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            InitializeComponent();

            this.TextBoxNumFractions.Text = this.dosimetry.numberoffractions.First().ToString();

            SetupComboBoxes();
        }

        public class DataGridOrgans
        {
            public int id { get; set; }
            public bool Include { get; set; }
            public string Organ { get; set; }
            public string Structure { get; set; }
            public string ObjectiveType { get; set; }
            public string AtValue { get; set; }
            public string AtUnit { get; set; }
            public string ObjectiveExp { get; set; }
            public string ThanValue { get; set; }
            public string ThanUnit { get; set; }
            public string Comment { get; set; }
        }

        protected void SetupComboBoxes()
        {
            this.ComboBox1.ItemsSource = this.protocolgroupsDisplay.Keys;
            this.ComboBox1.SelectedIndex = this.selectedindex;
            this.ComboBox1.SelectionChanged += new SelectionChangedEventHandler(onSelectComboBox1);

            this.ComboBox2.ItemsSource = this.protocolgroupsDisplay.First().Value;
            this.ComboBox2.SelectedIndex = this.selectedindex2;
        }

        protected void onSelectComboBox1(object sender, EventArgs e)
        {
            this.selectedindex = this.ComboBox1.SelectedIndex;
            this.ComboBox2.ItemsSource = this.protocolgroupsDisplay[this.ComboBox1.SelectedItem.ToString()];
            this.ComboBox2.SelectedIndex = 0;
            this.selectedindex2 = 0;
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.ComboBox2.SelectedIndex == -1)
            {
                return;
            }
            else
            {
                AddDataToDataGrid();
            }
        }

        private void AddDataToDataGrid()
        {
            List<DataGridOrgans> data = new List<DataGridOrgans>() { };
            List<Objective> objectiveList = this.dosimetry.GetProtocol(this.protocolgroupsFull[this.ComboBox1.SelectedItem.ToString()][this.ComboBox2.SelectedIndex]);
            int i = 0;
            foreach (Objective ob in objectiveList)
            {
                if (ob.structuretype == "Organ")
                {
                    string atValue = "";
                    if (!Double.IsNaN(ob.at_value))
                    {
                        // Conversion to Nominal dose is done automatically by Objectives.cs for at_value,
                        // but not for than_value
                        // Here At value is recalculated using a seperate EQD2 conversion based on the num fractions in TextBoxNumfractions
                        // change from EQD2 to normal (nasty way of doing it)
                        if (ob.at.Contains("cGy2"))
                        {
                            var oldAt = Convert.ToDouble(ob.at.Replace("cGy2", ""), System.Globalization.CultureInfo.InvariantCulture);
                            if (ConvertTextToDouble(this.TextBoxNumFractions.Text) > 0)
                            {
                                ob.at_value = Eqd2ToNominal(oldAt, ob);
                            }
                            else
                            {
                                ob.at_value = 0;
                            }
                        }
                        
                        atValue = ob.at_value.ToString("0.#", CultureInfo.InvariantCulture);
                    }

                    string atUnit = "";
                    if (ob.at.Contains("cGy") || ob.at.Contains("cGy2"))
                    {
                        atUnit = "cGy";
                    }
                    else if (ob.at.Contains("%"))
                    {
                        atUnit = "%";
                    }
                    else if (ob.at.Contains("cm3"))
                    {
                        atUnit = "cm3";
                    }

                    string thanValue = "";
                    if (!Double.IsNaN(ob.than_value))
                    {
                        if (ob.than.Contains("cGy2"))
                        {
                            if (ConvertTextToDouble(this.TextBoxNumFractions.Text) > 0)
                            {
                                ob.than_value = Eqd2ToNominal(ob.than_value, ob);
                            }
                            else
                            {
                                ob.than_value = 0;
                            }

                        }
                        thanValue = ob.than_value.ToString("0.#", CultureInfo.InvariantCulture);
                    }

                    string thanUnit = "";
                    if (ob.than.Contains("cGy") || ob.than.Contains("cGy2"))
                    {
                        thanUnit = "cGy";
                    }
                    else if (ob.than.Contains("%"))
                    {
                        thanUnit = "%";
                    }
                    else if (ob.than.Contains("cm3"))
                    {
                        thanUnit = "cm3";
                    }

                    Structure structure = ob.FindStructure(this.dosimetry.structureset, objectiveList);
                    string structureName = "";
                    if (structure != (Structure)null)
                    {
                        structureName = structure.Id;
                    }

                    DataGridOrgans item = new DataGridOrgans()
                    {
                        id = i,
                        Include = true,
                        Organ = ob.fullstructurename,
                        Structure = structureName,
                        ObjectiveType = ob.type,
                        AtValue = atValue,
                        AtUnit = atUnit,
                        ObjectiveExp = ob.exp == "less" ? "<" : ">",
                        ThanValue = thanValue,
                        ThanUnit = thanUnit,
                        Comment = ob.comment
                    };

                    data.Add(item);
                    i++;
                }
            }
            List<string> structureList = this.dosimetryPanel.CollectStructures();
            structureList.Insert(0, "");
            this.DataGridStructure.ItemsSource = structureList;

            this.DataGridOrgansList = data;
            ListCollectionView collectionView = new ListCollectionView(this.DataGridOrgansList);
            collectionView.GroupDescriptions.Add(new PropertyGroupDescription("Organ"));
            this.DataGridOrgansCollection = collectionView;
            this.DataGrid1.ItemsSource = this.DataGridOrgansCollection;
        }

        private void DataGrid_SourceUpdated(object sender, DataTransferEventArgs e)
        {
        }


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

            if (this.DataGrid1.SelectedItems.Count > 0)
            {
                var selectedItems = this.DataGrid1.SelectedItems;
                List<DataGridOrgans> items = selectedItems.Cast<DataGridOrgans>().ToList();
                List<int> rowNumbers = new List<int>() { };
                foreach (var item in items)
                {
                    rowNumbers.Add(item.id);
                }
                ChangeIncludeCheckBox(rowNumbers);

                List<DataGridOrgans> allItems = this.DataGrid1.Items.Cast<DataGridOrgans>().ToList();
                for (int i = 0; i < this.DataGrid1.Items.Count; i++)
                {
                    if (rowNumbers.Contains(allItems[i].id))
                    {
                        DataGridRow visualItem = (DataGridRow)this.DataGrid1.ItemContainerGenerator.ContainerFromItem(this.DataGrid1.Items[i]);
                        if (visualItem == null) break;
                        visualItem.IsSelected = true;
                        visualItem.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    }
                }
            }
        }


        private void ChangeIncludeCheckBox(List<int> rowNumbers)
        {
            foreach (var row in rowNumbers)
            {
                this.DataGridOrgansList.ElementAt(row).Include = this.checkBoxInclude;
            }
            this.checkBoxInclude = !this.checkBoxInclude;

            this.DataGridOrgansCollection.Refresh();
            this.DataGrid1.UpdateLayout();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var selectedItems = this.DataGrid1.SelectedItems;
            List<DataGridOrgans> items = selectedItems.Cast<DataGridOrgans>().ToList();

            List<int> rowNumbers = new List<int>() { };
            foreach (var item in items)
            {
                rowNumbers.Add(item.id);
            }

            if (items.Count != 1)
            {
                return;
            }
            else
            {
                string organ = this.DataGridOrgansList.ElementAt(items.First().id).Organ;
                string structure = this.DataGridOrgansList.ElementAt(items.First().id).Structure;
                ChangeStructuresComboBox(organ, structure);

                List<DataGridOrgans> allItems = this.DataGrid1.Items.Cast<DataGridOrgans>().ToList();
                for (int i = 0; i < this.DataGrid1.Items.Count; i++)
                {
                    if (rowNumbers.Contains(allItems[i].id))
                    {
                        DataGridRow visualItem = (DataGridRow)this.DataGrid1.ItemContainerGenerator.ContainerFromItem(this.DataGrid1.Items[i]);
                        if (visualItem == null) break;
                        visualItem.IsSelected = true;
                        visualItem.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    }
                }
            }
        }

        private void ChangeStructuresComboBox(string organ, string structure)
        {
            foreach (var row in this.DataGridOrgansList)
            {
                if (row.Organ == organ)
                {
                    row.Structure = structure;
                }
            }
            this.DataGridOrgansCollection.Refresh();
            this.DataGrid1.UpdateLayout();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            this.DataGridOrgansListDialogResult = this.DataGridOrgansList;
            this.Close();
        }



        private double ConvertTextToDouble(string text)
        {
            if (Double.TryParse(text, out double result))
            {
                return result;
            }
            else
            {
                return Double.NaN;
            }
        }

        public double Eqd2ToNominal(double EQD, Objective ob)
        {
            // convert EQD to nominal dose
            double result;
            double n = ConvertTextToDouble(this.TextBoxNumFractions.Text);

            if (n > 0 && !Double.IsNaN(n))
            {
                double ab = this.dosimetry.StructureAlphaBeta[ob.structure];
                result = -n * ab / 2.0 + Math.Sqrt((n * ab / 2.0) * (n * ab / 2.0) + n * (EQD / 100.0) * (ab + 2.0));
                result *= 100.0;
            }
            else
            {
                result = EQD;
            }
            return result;
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            string protocol = this.protocolgroupsFull[this.ComboBox1.SelectedItem.ToString()][this.ComboBox2.SelectedIndex];
            string protocolName = this.ComboBox1.SelectedItem.ToString() + ": " + this.ComboBox2.SelectedItem.ToString();
            var dataGridProtocolViews = this.dosimetryPanel.ReadFromProtocol(protocol);

            ProtocolView protocolview = new ProtocolView(dataGridProtocolViews, protocolName);
            protocolview.Owner = this;
            protocolview.Show();
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            try
            {
                EQDCalculator eqdcalc = new EQDCalculator();
                eqdcalc.Owner = this;
                eqdcalc.Show();
            }
            catch (Exception g)
            {
                MessageBox.Show("Something went wrong. \n" + g.ToString(), "Error");
                return;
            }
        }
    }
}

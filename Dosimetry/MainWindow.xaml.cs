// this is the main window that is openned when the script starts
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Serialization;
using VMS.TPS.Common.Model.Types;

namespace Dosimetry
{
    /// <summary>
    /// Interaction logic for DosimetryPanel.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Dosimetry dosimetry;
        public Dictionary<string, List<string>> protocolgroupsDisplay;
        public Dictionary<string, List<string>> protocolgroupsFull;
        public Dictionary<string, List<List<double>>> BodyMinusPTVTables;

        public int selectedindex = 0;
        public int selectedindex2 = 0;

        public double DoseNormalization; // only used with organs (general dosimetry)
        public double DoseNormalizationSpecial; // only used with organs (special dosimetry)
        public double PrescribedDose;

        public List<string> missingstructures;
        public List<string> structuredosecoverOrgans;
        public List<string> structuredosecoverPTVs;
        public List<string> structuredosecoverFull;

        public List<Objective> objectivesOrgans;
        public List<string> namesOrgans;
        public List<List<Objective>> objectiveGroupsOrgans;
        public List<string> namesfromstructuresetOrgans;

        public List<Objective> objectivesPTVsFromProtocol; // The original objectives from protocol

        public List<string> namesPTVs;
        public List<List<Objective>> objectiveGroupsPTVs;
        public List<string> namesfromstructuresetPTVs;
        public List<double> doseValuesPTVs;
        public List<DataGridPTVs> DataGridPTVsCollection { get; set; }
        public List<string> DataGridPTVsCombobox { get; set; }
        public List<string> DataGridReferencePTVsCombobox { get; set; }

        public List<DataGridSpecialOrgans> DataGridSpecialOrgansList;
        public ListCollectionView DataGridSpecialOrgansCollection { get; set; }

        public List<DataGridSpecialPTVs1> DataGridSpecialPTV1List;
        public ListCollectionView DataGridSpecialPTV1Collection { get; set; }

        public List<DataGridSpecialPTVs2> DataGridSpecialPTV2List;
        public ListCollectionView DataGridSpecialPTV2Collection { get; set; }

        public List<ComboBoxDataTable> ComboBoxDataTablesList;

        public SqlQuery sql;

        public string DosimetryVersion = "";

        public List<string> ComboBoxSqlList { get; set; }


        public MainWindow(Dosimetry dosimetry)
        {
            var version = Assembly.GetCallingAssembly().GetName().Version.ToString();
            this.DosimetryVersion = version;
            this.Title = "Dosimetry " + version;

            this.dosimetry = dosimetry;
            this.sql = new SqlQuery(this.dosimetry.databasePath);

            // Read protocol groups
            var protocolgroups = this.dosimetry.GetProtocolGroup();

            this.protocolgroupsDisplay = protocolgroups.Item1;
            this.protocolgroupsFull = protocolgroups.Item2;

            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            //this.SizeToContent = SizeToContent.WidthAndHeight;

            // Prevent too large windows:
            this.MaxWidth = System.Windows.SystemParameters.FullPrimaryScreenWidth;
            this.MaxHeight = System.Windows.SystemParameters.FullPrimaryScreenHeight;

            InitializeComponent();

            PreviewKeyDown += (s, e) => { if (e.Key == Key.F2) Close(); };

            // Add plan name and fractionation to PlanLabel
            string planlabelText;
            if (this.dosimetry.planningitem == null)
            {
                planlabelText = "Plan: ";
            }
            else
            {
                planlabelText = this.dosimetry.planningitem.Id;
            }

            double DoseSum = 0;
            for (int i = 0; i < this.dosimetry.doseperfractions.Count(); i++)
            {
                planlabelText += ", " + String.Format("{0} \u00D7 {1} Gy = {2} Gy ({3})", this.dosimetry.numberoffractions[i], this.dosimetry.doseperfractions[i] / 100.0, this.dosimetry.numberoffractions[i] * this.dosimetry.doseperfractions[i] / 100.0, this.dosimetry.targetvolumes[i]);
                DoseSum += this.dosimetry.numberoffractions[i] * this.dosimetry.doseperfractions[i];
            }
            this.DosimetryPlanLabel.Content = planlabelText;
            this.DosimetryPlanLabel.FontWeight = FontWeights.Bold;

            this.DosimetrySpecialPlanLabel.Content = planlabelText;
            this.DosimetrySpecialPlanLabel.FontWeight = FontWeights.Bold;

            this.PrescribedDose = DoseSum;
            this.TextBoxNormalization.Text = DoseSum.ToString(CultureInfo.InvariantCulture);
            this.ButtonNormalization.Content = "\u2794";

            this.DoseNormalization = ConvertTextToDouble(this.TextBoxNormalization.Text) / this.PrescribedDose;

            this.TextBoxSpecialNormalization.Text = DoseSum.ToString(CultureInfo.InvariantCulture);
            this.ButtonSpecialNormalization.Content = "\u2794";

            this.DoseNormalizationSpecial = ConvertTextToDouble(this.TextBoxNormalization.Text) / this.PrescribedDose;

            this.ButtonSpecialFillTargetObjectives.Content = "Fill " + "\u2193";

            InitializeDataGridSpecialOrgans();
            InitializeDataGridSpecialPTVs1();
            InitializeDataGridSpecialPTVs2();

            // Load and catch
            SetupComboBoxes();

            bool doesSQLWork = InitiateSQL();

            if (this.dosimetry.runType == "Dosimetry" & doesSQLWork)
            {
                if (this.ComboBoxDataTablesList.Count > 0)
                {
                    ShowSavedTable();
                    this.TabControlDosimetry.SelectedIndex = 1;
                }
            }
            else
            {
                this.TabControlDosimetry.SelectedIndex = 0;
                LoadAndCatch();
                DisableButtonsNonDosimetry();
            }
        }


        private void DisableButtonsNonDosimetry()
        {
            //this.ButtonGetFromProtocol.IsEnabled = false;
            this.ButtonCreateOptObjectives.IsEnabled = false;
            this.ButtonCreatePlan.IsEnabled = false;
            this.ButtonSaveTable.IsEnabled = false;
            this.ButtonDeleteTable.IsEnabled = false;

        }

        public class DataGridOrgans
        {
            public string Organ { get; set; }
            public string Structure { get; set; }
            public string Objective { get; set; }
            public string Value { get; set; }
            public string Comment { get; set; }
            public string Passed { get; set; }
            public string PassedColor { get; set; }
        }


        public class DataGridPTVs
        {
            public string Structure { get; set; }
            public string Dose { get; set; }
            public string StructureFromProtocol { get; set; }
            public string ReferenceStructure { get; set; }
        }

        public class DataGridProtocolView
        {
            // for displaying protocols in readable form
            public string Type { get; set; }
            public string Name { get; set; }
            public string Objective { get; set; }
            public string Comment { get; set; }
        }



        protected void SetupComboBoxes()
        {
            this.DosimetryComboBox1.ItemsSource = this.protocolgroupsDisplay.Keys;
            this.DosimetryComboBox1.SelectedIndex = this.selectedindex;
            this.DosimetryComboBox1.SelectionChanged += new SelectionChangedEventHandler(onSelectComboBox1);

            this.DosimetryComboBox2.ItemsSource = this.protocolgroupsDisplay.First().Value;
            this.DosimetryComboBox2.SelectedIndex = this.selectedindex2;
            this.DosimetryComboBox2.SelectionChanged += new SelectionChangedEventHandler(onSelectComboBox2);
        }

        protected void onSelectComboBox1(object sender, EventArgs e)
        {
            this.selectedindex = this.DosimetryComboBox1.SelectedIndex;
            this.DosimetryComboBox2.ItemsSource = this.protocolgroupsDisplay[this.DosimetryComboBox1.SelectedItem.ToString()];
            this.DosimetryComboBox2.SelectedIndex = 0;
            this.selectedindex2 = 0;
        }

        protected void onSelectComboBox2(object sender, EventArgs e)
        {
            if (this.DosimetryComboBox2.SelectedIndex == -1)
            {
                return;
            }
            LoadAndCatch();
        }


        protected void LoadAndCatch()
        {
            try
            {
                changeData();
                UpdateMissingStructureLabel();
                JoinDoseCoverPTVsAndOrgans();
                UpdateDoseCoverLabel();
                UpdateOrgansDataGrid();
                UpdatePTV1DataGrid();
                //UpdatePTV2DataGrid();
            }
            catch (KeyNotFoundException ex)
            {
                MessageBox.Show("Unknown problem. Most likely the organs are not mapped properly. \n \n" + ex, "Error");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unknown problem. \n \n" + ex, "Error");
            }
        }

        protected void changeData()
        {
            // Collect just organs from the full list that contains also PTVs
            this.objectivesOrgans = new List<Objective>() { };
            this.objectivesPTVsFromProtocol = new List<Objective>() { };

            foreach (Objective obje in this.dosimetry.GetProtocol(this.protocolgroupsFull[this.DosimetryComboBox1.SelectedItem.ToString()][this.DosimetryComboBox2.SelectedIndex]))
            {
                if (obje.structuretype == "Organ")
                {
                    this.objectivesOrgans.Add(obje);
                }
                else
                {
                    this.objectivesPTVsFromProtocol.Add(obje);
                }
            }

            // Add default PTV objectives if none are defined in the protcol:
            CreateDefaultObjective(this.protocolgroupsFull[this.DosimetryComboBox1.SelectedItem.ToString()][this.DosimetryComboBox2.SelectedIndex]);

            this.namesOrgans = new List<string> { }; // name of organ from protocol
            this.namesfromstructuresetOrgans = new List<string> { }; // name of structure that corresponds to organ from protocol
            this.objectiveGroupsOrgans = new List<List<Objective>> { };

            this.structuredosecoverOrgans = new List<string> { };
            this.structuredosecoverPTVs = new List<string> { };

            this.missingstructures = new List<string> { };

            // collect objectives into groups
            foreach (var ob in this.objectivesOrgans)
            {
                ob.CalculateObjectiveOrgan(this.dosimetry.structureset, this.objectivesOrgans, this.DoseNormalization);
            }
            // Names = names of organs from protocol
            foreach (var ob in this.objectivesOrgans)
            {
                if (ob.exists == true)
                {
                    this.namesOrgans.Add(ob.fullstructurename);
                }
                else
                {
                    this.missingstructures.Add(ob.fullstructurename);
                }
            }


            this.namesOrgans = this.namesOrgans.Distinct().ToList();
            this.missingstructures = this.missingstructures.Distinct().ToList();

            // Names from structureset
            foreach (var name in this.namesOrgans)
            {
                this.objectiveGroupsOrgans.Add(this.objectivesOrgans.Where(x => x.fullstructurename == name).ToList());
            }

            foreach (var obj in objectiveGroupsOrgans)
            {
                this.namesfromstructuresetOrgans.Add(obj.ElementAt(0).structureFromSet.Id);

                if (obj.ElementAt(0).dosecover < 1.0)
                {
                    this.structuredosecoverOrgans.Add(obj.ElementAt(0).structureFromSet.Id);
                }
            }

            // .................... PTV names from protocol ....................
            this.namesPTVs = new List<string> { }; // name of organ from protocol
            foreach (var ob in this.objectivesPTVsFromProtocol)
            {
                this.namesPTVs.Add(ob.fullstructurename);
            }
            this.namesPTVs = this.namesPTVs.Distinct().ToList();

            UpdateOrgansDataGrid();
        }


        protected void UpdateOrgansDataGrid()
        {
            List<DataGridOrgans> dataList1 = new List<DataGridOrgans>();
            List<DataGridOrgans> dataList2 = new List<DataGridOrgans>();

            for (int i = 0; i < this.namesOrgans.Count(); i++)
            {
                for (int j = 0; j < this.objectiveGroupsOrgans[i].Count(); j++)
                {
                    Objective ob = this.objectiveGroupsOrgans[i][j];

                    string passedSymbol = ob.haspassedmark;
                    string passedSymbolColor;

                    if (ob.haspassed == 0)
                    {
                        passedSymbolColor = "Red";
                    }
                    else if (ob.haspassed == 1)
                    {
                        passedSymbolColor = "Green";
                    }
                    else
                    {
                        passedSymbolColor = "Blue";
                    }

                    DataGridOrgans temp = new DataGridOrgans()
                    {
                        Organ = this.namesOrgans[i],
                        Structure = this.namesfromstructuresetOrgans.ElementAt(i),
                        Objective = ob.objectivestring,
                        Value = Math.Round(ob.value, 1).ToString(CultureInfo.InvariantCulture),
                        Comment = ob.comment,
                        Passed = passedSymbol,
                        PassedColor = passedSymbolColor
                    };
                    if (i % 2 == 0)
                    {
                        dataList1.Add(temp);
                    }
                    else
                    {
                        dataList2.Add(temp);
                    }
                }
            }
            ListCollectionView collectionView1 = new ListCollectionView(dataList1);
            collectionView1.GroupDescriptions.Add(new PropertyGroupDescription("Organ"));
            this.DataGridOrgans1.ItemsSource = collectionView1;

            ListCollectionView collectionView2 = new ListCollectionView(dataList2);
            collectionView2.GroupDescriptions.Add(new PropertyGroupDescription("Organ"));
            this.DataGridOrgans2.ItemsSource = collectionView2;
        }

        protected List<string> CollectPTVs()
        {
            List<string> structureList = new List<string>() { };
            foreach (var str in this.dosimetry.structureset.Structures)
            {
                if (!str.IsEmpty && (str.DicomType == "PTV" || str.DicomType == "CTV" || str.DicomType == "GTV" || str.DicomType == "IRRAD_VOLUME"))
                {
                    structureList.Add(str.Id);
                }
            }
            structureList.Sort();
            return structureList;
        }

        protected void JoinDoseCoverPTVsAndOrgans()
        {
            List<string> full = new List<string>() { };

            foreach (var dc in this.structuredosecoverOrgans)
            {
                full.Add(dc);
            }
            foreach (var dc in this.structuredosecoverPTVs)
            {
                full.Add(dc);
            }
            this.structuredosecoverFull = full;
        }


        protected void UpdatePTV1DataGrid()
        {
            List<string> PTVstructureList = CollectPTVs();
            this.DataGridPTVsCollection = new List<DataGridPTVs>();

            foreach (var s in PTVstructureList)
            {
                DataGridPTVs temp = new DataGridPTVs()
                {
                    Structure = s
                };
                this.DataGridPTVsCollection.Add(temp);
            }

            this.DataGridPTVsCombobox = this.namesPTVs;
            this.DataGridReferencePTVsCombobox = CollectPTVs();

            DataGridPTVs1.ItemsSource = this.DataGridPTVsCollection;
            DataGridPTVs1Protocol.ItemsSource = this.DataGridPTVsCombobox;
            DataGridPTVs1Ref.ItemsSource = this.DataGridReferencePTVsCombobox;
        }


        protected void UpdateMissingStructureLabel()
        {
            this.LabelMissingStructures.Content = this.missingstructures.Count().ToString();
            string labelmiss_message2 = "Missing organs:\n  " + string.Join(",\n  ", this.missingstructures);
            ToolTip tt3 = new ToolTip();
            tt3.Content = labelmiss_message2;
            this.LabelMissingStructures.ToolTip = tt3;
        }

        protected void UpdateDoseCoverLabel()
        {
            this.LabelDoseCover.Content = this.structuredosecoverFull.Count().ToString();

            //Dose cover warning tooltip
            string labeldosecover_message2 = "Incomplete dose coverage:\n  " + string.Join(",\n  ", this.structuredosecoverFull);
            ToolTip tt4 = new ToolTip();
            tt4.Content = labeldosecover_message2;
            this.LabelDoseCover.ToolTip = tt4;

            if (this.structuredosecoverFull.Count() != 0)
            {
                this.GroupBoxWarnings.BorderBrush = Brushes.Red;
                this.GroupBoxWarnings.BorderThickness = new Thickness(2);

                this.BorderDoseCover.BorderBrush = Brushes.Violet;
                this.BorderDoseCover.BorderThickness = new Thickness(1);
                this.BorderDoseCover.Background = Brushes.LightPink;
            }
            else
            {
                this.GroupBoxWarnings.BorderBrush = Brushes.Gray;
                this.GroupBoxWarnings.BorderThickness = new Thickness(1);

                this.BorderDoseCover.BorderBrush = Brushes.Black;
                this.BorderDoseCover.BorderThickness = new Thickness(1);
                this.BorderDoseCover.Background = Brushes.FloralWhite;
            }
        }

        protected void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected void CalculatePTVObjectives()
        {
            for (int i = 0; i < this.objectiveGroupsPTVs.Count; i++)
            {
                for (int j = 0; j < this.objectiveGroupsPTVs[i].Count; j++)
                {
                    this.objectiveGroupsPTVs[i][j].CalculateObjectivePTV(this.dosimetry.structureset, this.namesfromstructuresetPTVs[i], this.doseValuesPTVs[i]);
                }
            }
            UpdatePTV2DataGrid();
        }

        protected void UpdatePTV2DataGrid()
        {
            List<DataGridOrgans> dataList1 = new List<DataGridOrgans>();
            List<string> dosecoverPTVs = new List<string> { };
            int roundDigits = 1;

            for (int i = 0; i < this.namesfromstructuresetPTVs.Count; i++)
            {
                for (int j = 0; j < this.objectiveGroupsPTVs[i].Count; j++)
                {
                    Objective ob = this.objectiveGroupsPTVs[i][j];

                    string passedSymbol = ob.haspassedmark;
                    string passedSymbolColor;

                    if (ob.haspassed == 0)
                    {
                        passedSymbolColor = "Red";
                    }
                    else if (ob.haspassed == 1)
                    {
                        passedSymbolColor = "Green";
                    }
                    else
                    {
                        passedSymbolColor = "Blue";
                    }

                    if (ob.type == "R50" || ob.type == "R100")
                    {
                        roundDigits = 2;
                    }
                    else
                    {
                        roundDigits = 1;
                    }

                    DataGridOrgans temp = new DataGridOrgans()
                    {
                        Organ = this.namesfromstructuresetPTVs.ElementAt(i),
                        Structure = ob.fullstructurename,
                        Objective = ob.objectivestring,
                        Value = Math.Round(ob.value, roundDigits).ToString(CultureInfo.InvariantCulture),
                        Comment = ob.comment,
                        Passed = passedSymbol,
                        PassedColor = passedSymbolColor
                    };

                    dataList1.Add(temp);
                }

                if (this.objectiveGroupsPTVs[i].First().dosecover < 1.0)
                {
                    dosecoverPTVs.Add(this.namesfromstructuresetPTVs.ElementAt(i));
                }
            }
            ListCollectionView collectionView1 = new ListCollectionView(dataList1);
            collectionView1.GroupDescriptions.Add(new PropertyGroupDescription("Organ"));
            this.DataGridPTVs2.ItemsSource = collectionView1;

            this.structuredosecoverPTVs = dosecoverPTVs;
            JoinDoseCoverPTVsAndOrgans();
            UpdateDoseCoverLabel();
        }

        private bool ValidateDose(string dosestring)
        {
            double dose;
            var canConvert = double.TryParse(dosestring, out dose);

            if (canConvert && dose <= 0)
            {
                return false;
            }
            return canConvert;
        }

        protected Objective CopyProtocolObjective(Objective objOriginal)
        {
            /// This is needed becuase of the foolish way i implemented the creation of objectives
            Objective objNew = new Objective(
                        this.dosimetry,
                        objOriginal.protocol,
                        objOriginal.structure,
                        objOriginal.prv,
                        objOriginal.structuretype,
                        objOriginal.type,
                        objOriginal.at,
                        objOriginal.exp,
                        objOriginal.than,
                        objOriginal.comment
                        );
            return objNew;
        }

        protected void CreateDefaultObjective(string protocol)
        {
            // Create default criteria for PTV if none is defined in the protocol
            if (this.objectivesPTVsFromProtocol.Count < 1)
            {
                Objective objPTV1 = new Objective(
                        this.dosimetry,
                        protocol,
                        "PTV",
                        false,
                        "PTV",
                        "V",
                        "95%",
                        "more",
                        "98%",
                        ""
                        );
                Objective objPTV2 = new Objective(
                        this.dosimetry,
                        protocol,
                        "PTV",
                        false,
                        "PTV",
                        "V",
                        "107%",
                        "less",
                        "2%",
                        ""
                        );
                this.objectivesPTVsFromProtocol.Add(objPTV1);
                this.objectivesPTVsFromProtocol.Add(objPTV2);
            }
        }


        private void DosimetryPanelButtonCalculate_Click(object sender, RoutedEventArgs e)
        {
            //ReReadProtocolPTVs();
            this.namesfromstructuresetPTVs = new List<string> { };
            this.objectiveGroupsPTVs = new List<List<Objective>> { };
            this.doseValuesPTVs = new List<double> { };

            foreach (var p in this.DataGridPTVsCollection)
            {
                if (p.StructureFromProtocol != null && p.Dose != null && p.Dose != "" && p.Structure != null && p.Structure != "")
                {
                    if (ValidateDose(p.Dose) && p.StructureFromProtocol != "")
                    {
                        List<Objective> objectiveListNew = new List<Objective>() { };
                        List<Objective> objectiveListProtocol = this.objectivesPTVsFromProtocol.Where(x => x.fullstructurename == p.StructureFromProtocol).ToList();

                        foreach (var objProt in objectiveListProtocol)
                        {
                            Objective objNew = CopyProtocolObjective(objProt);

                            // if Ref Target column filed, define the auxReferenceStructureFromSet
                            if (p.ReferenceStructure != null && p.ReferenceStructure != "")
                            {
                                objNew.auxRefStructueFromSet = this.dosimetry.structureset.Structures.First(s => s.Id == p.ReferenceStructure);
                            }
                            objectiveListNew.Add(objNew);
                        }
                        this.objectiveGroupsPTVs.Add(objectiveListNew);
                        this.namesfromstructuresetPTVs.Add(p.Structure);
                        this.doseValuesPTVs.Add(Convert.ToDouble(p.Dose, System.Globalization.CultureInfo.InvariantCulture));
                    }
                }
            }

            if (this.objectiveGroupsPTVs.Count() > 0 && this.namesfromstructuresetPTVs.Count() > 0)
            {
                CalculatePTVObjectives();
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string protocol = this.protocolgroupsFull[this.DosimetryComboBox1.SelectedItem.ToString()][this.DosimetryComboBox2.SelectedIndex];

            List<DataGridProtocolView> dataGridProtocolViews = ReadFromProtocol(protocol);
            string protocolName = this.DosimetryComboBox1.SelectedItem.ToString() + ": " + this.DosimetryComboBox2.SelectedItem.ToString();
            try
            {
                ProtocolView protocolview = new ProtocolView(dataGridProtocolViews, protocolName);
                protocolview.Owner = this;
                protocolview.Show();
            }
            catch (Exception w)
            {
                MessageBox.Show("Something went wrong. \n" + w.ToString(), "Error");
            }


        }

        public List<DataGridProtocolView> ReadFromProtocol(string protocol)
        {
            // read protocol and display it in a window.
            List<DataGridProtocolView> dataGridProtocolViews = new List<DataGridProtocolView>() { };

            XmlDocument xml = new XmlDocument();

            using (FileStream fileStream = new FileStream(this.dosimetry.protocolPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                xml.Load(fileStream);
            }

            XmlNodeList resources = xml.DocumentElement.SelectNodes("/root/Protocol[@name='" + protocol + "']/Structure");

            foreach (XmlNode node_st in resources)
            {
                XmlNodeList nodes_st = node_st.SelectNodes("./Objective");
                foreach (XmlNode node_obj in nodes_st)
                {
                    string structurename = node_st.Attributes["name"].Value;
                    string structuretype = node_st.Attributes["type"].Value;
                    string type = node_obj.Attributes["type"].Value;
                    string at = node_obj.Attributes["at"].Value;
                    string exp = node_obj.Attributes["exp"].Value;
                    string than = node_obj.Attributes["than"].Value;

                    string exp2;

                    if (exp == "less")
                    {
                        exp2 = " < ";
                    }
                    else
                    {
                        exp2 = " > ";
                    }
                    string objectiveString = type + at + exp2 + than;

                    DataGridProtocolView pv = new DataGridProtocolView()
                    {
                        Type = structuretype,
                        Name = structurename,
                        Objective = objectiveString,
                        Comment = node_obj.Attributes["comment"].Value
                    };
                    dataGridProtocolViews.Add(pv);
                }
            }

            return dataGridProtocolViews;
        }


        private void ButtonNormalization_Click(object sender, RoutedEventArgs e)
        {
            this.DoseNormalization = ConvertTextToDouble(this.TextBoxNormalization.Text) / this.PrescribedDose;
            LoadAndCatch();
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

        //################################################################################################################
        //###########################################SPECIAL TAB##########################################################
        //################################################################################################################
        public List<string> CollectStructures()
        {
            List<string> structureList = new List<string>() { };
            foreach (var str in this.dosimetry.structureset.Structures)
            {
                if (!str.IsEmpty && str.DicomType != "PTV" && str.DicomType != "CTV" && str.DicomType != "GTV" &&
                    str.DicomType != "MARKER" && str.DicomType != "SUPPORT")
                {
                    structureList.Add(str.Id);
                }
            }
            structureList.Sort();
            return structureList;
        }

        public class DataGridSpecialOrgans
        {
            public string Structure { get; set; }

            public string objectiveType = "V";
            public string ObjectiveType
            {
                get { return objectiveType; }
                set { objectiveType = value; }
            }

            public string atValue = "";
            public string AtValue
            {
                get { return atValue; }
                set { atValue = value; }
            }

            public string atUnit = "cGy";
            public string AtUnit
            {
                get { return atUnit; }
                set { atUnit = value; }
            }

            public string objectiveExp = "<";
            public string ObjectiveExp
            {
                get { return objectiveExp; }
                set { objectiveExp = value; }
            }

            public string thanValue = "";

            public string ThanValue
            {
                get { return thanValue; }
                set { thanValue = value; }
            }

            public string thanUnit = "%";
            public string ThanUnit
            {
                get { return thanUnit; }
                set { thanUnit = value; }
            }

            public double calculatedValue = Double.NaN;
            public double CalculatedValue
            {
                get { return calculatedValue; }
                set { calculatedValue = value; }
            }

            public string comment = "";
            public string Comment
            {
                get { return comment; }
                set { comment = value; }
            }

            public string passed = "\u2753";
            public string Passed
            {
                get { return passed; }
                set { passed = value; }
            }
            public string passedColor = "Blue";
            public string PassedColor
            {
                get { return passedColor; }
                set { passedColor = value; }
            }

            private bool ValidateDose(string dosestring)
            {
                double dose;
                if (dosestring.Contains(","))
                {
                    return false;
                }
                var canConvert = double.TryParse(dosestring, out dose);

                if (canConvert & dose < 0 & Double.IsNaN(dose))
                {
                    return false;
                }
                return canConvert;
            }

            public bool IsValid()
            {
                bool result = false;
                if (this.ObjectiveType != null)
                {
                    // Dmean and Dmax can be without AtValue
                    if (this.ObjectiveType == "Dmean" || this.ObjectiveType == "Dmax")
                    {
                        if (this.Structure != null & this.ObjectiveExp != null &
                            this.ThanUnit != null & this.ThanValue != null &
                            this.Structure != "" & this.ObjectiveExp != "" &
                            this.ThanUnit != "" & this.ThanValue != "")
                        {
                            if (ValidateDose(this.ThanValue))
                            {
                                if (this.ThanUnit == "cGy" || this.ThanUnit == "%")
                                {
                                    result = true;
                                }
                            }
                        }
                    }
                    else if (this.ObjectiveType == "D" || this.ObjectiveType == "V" || this.ObjectiveType == "V-V")
                    {
                        if (this.Structure != null & this.ObjectiveExp != null &
                            this.AtValue != null & this.AtUnit != null &
                            this.ThanUnit != null & this.ThanValue != null &
                            this.Structure != "" & this.ObjectiveExp != "" &
                            this.AtValue != "" & this.AtUnit != "" &
                            this.ThanUnit != "" & this.ThanValue != "")
                        {
                            if (ValidateDose(this.AtValue) & ValidateDose(this.ThanValue))
                            {
                                if (this.ObjectiveType == "V" || this.ObjectiveType == "V-V")
                                {
                                    if ((this.AtUnit == "cGy" || this.AtUnit == "%") & (this.ThanUnit == "cm3" || this.ThanUnit == "%"))
                                    {
                                        result = true;
                                    }
                                }
                                else
                                {
                                    if ((this.AtUnit == "cm3" || this.AtUnit == "%") & (this.ThanUnit == "cGy" || this.ThanUnit == "%"))
                                    {
                                        result = true;
                                    }
                                }
                            }
                        }
                    }
                }

                return result;
            }
        }

        public class DataGridSpecialPTVs1
        {
            public string Structure { get; set; }
            public string totalDose = "";
            public string TotalDose
            {
                get { return totalDose; }
                set { totalDose = value; }
            }
            public string ReferenceStructure { get; set; }

            private bool ValidateDose(string dosestring)
            {
                double dose;
                if (dosestring.Contains(","))
                {
                    return false;
                }
                var canConvert = double.TryParse(dosestring, out dose);

                if (canConvert & dose < 0 & Double.IsNaN(dose))
                {
                    return false;
                }
                return canConvert;
            }

            public bool IsValid()
            {
                bool result = false;
                if (this.Structure != null && this.TotalDose != null && this.Structure != "" && this.TotalDose != "" && ValidateDose(this.TotalDose))
                {
                    result = true;
                }
                return result;
            }
        }

        public class DataGridSpecialPTVs2
        {
            public string Structure { get; set; }

            public string objectiveType = "V";
            public string ObjectiveType
            {
                get { return objectiveType; }
                set { objectiveType = value; }
            }

            public string atValue = "";
            public string AtValue
            {
                get { return atValue; }
                set { atValue = value; }
            }

            public string atUnit = "%";
            public string AtUnit
            {
                get { return atUnit; }
                set { atUnit = value; }
            }

            public string objectiveExp = "<";
            public string ObjectiveExp
            {
                get { return objectiveExp; }
                set { objectiveExp = value; }
            }

            public string thanValue = "";

            public string ThanValue
            {
                get { return thanValue; }
                set { thanValue = value; }
            }

            public string thanUnit = "%";
            public string ThanUnit
            {
                get { return thanUnit; }
                set { thanUnit = value; }
            }

            public double calculatedValue = Double.NaN;
            public double CalculatedValue
            {
                get { return calculatedValue; }
                set { calculatedValue = value; }
            }

            public string comment = "";
            public string Comment
            {
                get { return comment; }
                set { comment = value; }
            }

            public string passed = "\u2753";
            public string Passed
            {
                get { return passed; }
                set { passed = value; }
            }
            public string passedColor = "Blue";
            public string PassedColor
            {
                get { return passedColor; }
                set { passedColor = value; }
            }

            private bool ValidateDose(string dosestring)
            {
                double dose;
                if (dosestring.Contains(","))
                {
                    return false;
                }
                var canConvert = double.TryParse(dosestring, out dose);

                if (canConvert && dose < 0)
                {
                    return false;
                }
                return canConvert;
            }

            public bool IsValid()
            {
                bool result = false;
                if (this.ObjectiveType != null)
                {
                    // Dmean and Dmax can be without AtValue
                    if (this.ObjectiveType == "Dmean" || this.ObjectiveType == "Dmax")
                    {
                        if (this.Structure != null & this.ObjectiveExp != null &
                            this.ThanUnit != null & this.ThanValue != null &
                            this.Structure != "" & this.ObjectiveExp != "" &
                            this.ThanUnit != "" & this.ThanValue != "")
                        {
                            if (ValidateDose(this.ThanValue))
                            {
                                if (this.ThanUnit == "cGy" || this.ThanUnit == "%")
                                {
                                    result = true;
                                }
                            }
                        }
                    }
                    else if (this.ObjectiveType == "D" || this.ObjectiveType == "V" || this.ObjectiveType == "V(BODY-PTV)" || this.ObjectiveType == "D(BODY-PTV)")
                    {
                        if (this.Structure != null & this.ObjectiveExp != null &
                            this.AtValue != null & this.AtUnit != null &
                            this.ThanUnit != null & this.ThanValue != null &
                            this.Structure != "" & this.ObjectiveExp != "" &
                            this.AtValue != "" & this.AtUnit != "" &
                            this.ThanUnit != "" & this.ThanValue != "")
                        {
                            if (ValidateDose(this.AtValue) & ValidateDose(this.ThanValue))
                            {
                                if (this.ObjectiveType == "V")
                                {
                                    if ((this.AtUnit == "cGy" || this.AtUnit == "%") & (this.ThanUnit == "cm3" || this.ThanUnit == "%"))
                                    {
                                        result = true;
                                    }
                                }
                                else if (this.ObjectiveType == "V(BODY-PTV)")
                                {
                                    if ((this.AtUnit == "cGy" || this.AtUnit == "%") & this.ThanUnit == "%")
                                    {
                                        result = true;
                                    }
                                }
                                else
                                {
                                    if ((this.AtUnit == "cm3" || this.AtUnit == "%") & (this.ThanUnit == "cGy" || this.ThanUnit == "%"))
                                    {
                                        result = true;
                                    }
                                }
                            }
                        }
                    }
                    else if (this.ObjectiveType == "R50" || this.ObjectiveType == "R100" || this.ObjectiveType == "BODY-(PTV+2cm)")
                    {
                        if (this.Structure != null && this.Structure != "" && this.ThanUnit != null && this.ThanUnit != "")
                        {
                            if (this.ThanUnit == "table1" || this.ThanUnit == "table2")
                            {
                                result = true;
                            }

                        }
                    }
                }

                return result;
            }
        }

        private void InitializeDataGridSpecialOrgans()
        {
            this.DataGridSpecialOrganStructure.ItemsSource = CollectStructures();
            this.DataGridSpecialOrganObjectiveType.ItemsSource = new List<string> { "V", "D", "Dmax", "Dmean", "V-V" };
            this.DataGridSpecialOrganAtUnit.ItemsSource = new List<string> { "cGy", "%", "cm3" };
            this.DataGridSpecialOrganExp.ItemsSource = new List<string> { "<", ">" };
            this.DataGridSpecialOrganThanUnit.ItemsSource = new List<string> { "%", "cm3", "cGy" };

            this.DataGridSpecialOrgansList = new List<DataGridSpecialOrgans>() { };
            ListCollectionView collectionView = new ListCollectionView(this.DataGridSpecialOrgansList);
            collectionView.GroupDescriptions.Add(new PropertyGroupDescription("Structure"));
            this.DataGridSpecialOrgansCollection = collectionView;
            this.DataGridSpecialOrgan.ItemsSource = this.DataGridSpecialOrgansCollection;
        }

        private void DataGridSpecialOrgan_SourceUpdated(Object sender, DataTransferEventArgs args)
        {
            UpdateDataGridSpecialOrgan();
        }

        private void UpdateDataGridSpecialOrgan()
        {
            foreach (var row in this.DataGridSpecialOrgansList)
            {
                if (row.IsValid())
                {
                    Objective ob = new Objective(
                        this.dosimetry,
                        "fake",
                        row.Structure,
                        false,
                        "Organ",
                        row.ObjectiveType,
                        row.AtValue + row.AtUnit,
                        row.ObjectiveExp == "<" ? "less" : "more",
                        row.ThanValue + row.ThanUnit,
                        row.Comment
                        );
                    ob.CalculateObjectiveOrgan(this.dosimetry.structureset, new List<Objective>() { }, this.DoseNormalizationSpecial, row.Structure);
                    row.CalculatedValue = Math.Round(ob.value, 1);

                    string passedSymbolColor;
                    if (ob.haspassed == 0)
                    {
                        passedSymbolColor = "Red";
                    }
                    else if (ob.haspassed == 1)
                    {
                        passedSymbolColor = "Green";
                    }
                    else
                    {
                        passedSymbolColor = "Blue";
                    }
                    row.Passed = ob.haspassedmark;
                    row.PassedColor = passedSymbolColor;
                }
                else
                {
                    row.CalculatedValue = Double.NaN;
                    row.Passed = "\u2753";
                    row.PassedColor = "Blue";

                }
            }
        }

        private void ButtonSpecialNormalization_Click(object sender, RoutedEventArgs e)
        {
            this.DoseNormalizationSpecial = ConvertTextToDouble(this.TextBoxSpecialNormalization.Text) / this.PrescribedDose;
            UpdateDataGridSpecialOrgan();
            this.DataGridSpecialOrgansCollection.Refresh();
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox txt = sender as TextBox;
            int ind = txt.CaretIndex;
            txt.Text = txt.Text.Replace(",", ".");
            txt.CaretIndex = ind;
        }

        private void InitializeDataGridSpecialPTVs1()
        {
            List<string> structures = CollectPTVs();
            this.DataGridSpecialPTV1List = new List<DataGridSpecialPTVs1>() { };

            foreach (var s in structures)
            {
                DataGridSpecialPTVs1 temp = new DataGridSpecialPTVs1()
                {
                    Structure = s
                };
                this.DataGridSpecialPTV1List.Add(temp);
            }

            this.DataGridSpecialPTV1Ref.ItemsSource = structures;
            ListCollectionView collectionViewPTVs1 = new ListCollectionView(this.DataGridSpecialPTV1List);
            collectionViewPTVs1.GroupDescriptions.Add(new PropertyGroupDescription("Structure"));
            this.DataGridSpecialPTV1Collection = collectionViewPTVs1;
            this.DataGridSpecialPTV1.ItemsSource = this.DataGridSpecialPTV1Collection;
        }

        private void InitializeDataGridSpecialPTVs2()
        {
            this.DataGridSpecialPTV2Structure.ItemsSource = CollectPTVs();
            this.DataGridSpecialPTV2ObjectiveType.ItemsSource = new List<string> { "V", "D", "Dmax", "Dmean", "V(BODY-PTV)", "D(BODY-PTV)", "BODY-(PTV+2cm)", "R50", "R100" };
            this.DataGridSpecialPTV2AtUnit.ItemsSource = new List<string> { "%", "cGy", "cm3" };
            this.DataGridSpecialPTV2Exp.ItemsSource = new List<string> { "<", ">" };
            this.DataGridSpecialPTV2ThanUnit.ItemsSource = new List<string> { "%", "cm3", "cGy", "table1", "table2" };

            this.DataGridSpecialPTV2List = new List<DataGridSpecialPTVs2>() { };
            ListCollectionView collectionViewPTVs2 = new ListCollectionView(this.DataGridSpecialPTV2List);
            collectionViewPTVs2.GroupDescriptions.Add(new PropertyGroupDescription("Structure"));
            this.DataGridSpecialPTV2Collection = collectionViewPTVs2;
            this.DataGridSpecialPTV2.ItemsSource = this.DataGridSpecialPTV2Collection;
        }


        private void UpdateDataGridSpecialPTV2()
        {
            // First get the total doses and reference PTVs from the first table
            int roundDigits = 1;
            foreach (var row in this.DataGridSpecialPTV2List)
            {
                Double calculatedValue = Double.NaN;
                string passed = "\u2753";
                string passedColor = "Blue";

                if (row.IsValid())
                {
                    foreach (var p in this.DataGridSpecialPTV1List)
                    {
                        if (p.IsValid())
                        {
                            if (row.Structure == p.Structure)
                            {
                                if (row.ObjectiveType == "R50" || row.ObjectiveType == "R100" || row.ObjectiveType == "BODY-(PTV+2cm)")
                                {
                                    row.ThanValue = ""; // must add this, otherwise the thanvalue will not be update in the datagrid
                                }

                                Objective ob = new Objective(
                                    this.dosimetry,
                                    "fake",
                                    row.Structure,
                                    false,
                                    "PTV",
                                    row.ObjectiveType,
                                    row.AtValue + row.AtUnit,
                                    row.ObjectiveExp == "<" ? "less" : "more",
                                    row.ThanValue + row.ThanUnit,
                                    row.Comment
                                    );

                                if (p.ReferenceStructure != null && p.ReferenceStructure != "")
                                {
                                    ob.auxRefStructueFromSet = this.dosimetry.structureset.Structures.First(s => s.Id == p.ReferenceStructure);
                                }

                                double totaldose;
                                double.TryParse(p.TotalDose, out totaldose);

                                ob.CalculateObjectivePTV(this.dosimetry.structureset, row.Structure, totaldose);

                                if (ob.type == "R50" || ob.type == "R100" || ob.type == "BODY-(PTV+2cm)")
                                {

                                    roundDigits = 2;
                                    row.ThanValue = ob.than;
                                }
                                else
                                {
                                    roundDigits = 1;
                                }

                                calculatedValue = Math.Round(ob.value, roundDigits);

                                string passedSymbolColor;
                                if (ob.haspassed == 0)
                                {
                                    passedSymbolColor = "Red";
                                }
                                else if (ob.haspassed == 1)
                                {
                                    passedSymbolColor = "Green";
                                }
                                else
                                {
                                    passedSymbolColor = "Blue";
                                }
                                passed = ob.haspassedmark;
                                passedColor = passedSymbolColor;
                            }

                        }
                    }
                }
                row.CalculatedValue = calculatedValue;
                row.Passed = passed;
                row.PassedColor = passedColor;
            }
        }

        private void DataGridSpecialPTV1_TargetUpdated(Object sender, DataTransferEventArgs args)
        {
            UpdateDataGridSpecialPTV2();
            this.DataGridSpecialPTV2Collection.Refresh();
        }

        private void DataGridSpecialPTV2_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            UpdateDataGridSpecialPTV2();
        }



        // ########################################################################################################################
        // ########################################## SQL #########################################################################
        // ########################################################################################################################

        public class ComboBoxDataTable
        {
            public string TableName { get; set; }
            public string dateTime { get; set; }

            public string DisplayName { get; set; }
            public void SetDisplayName()
            {
                this.DisplayName = this.TableName + "(" + this.dateTime + ")";
            }
        }

        private void RefreshComboBoxDataTable()
        {
            this.ComboBoxSpecialDataTable.ItemsSource = this.ComboBoxDataTablesList;
            this.ComboBoxSpecialDataTable.DisplayMemberPath = "DisplayName";
            this.ComboBoxSpecialDataTable.SelectedValuePath = "TableName";
            this.ComboBoxSpecialDataTable.SelectedIndex = 0;
        }

        public bool InitiateSQL()
        {
            // first check if database exists
            if (!sql.TestConnection())
            {
                MessageBox.Show("Connection with the SQL database does not work. You can work without it, but you will not be able to use saved tables.", "Error");
                this.ButtonShowTable.IsEnabled = false;
                this.ButtonSaveTable.IsEnabled = false;
                this.ButtonDeleteTable.IsEnabled = false;
                this.ButtonSql.IsEnabled = false;
                return false;
            }
            else
            {
                string PatientID = this.dosimetry.structureset.Patient.Id;
                List<List<string>> tables = sql.GetTableNames(PatientID);
                List<ComboBoxDataTable> collection = new List<ComboBoxDataTable>() { };

                foreach (var table in tables)
                {
                    ComboBoxDataTable temp = new ComboBoxDataTable()
                    {
                        TableName = table[0],
                        dateTime = table[1],
                        DisplayName = table[0] + " (" + table[1] + ")"
                    };
                    collection.Add(temp);
                }
                this.ComboBoxDataTablesList = collection;
                RefreshComboBoxDataTable();
            }
            return true;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            // save tables
            string currentname;
            if (this.ComboBoxSpecialDataTable.SelectedItem == null)
            {
                currentname = "";
            }
            else
            {
                currentname = this.ComboBoxSpecialDataTable.SelectedValue.ToString();
            }

            Window SaveTableDialog = new SaveTableDialog(
                this.sql,
                currentname,
                this.dosimetry.structureset.Patient.Id,
                this.dosimetry.scriptcontext.CurrentUser.Name,
                SerializeDataGridSpecialOrgans(),
                SerializeDataGridSpecialPTVs1(),
                SerializeDataGridSpecialPTVs2(),
                this.TextBoxSpecialNormalization.Text
                );

            SaveTableDialog.ShowDialog();
            InitiateSQL();
        }

        private void ButtonShowTable_Click(object sender, RoutedEventArgs e)
        {
            if (this.ComboBoxSpecialDataTable.SelectedItem != null)
            {
                MessageBoxResult result = MessageBox.Show("Existing tables will be cleared. Do you wish to continue?", "Warning", MessageBoxButton.YesNo);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        ShowSavedTable();
                        break;
                    case MessageBoxResult.No:
                        break;
                }
            }
        }

        private void ShowSavedTable()
        {
            string currentname = this.ComboBoxSpecialDataTable.SelectedValue.ToString();
            Dictionary<string, string> data = sql.GetTableData(this.dosimetry.structureset.Patient.Id, currentname);
            if (data.Count < 1)
            {
                MessageBox.Show("Very unusual! Patient " + this.dosimetry.structureset.Patient.Id + " does not exist in the SQl database. Was it renamed in Eclipse?", "Error");
                return;
            }
            var dataGridOrgans = DeSerializeDataGrid(data["DataGridOrgans"]);
            var dataGridPTV1 = DeSerializeDataGrid(data["DataGridPTV1"]);
            var dataGridPTV2 = DeSerializeDataGrid(data["DataGridPTV2"]);

            this.TextBoxSpecialNormalization.Text = data["Normalization"];
            this.DoseNormalizationSpecial = ConvertTextToDouble(this.TextBoxSpecialNormalization.Text) / this.PrescribedDose;

            this.DataGridSpecialOrgansList.Clear();
            //this.DataGridSpecialPTV1List.Clear();
            this.DataGridSpecialPTV2List.Clear();

            List<string> structuresOrgans = CollectStructures();
            List<string> structuresPTVs = CollectPTVs();

            // organs
            foreach (var row in dataGridOrgans)
            {
                string structure = null;
                if (structuresOrgans.Contains(row[0]))
                {
                    structure = row[0];
                }
                DataGridSpecialOrgans item = new DataGridSpecialOrgans()
                {
                    Structure = structure,
                    ObjectiveType = row[1],
                    AtValue = row[2],
                    AtUnit = row[3],
                    ObjectiveExp = row[4],
                    ThanValue = row[5],
                    ThanUnit = row[6],
                    Comment = row[7]
                };
                this.DataGridSpecialOrgansList.Add(item);

            }
            UpdateDataGridSpecialOrgan();
            this.DataGridSpecialOrgansCollection.Refresh();

            // PTV1
            // Do not clear this table because the user cannot add rows manually!
            // instead go through each row and change data if needed!

            ClearDataGridPTV1();
            foreach (var row in this.DataGridSpecialPTV1List)
            {
                foreach (var row2 in dataGridPTV1)
                {
                    if (row2[0] == row.Structure)
                    {
                        row.TotalDose = row2[1];
                        row.ReferenceStructure = row2[2];
                        break;
                    }
                }
            }
            this.DataGridSpecialPTV1Collection.Refresh();

            // PTV2
            foreach (var row in dataGridPTV2)
            {
                if (structuresPTVs.Contains(row[0]))
                {
                    DataGridSpecialPTVs2 item = new DataGridSpecialPTVs2()
                    {
                        Structure = row[0],
                        ObjectiveType = row[1],
                        AtValue = row[2],
                        AtUnit = row[3],
                        ObjectiveExp = row[4],
                        ThanValue = row[5],
                        ThanUnit = row[6],
                        Comment = row[7]
                    };
                    this.DataGridSpecialPTV2List.Add(item);
                }
            }
            UpdateDataGridSpecialPTV2();
            this.DataGridSpecialPTV2Collection.Refresh();
        }

        private void ClearDataGridPTV1()
        {
            // this function deletes entries into DataGridPTV1
            foreach (var row in this.DataGridSpecialPTV1List)
            {
                row.totalDose = null;
                row.ReferenceStructure = null;
            }
            this.DataGridSpecialPTV1Collection.Refresh();
        }

        private void ButtonDeleteTable_Click(object sender, RoutedEventArgs e)
        {
            string currentname;
            if (this.ComboBoxSpecialDataTable.SelectedItem != null)
            {
                currentname = this.ComboBoxSpecialDataTable.SelectedValue.ToString();
                MessageBoxResult result = MessageBox.Show("Do you really wish to delete the table " + currentname + " from the database?", "Warning", MessageBoxButton.YesNo);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        this.sql.DeleteTable(this.dosimetry.structureset.Patient.Id, currentname);
                        break;
                    case MessageBoxResult.No:
                        break;
                }
                InitiateSQL();
            }
        }

        private string SerializeDataGridSpecialOrgans()
        {
            List<List<string>> data = new List<List<string>>() { };
            string xml;
            foreach (var row in this.DataGridSpecialOrgansList)
            {
                if (row.IsValid())
                {
                    List<string> temp = new List<string>() { };
                    temp.Add(row.Structure);
                    temp.Add(row.ObjectiveType);
                    temp.Add(row.AtValue);
                    temp.Add(row.AtUnit);
                    temp.Add(row.ObjectiveExp);
                    temp.Add(row.ThanValue);
                    temp.Add(row.ThanUnit);
                    temp.Add(row.Comment);
                    data.Add(temp);
                }
            }
            using (StringWriter sw = new StringWriter())
            {
                XmlSerializer xs = new XmlSerializer(typeof(List<List<string>>));
                xs.Serialize(sw, data);
                xml = sw.ToString();
            }
            return xml;
        }

        private string SerializeDataGridSpecialPTVs1()
        {
            List<List<string>> data = new List<List<string>>() { };
            string xml;
            foreach (var row in this.DataGridSpecialPTV1List)
            {
                if (row.IsValid())
                {
                    List<string> temp = new List<string>() { };
                    temp.Add(row.Structure);
                    temp.Add(row.TotalDose);
                    temp.Add(row.ReferenceStructure);

                    data.Add(temp);
                }
            }
            using (StringWriter sw = new StringWriter())
            {
                XmlSerializer xs = new XmlSerializer(typeof(List<List<string>>));
                xs.Serialize(sw, data);
                xml = sw.ToString();
            }
            return xml;
        }

        private string SerializeDataGridSpecialPTVs2()
        {
            List<List<string>> data = new List<List<string>>() { };
            string xml;
            foreach (var row in this.DataGridSpecialPTV2List)
            {
                if (row.IsValid())
                {
                    List<string> temp = new List<string>() { };
                    temp.Add(row.Structure);
                    temp.Add(row.ObjectiveType);
                    temp.Add(row.AtValue);
                    temp.Add(row.AtUnit);
                    temp.Add(row.ObjectiveExp);
                    temp.Add(row.ThanValue);
                    temp.Add(row.ThanUnit);
                    temp.Add(row.Comment);
                    data.Add(temp);
                }
            }
            using (StringWriter sw = new StringWriter())
            {
                XmlSerializer xs = new XmlSerializer(typeof(List<List<string>>));
                xs.Serialize(sw, data);
                xml = sw.ToString();
            }
            return xml;
        }

        private List<List<string>> DeSerializeDataGrid(string xml)
        {
            List<List<string>> data = new List<List<string>>() { };

            XmlSerializer xs = new XmlSerializer(typeof(List<List<string>>));
            using (TextReader reader = new StringReader(xml))
            {
                data = (List<List<string>>)xs.Deserialize(reader);
            }
            return data;
        }

        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Do you really wish to clear the tables?", "Warning", MessageBoxButton.YesNo);
            switch (result)
            {
                case MessageBoxResult.Yes:
                    this.DataGridSpecialOrgansList.Clear();
                    this.DataGridSpecialOrgansCollection.Refresh();
                    this.TextBoxSpecialNormalization.Text = "NaN";
                    ClearDataGridPTV1();
                    this.DataGridSpecialPTV2List.Clear();
                    this.DataGridSpecialPTV2Collection.Refresh();
                    break;
                case MessageBoxResult.No:
                    break;
            }
        }

        private void ButtonSql_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AdminSQL adminsql = new AdminSQL(this.sql);
                adminsql.ShowDialog();
            }
            catch (Exception g)
            {
                MessageBox.Show("Something went wrong. \n" + g.ToString(), "Error");
                return;
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            // import from protocol
            try
            {
                ImportFromProtocol importfromprotocol = new ImportFromProtocol(this.dosimetry, this);
                importfromprotocol.ShowDialog();

                if (importfromprotocol.DataGridOrgansListDialogResult.Count < 1)
                {
                    return;
                }

                this.DataGridSpecialOrgansList.Clear();
                foreach (var row in importfromprotocol.DataGridOrgansList)
                {
                    if (row.Include == true && row.Structure != null && row.Structure != "")
                    {
                        DataGridSpecialOrgans item = new DataGridSpecialOrgans()
                        {
                            Structure = row.Structure,
                            ObjectiveType = row.ObjectiveType,
                            AtValue = row.AtValue,
                            AtUnit = row.AtUnit,
                            ObjectiveExp = row.ObjectiveExp,
                            ThanValue = row.ThanValue,
                            ThanUnit = row.ThanUnit,
                            Comment = row.Comment
                        };
                        this.DataGridSpecialOrgansList.Add(item);
                    }
                }
                UpdateDataGridSpecialOrgan();
                this.DataGridSpecialOrgansCollection.Refresh();
            }
            catch (Exception g)
            {
                MessageBox.Show("Something went wrong. \n" + g.ToString(), "Error");
                return;
            }
        }


        private void Button_Click_4(object sender, RoutedEventArgs e)
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


        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            // Add optimization objectives
            if (this.dosimetry.scriptcontext.ExternalPlanSetup == null ||
                this.dosimetry.scriptcontext.StructureSet == null)
            {
                MessageBox.Show("A plan must be active in the context.", "Error");
                return;
            }

            MessageBoxResult result = MessageBox.Show("Existing objectives will be deleted.\n"
                + "Do you really wish to proceed?", "Warning", MessageBoxButton.YesNo);
            switch (result)
            {
                case MessageBoxResult.Yes:
                    try
                    {
                        var addOptimizationObjectives = new AddOptimizationObjectives(this, this.dosimetry.scriptcontext.ExternalPlanSetup);
                        addOptimizationObjectives.DeleteObjectives();
                        addOptimizationObjectives.AddOrganObjectives();
                        addOptimizationObjectives.AddNTOObjective();
                        addOptimizationObjectives.AddPTVObjectives();
                        MessageBox.Show("Optimization objectives inserted into the plan.", "Warning");
                    }
                    catch (Exception g)
                    {
                        MessageBox.Show("Something went wrong. \n" + g.ToString(), "Error");
                        return;
                    }
                    break;
                case MessageBoxResult.No:
                    break;
            }
        }


        private void ButtonCreatePlan_Click(object sender, RoutedEventArgs e)
        {
            // Add plan
            if (this.dosimetry.scriptcontext.StructureSet == null)
            {
                MessageBox.Show("StructureSet is not active in the context.", "Error");
                return;
            }

            try
            {
                CreatePlan createPlan = new CreatePlan(this);
                createPlan.ShowDialog();
            }
            catch (Exception g)
            {
                MessageBox.Show("Something went wrong.. \n" + g.ToString(), "Error");
                return;
            }

        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            try
            {
                string tempFileName = "Dosimetry_HelpFile_Version_" + this.DosimetryVersion + ".chm";
                string tempFolder = System.IO.Path.GetTempPath();
                File.Copy(this.dosimetry.WorkingFolder + "\\Help\\Dosimetry_Help.chm", System.IO.Path.Combine(tempFolder, tempFileName), true);
                System.Diagnostics.Process.Start(System.IO.Path.Combine(tempFolder, tempFileName));
            }
            catch
            {
                MessageBox.Show("Cannot open help. Check that it is not already open.");
            }
        }

        private void ButtonSpecialFillTargetObjectives_Click(object sender, RoutedEventArgs e)
        {
            // Fill target goals with default objectives!
            this.DataGridSpecialPTV2List.Clear();
            this.DataGridSpecialPTV2Collection.Refresh();

            foreach (var row in this.DataGridSpecialPTV1List)
            {
                if (row.IsValid())
                {
                    DataGridSpecialPTVs2 Lower = new DataGridSpecialPTVs2()
                    {
                        Structure = row.Structure,
                        ObjectiveType = "V",
                        AtValue = "95",
                        AtUnit = "%",
                        ObjectiveExp = ">",
                        ThanValue = "98",
                        ThanUnit = "%",
                        Comment = ""
                    };
                    DataGridSpecialPTVs2 Upper = new DataGridSpecialPTVs2()
                    {
                        Structure = row.Structure,
                        ObjectiveType = "V",
                        AtValue = "107",
                        AtUnit = "%",
                        ObjectiveExp = "<",
                        ThanValue = "2",
                        ThanUnit = "%",
                        Comment = ""
                    };
                    this.DataGridSpecialPTV2List.Add(Lower);
                    this.DataGridSpecialPTV2List.Add(Upper);
                }
            }
            UpdateDataGridSpecialPTV2();
            this.DataGridSpecialPTV2Collection.Refresh();
        }

        private void MenuItemProtocols_Click(object sender, RoutedEventArgs e)
        {
            EditXML EditXML = new EditXML(this.dosimetry.protocolPath, "Protocols");
            EditXML.ShowDialog();
        }

        private void MenuItemProtocolGroups_Click(object sender, RoutedEventArgs e)
        {
            EditXML EditXML = new EditXML(this.dosimetry.structureMappingPath, "ProtocolGroups");
            EditXML.ShowDialog();
        }

        private void MenuItemStructureMapping_Click(object sender, RoutedEventArgs e)
        {
            EditXML EditXML = new EditXML(this.dosimetry.structureMappingPath, "StructureMapping");
            EditXML.ShowDialog();
        }

        private void MenuItemMachineSettings_Click(object sender, RoutedEventArgs e)
        {
            EditXML EditXML = new EditXML(this.dosimetry.machineSettingsPath, "MachineSettings");
            EditXML.ShowDialog();
        }
    }
}

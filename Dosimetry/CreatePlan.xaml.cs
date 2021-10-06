// Creates an external plan setup for starters
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
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace Dosimetry
{
    /// <summary>
    /// Interaction logic for CreatePlan.xaml
    /// </summary>
    public partial class CreatePlan : Window
    {
        public MainWindow dosimetryPanel;
        public ScriptContext context;

        public StructureSet NewStructureSet;
        public Course NewCourse; // or old
        public ExternalPlanSetup NewPlan;

        public List<string> ExistingStructureSetIds = new List<string>() { };
        public List<string> ExistingPlanIds = new List<string>() { };

        public List<string> ErrorMessages = new List<string>() { "Finished" };

        public List<DataGridTargets> DataGridTargetsList = new List<DataGridTargets>() { };
        public ListCollectionView DataGridTargetsCollection { get; set; }

        public List<DosePrescription> DataGridDosePrescription = new List<DosePrescription>() { };
        public ListCollectionView DataGridDosePrescriptionCollection { get; set; }

        public List<List<KeyValuePair<string, double>>> GroupedTargets;

        public List<Structure> finalTargets = new List<Structure>() { };
        public List<Structure> finalTransitionalPTVs = new List<Structure>() { };
        public List<double> finalDoses = new List<double>() { };

        public MachineSettings machineSettings;

        public CreatePlan(MainWindow dosimetryPanel)
        {
            this.dosimetryPanel = dosimetryPanel;
            this.context = dosimetryPanel.dosimetry.scriptcontext;

            this.machineSettings = new MachineSettings(this.dosimetryPanel.dosimetry.machineSettingsPath);
            this.machineSettings.Read();

            this.NewStructureSet = this.context.StructureSet; // temporarilly

            GetAllStructureSetIds();
            GetAllImageIds();

            InitializeComponent();
            this.LabelStructureSetArrow.Content = "\u2190";

            this.TextBoxStructureSetCurrent.Text = this.context.StructureSet.Id;

            this.TextBoxStructureSetNew.IsEnabled = false;
            this.CheckBoxRemoveEmptyStructures.IsEnabled = false;
            this.CheckBoxAddVirtualBolus.IsEnabled = false;
            this.DataGrid1.IsEnabled = false;
            this.ListViewVirtBolus.IsEnabled = false;

            AddListViewVirtBolus();
            AddTargetSubtractionsGrid();

            if (CheckHighResolution())
            {
                this.CheckBoxConvertHighRes.IsEnabled = true;
                this.LabelConvertHighRes.Content = "(At least one target is of high resolution.)";
                this.LabelConvertHighRes.Foreground = Brushes.Red;

            }

            this.ComboBoxCourse.IsEnabled = false;
            this.TextBoxPlanName.IsEnabled = false;
            this.DataGridPrescription.IsEnabled = false;
            this.ComboBoxMachine.IsEnabled = false;
            this.ComboBoxEnergy.IsEnabled = false;
            this.ComboBoxTechnique.IsEnabled = false;
            this.ComboBoxAlgorithmDose.IsEnabled = false;
            this.ComboBoxAlgorithmOpt.IsEnabled = false;
            this.CheckBoxAddOptimizationObjectives.IsEnabled = false;

            InitiateDosePrescriptionDataGrid();
            InitiateComboBoxCourse();
            InitiateComboBoxMachine();
        }

        public class DataGridTargets
        {
            public double Dose1 { get; set; }
            public double Dose2 { get; set; }
            public string Target1 { get; set; }
            public string Target2 { get; set; }
            public int Margin { get; set; }
            public bool IsValid(StructureSet structureset)
            {
                if (Target1 != null && Target1 != "" &&
                    Target2 != null && Target2 != "" &&
                    Target1 != Target2 &&
                    Margin >= 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public class DosePrescription
        {
            public int NumFractions { get; set; }
            public double DosePerFraction { get; set; }
            public double TotalDose { get; set; }
        }

        public void InitiateDosePrescriptionDataGrid()
        {
            List<DosePrescription> data = new List<DosePrescription>() { };
            DosePrescription row = new DosePrescription()
            {
                NumFractions = 0,
                DosePerFraction = 0,
                TotalDose = 0
            };
            data.Add(row);

            this.DataGridDosePrescription = data;
            ListCollectionView collectionView = new ListCollectionView(this.DataGridDosePrescription);
            collectionView.GroupDescriptions.Add(new PropertyGroupDescription("DosePrescription"));
            this.DataGridDosePrescriptionCollection = collectionView;
            this.DataGridPrescription.ItemsSource = this.DataGridDosePrescriptionCollection;
        }

        public List<string> GetTargets()
        {
            List<string> targets = new List<string>() { };

            foreach (var row in this.dosimetryPanel.DataGridSpecialPTV1List)
            {
                if (row.Structure != null & row.Structure != "" &
                    row.TotalDose != null & row.TotalDose != "")
                {
                    targets.Add(row.Structure);
                }
            }
            return targets;
        }

        public bool CheckHighResolution()
        {
            // return True if at least one target is high resolution
            foreach (var row in this.dosimetryPanel.DataGridSpecialPTV1List)
            {
                if (row.Structure != null & row.Structure != "" &
                    row.TotalDose != null & row.TotalDose != "")
                {
                    Structure structure = this.NewStructureSet.Structures.First(id => id.Id == row.Structure);
                    if (structure.IsHighResolution)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void AddTargetSubtractionsGrid()
        {
            // EXACTLY THE SAME ORDER OF OPERATION MUST BE MAINTAINED AT THE TIME OF BOOLEAN OPERATION!
            List<DataGridTargets> data = new List<DataGridTargets>() { };
            Dictionary<string, double> targets = new Dictionary<string, double>() { };

            foreach (var row in this.dosimetryPanel.DataGridSpecialPTV1List)
            {
                if (row.Structure != null & row.Structure != "" &
                    row.TotalDose != null & row.TotalDose != "")
                {
                    targets.Add(row.Structure, ConvertTextToDouble(row.TotalDose));
                }
            }

            if (targets.Count < 1)
            {
                return;
            }

            var groupedTargets = targets
                .Where(u => u.Key.IndexOf("ptv", StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(u => u.Value)
                .GroupBy(u => u.Value)
                .Select(grp => grp.ToList())
                .ToList();

            this.GroupedTargets = groupedTargets;

            // If two or more ptvs have the same dose, sum them up:
            List<string> summedPTVs = new List<string>() { };
            List<double> doses = new List<double>() { };
            foreach (var group in groupedTargets)
            {
                List<string> tempSum = new List<String>() { };
                foreach (var s in group)
                {
                    tempSum.Add(s.Key);
                }
                summedPTVs.Add(String.Join("+", tempSum));
                doses.Add(group[0].Value);
            }

            // Create summed up ptvs so that each PTV is within a larger PTV except the last one.
            List<string> summedPTVs2 = new List<string>() { };
            summedPTVs2.Add(summedPTVs.First());
            for (int j = 1; j < summedPTVs.Count; ++j)
            {
                string temp = summedPTVs2.Last() + "+" + summedPTVs[j];
                summedPTVs2.Add(temp);
            }

            // Now create subtractions
            List<string> subtractedPTVs = new List<string>() { };
            subtractedPTVs.Add(summedPTVs2.First());
            for (int k = 1; k < summedPTVs2.Count; k++)
            {
                subtractedPTVs.Add("(" + summedPTVs2[k] + ") - (" + summedPTVs2[k - 1] + ")");
            }

            if (summedPTVs2.Count > 1)
            {
                for (int i = 0; i < summedPTVs2.Count - 1; i++)
                {
                    string target1 = summedPTVs2[i];
                    string target2 = summedPTVs2[i + 1];
                    double dose1 = doses[i];
                    double dose2 = doses[i + 1];

                    int margin = CalculateMargin(dose1, dose2);

                    DataGridTargets item = new DataGridTargets()
                    {
                        Dose1 = dose1,
                        Dose2 = dose2,
                        Target1 = target1,
                        Target2 = target2,
                        Margin = margin
                    };
                    data.Add(item);
                }
            }

            this.DataGridTargetsList = data;
            ListCollectionView collectionView = new ListCollectionView(this.DataGridTargetsList);
            collectionView.GroupDescriptions.Add(new PropertyGroupDescription("Targets"));
            this.DataGridTargetsCollection = collectionView;
            this.DataGrid1.ItemsSource = this.DataGridTargetsCollection;
        }



        public void CreatePTVTransitions()
        {
            List<Structure> temporaryStructures = new List<Structure>() { };
            List<Structure> finalStructures = new List<Structure>() { };

            if (this.DataGridTargetsList.Count < 1)
            {
                ErrorMessages.Add("Target DataGrid is empty. No transitional volumes can be created.");
                return;
            }

            // Get margins from table
            List<double> margins = new List<double>() { };
            foreach (var item in this.DataGridTargetsList)
            {
                margins.Add(item.Margin);
            }

            //List<List<KeyValuePair<string, double>>> groupedTargets = this.GroupedTargets;

            // If two or more ptvs have the same dose, sum them up:
            List<Structure> summedPTVs = new List<Structure>() { };
            List<double> doses = new List<double>() { };

            for (int i = 0; i < this.GroupedTargets.Count; i++)
            {
                var group = this.GroupedTargets[i];

                if (group.Count > 1)
                {
                    Structure sumPTV = AddStructure("temp_sumPTV_" + i.ToString(), "PTV", this.NewStructureSet);

                    if (sumPTV == (Structure)null)
                    {
                        ErrorMessages.Add("Cannot create temporary structure " + "temp_sumPTV_" + i.ToString());
                        ErrorMessages.Add("Stopping the creation of transitional target volumes");
                        RemoveStructuresInList(temporaryStructures);
                        RemoveStructuresInList(finalStructures);
                        return;
                    }
                    else
                    {
                        temporaryStructures.Add(sumPTV);
                    }

                    foreach (var s in group)
                    {
                        Structure currentPTV = this.NewStructureSet.Structures.First(u => u.Id == s.Key);
                        SegmentVolume temp = BooleanOperator(sumPTV, currentPTV, "Or");
                        if (temp == (SegmentVolume)null)
                        {
                            ErrorMessages.Add("Cannot perform boolean operation on " + sumPTV.Id + " and " + currentPTV.Id);
                            ErrorMessages.Add("Stopping the creation of transitional target volumes");
                            RemoveStructuresInList(temporaryStructures);
                            RemoveStructuresInList(finalStructures);
                            return;
                        }
                        else
                        {
                            sumPTV.SegmentVolume = temp;
                        }
                    }
                    summedPTVs.Add(sumPTV);
                }
                else
                {
                    Structure currentPTV = this.NewStructureSet.Structures.First(u => u.Id == group[0].Key);
                    summedPTVs.Add(currentPTV);
                }
                doses.Add(group[0].Value);
            }

            this.finalDoses = doses;

            // Now calculate sums of structures inside summedPTVs = (PTV1, PTV2, PTV3, ..., PTVN) where doses = (D1, D2, D3 ... DN) and D1>D2>...>DN.
            // PTV1' = PTV1
            // PTV2' = PTV1 + PTV2
            // PTV3' = PTV1 + PTV2 + PTV3
            // ...

            List<Structure> summedPTVs2 = new List<Structure>() { };

            for (int i = 0; i < summedPTVs.Count; i++)
            {
                Structure temp = AddStructure("OptTarget_" + (i + 1).ToString(), "PTV", this.NewStructureSet);

                if (temp == (Structure)null)
                {
                    ErrorMessages.Add("Cannot create target volume " + "OptTarget_" + (i + 1).ToString());
                    ErrorMessages.Add("Stopping the creation of transitional target volumes");
                    RemoveStructuresInList(temporaryStructures);
                    RemoveStructuresInList(finalStructures);
                    return;
                }
                else
                {
                    finalStructures.Add(temp);
                }

                for (int j = 0; j <= i; j++)
                {
                    SegmentVolume tempSV = BooleanOperator(temp, summedPTVs[j], "Or");
                    if (tempSV == (SegmentVolume)null)
                    {
                        ErrorMessages.Add("Cannot perform boolean operation 'Or' on " + temp.Id + " and " + summedPTVs[j].Id);
                        ErrorMessages.Add("Stopping the creation of transitional target volumes");
                        RemoveStructuresInList(temporaryStructures);
                        RemoveStructuresInList(finalStructures);
                        return;
                    }
                    else
                    {
                        temp.SegmentVolume = tempSV;
                    }
                }
                summedPTVs2.Add(temp);
            }

            this.finalTargets = summedPTVs2;

            // Now Subtract summed up ptvs including margins
            // PTV2' - PTV1'
            // PTV3' - PTV2' - PTV1'
            // PTV4' - PTV3' - PTV2' - PTV1'

            List<Structure> subtractedPTVs = new List<Structure>() { };
            for (int k = 1; k < summedPTVs2.Count; k++)
            {
                Structure temp = AddStructure("OptTrans_" + k.ToString(), "PTV", this.NewStructureSet);

                if (temp == (Structure)null)
                {
                    ErrorMessages.Add("Cannot create transitional volume " + "OptTrans_" + k.ToString());
                    ErrorMessages.Add("Stopping the creation of transitional target volumes");
                    RemoveStructuresInList(temporaryStructures);
                    RemoveStructuresInList(finalStructures);
                    return;
                }
                else
                {
                    finalStructures.Add(temp);
                }

                temp.SegmentVolume = summedPTVs2[k].SegmentVolume;

                for (int w = 0; w < k; w++)
                {
                    Structure tempptvsummargin = AddStructure("Opt_Temp_PTV_" + w.ToString() + k.ToString(), "PTV", this.NewStructureSet);

                    if (tempptvsummargin == (Structure)null)
                    {
                        ErrorMessages.Add("Cannot create temporary volume " + "Opt_Temp_PTV_" + w.ToString() + k.ToString());
                        ErrorMessages.Add("Stopping the creation of transitional target volumes");
                        RemoveStructuresInList(temporaryStructures);
                        RemoveStructuresInList(finalStructures);
                        return;
                    }
                    else
                    {
                        temporaryStructures.Add(tempptvsummargin);
                    }

                    SegmentVolume ptvplusmargin = summedPTVs2[w].SegmentVolume.Margin(margins[w]); // Margin already converts to HighRes
                    tempptvsummargin.SegmentVolume = ptvplusmargin;

                    SegmentVolume subtracted = BooleanOperator(temp, tempptvsummargin, "Sub");
                    if (subtracted == (SegmentVolume)null)
                    {
                        ErrorMessages.Add("Cannot perform boolean operation 'Sub' on " + temp.Id + " and " + tempptvsummargin.Id);
                        ErrorMessages.Add("Stopping the creation of transitional target volumes");
                        RemoveStructuresInList(temporaryStructures);
                        RemoveStructuresInList(finalStructures);
                        return;
                    }
                    else
                    {
                        temp.SegmentVolume = subtracted;
                    }
                }
                subtractedPTVs.Add(temp);
            }
            this.finalTransitionalPTVs = subtractedPTVs;

            RemoveStructuresInList(temporaryStructures);
        }


        private void DataGrid1_SourceUpdated(object sender, DataTransferEventArgs e)
        {

        }

        public int CalculateMargin(double dose1, double dose2)
        {
            // default 3mm per 10% dose difference
            // dose1  lower dose
            // dose 2 higher dose
            double maxDose = Math.Max(dose1, dose2);

            return (int)Math.Round(30.0 * Math.Abs(dose1 - dose2) / maxDose);
        }

        private double ConvertTextToDouble(string text)
        {
            if (Double.TryParse(text, out double result))
            {
                return result;
            }
            else
            {
                return 0;
            }
        }


        public void GetAllStructureSetIds()
        {
            foreach (var s in this.context.Patient.StructureSets)
            {
                this.ExistingStructureSetIds.Add(s.Id);
            }
        }

        public void GetAllImageIds()
        {
            foreach (var s in this.context.Patient.StructureSets)
            {
                this.ExistingStructureSetIds.Add(s.Image.Id);
            }
        }


        private void StructureSetLabelNewTextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.ExistingStructureSetIds.Contains(this.TextBoxStructureSetNew.Text))
            {
                this.LabelStructureSetNewSuccess.Content = "\u274C";
                this.LabelStructureSetNewSuccess.Foreground = Brushes.Red;
            }
            else
            {
                this.LabelStructureSetNewSuccess.Content = "\u2714";
                this.LabelStructureSetNewSuccess.Foreground = Brushes.Green;
            }
        }

        public void DuplicateStructureSet()
        {
            this.NewStructureSet = this.context.StructureSet.Copy();

            if (this.NewStructureSet.Id != this.TextBoxStructureSetNew.Text)
            {
                try
                {
                    this.NewStructureSet.Id = this.TextBoxStructureSetNew.Text;
                }
                catch
                {
                    ErrorMessages.Add("Could not change StructureSet name to '" + this.TextBoxStructureSetNew.Text + "'. Using " + this.NewStructureSet.Id + " instead.");
                }
            }
            if (this.NewStructureSet.Image.Id != this.TextBoxStructureSetNew.Text)
            {
                try
                {
                    this.NewStructureSet.Image.Id = this.TextBoxStructureSetNew.Text;
                }
                catch
                {
                    ErrorMessages.Add("Could not change Image name to '" + this.TextBoxStructureSetNew.Text + "'. Using " + this.NewStructureSet.Image.Id + " instead.");
                }
            }
        }

        public void RemoveEmptyStructures()
        {
            foreach (var structure in this.NewStructureSet.Structures.ToList()) // must have ToList() otherwise it doesnt work!
            {
                if (structure.IsEmpty)
                {
                    RemoveStructure(structure, this.NewStructureSet);
                }
            }
        }

        public static int GetSlice(double z, StructureSet SS)
        {
            // https://jhmcastelo.medium.com/tips-for-vvectors-and-structures-in-esapi-575bc623074a
            //
            var imageRes = SS.Image.ZRes;
            double zDirImg = SS.Image.ZDirection.z;
            return Convert.ToInt32((z - SS.Image.Origin.z) * zDirImg / imageRes);
        }

        public static double GetZ(double slice, StructureSet SS)
        {
            var imageRes = SS.Image.ZRes;
            double zDirImg = SS.Image.ZDirection.z;
            return SS.Image.Origin.z + imageRes * slice / zDirImg;
        }


        public void CreateNewBodyAndAddBolus()
        {
            // not really sure if cropping new body to the extent of the old is really neccessary
            Structure Square = AddStructure("temp_Square", "CONTROL", this.NewStructureSet);

            if (Square != (Structure)null)
            {
                var parameters = this.NewStructureSet.GetDefaultSearchBodyParameters();
                parameters.LowerHUThreshold = -300;
                parameters.KeepLargestParts = false;
                //parameters.NumberOfLargestPartsToKeep = 1;
                parameters.PreDisconnect = false;
                parameters.FillAllCavities = true;
                parameters.PreCloseOpenings = false;
                parameters.Smoothing = true;
                parameters.SmoothingLevel = 1;

                Structure body = this.NewStructureSet.Structures.Where(e => e.DicomType == "EXTERNAL").OrderBy(e => e.Volume).Last();

                var bounds = body.MeshGeometry.Bounds;
                int meshBoundLow = GetSlice(bounds.Z, this.NewStructureSet);
                int meshBoundHigh = GetSlice(bounds.Z + bounds.SizeZ, this.NewStructureSet);

                if (meshBoundLow > meshBoundHigh)
                {
                    int t = meshBoundLow;
                    meshBoundLow = meshBoundHigh;
                    meshBoundHigh = t;
                }

                // copy body to different structure
                Structure bodyTemp = AddStructure("body_temp", "CONTROL", this.NewStructureSet);

                if (bodyTemp != (Structure)null)
                {
                    bodyTemp.SegmentVolume = body.SegmentVolume;

                    RemoveStructure(body, this.NewStructureSet);
                    Structure newBody = this.NewStructureSet.CreateAndSearchBody(parameters);

                    for (int i = meshBoundLow; i < meshBoundHigh; i++)
                    {
                        List<VVector> contour = new List<VVector>() { };

                        double z = GetZ(i, this.NewStructureSet);
                        VVector point1 = new VVector(bounds.X, bounds.Y, z);
                        VVector point2 = new VVector(bounds.X + bounds.SizeX, bounds.Y, z);
                        VVector point3 = new VVector(bounds.X + bounds.SizeX, bounds.Y + bounds.SizeY, z);
                        VVector point4 = new VVector(bounds.X, bounds.Y + bounds.SizeY, z);

                        contour.Add(point1);
                        contour.Add(point2);
                        contour.Add(point3);
                        contour.Add(point4);

                        try
                        {
                            Square.AddContourOnImagePlane(contour.ToArray(), i);
                        }
                        catch
                        {
                            ErrorMessages.Add("Cannot add contour point to Square. BODY will not be proper.");
                        }

                    }
                    SegmentVolume segmv1 = BooleanOperator(newBody, Square, "And");

                    if (segmv1 == (SegmentVolume)null)
                    {
                        ErrorMessages.Add("Cannot perform And operation on newBody and Square. Check body.");
                    }
                    else
                    {
                        newBody.SegmentVolume = segmv1;
                    }

                    RemoveStructure(Square, this.NewStructureSet);

                    Structure PTV = CreatePTVSum();

                    if (PTV != (Structure)null)
                    {
                        Structure virtbolus = AddStructure("virt bolus", "CONTROL", this.NewStructureSet);
                        if (virtbolus != (Structure)null)
                        {
                            virtbolus.SegmentVolume = PTV.SegmentVolume.Margin(5.0); // Margin already converts to HighRes

                            RemoveStructure(PTV, this.NewStructureSet);

                            SegmentVolume segmv2 = BooleanOperator(virtbolus, newBody, "Sub");

                            if (segmv2 == (SegmentVolume)null)
                            {
                                ErrorMessages.Add("Cannot subtract virt bolus and body");
                                RemoveStructure(virtbolus, this.NewStructureSet);
                                RemoveStructure(bodyTemp, this.NewStructureSet);
                                return;
                            }

                            virtbolus.SegmentVolume = segmv2;

                            SegmentVolume segmv3 = BooleanOperator(bodyTemp, virtbolus, "Or");
                            if (segmv3 == (SegmentVolume)null)
                            {
                                ErrorMessages.Add("Cannot add body and virt. bolus");
                                RemoveStructure(virtbolus, this.NewStructureSet);
                                RemoveStructure(bodyTemp, this.NewStructureSet);
                                return;
                            }
                            newBody.SegmentVolume = segmv3;
                            RemoveStructure(bodyTemp, this.NewStructureSet);

                            string errorMessage;
                            if (virtbolus.CanSetAssignedHU(out errorMessage))
                            {
                                virtbolus.SetAssignedHU(0);
                            }
                            else
                            {
                                ErrorMessages.Add("Cannot set HU = 0 to " + virtbolus.Id);
                            }
                        }
                        else
                        {
                            ErrorMessages.Add("Cannot create structure 'virt bolus'");
                            RemoveStructure(PTV, this.NewStructureSet);
                            RemoveStructure(bodyTemp, this.NewStructureSet);
                        }
                    }
                    else
                    {
                        ErrorMessages.Add("Missing PTV sum for virt. bolus");
                        RemoveStructure(bodyTemp, this.NewStructureSet);
                    }

                }
            }
        }

        private Structure CreatePTVSum()
        {
            // create sum of all ptvs
            List<string> targets = GetListViewVirtBolus();
            Structure PTVSum = (Structure)null;

            if (targets.Count > 0)
            {
                PTVSum = AddStructure("temp_ptv_sum", "PTV", this.NewStructureSet);
                if (PTVSum != (Structure)null)
                {
                    foreach (var sid in targets)
                    {
                        Structure ptv = this.NewStructureSet.Structures.First(u => u.Id == sid);

                        SegmentVolume segmv1 = BooleanOperator(PTVSum, ptv, "Or");

                        if (segmv1 == (SegmentVolume)null)
                        {
                            ErrorMessages.Add("Cannot create PTV sum for virt. bolus");
                            RemoveStructure(PTVSum, this.NewStructureSet);
                            return (Structure)null;
                        }
                        else
                        {
                            PTVSum.SegmentVolume = segmv1;
                        }
                    }
                }
                else
                {
                    ErrorMessages.Add("Cannot create PTV sum for virt. bolus");
                }
            }
            else
            {
                ErrorMessages.Add("No target selected for virt. bolus");
            }
            return PTVSum;
        }




        public bool ConvertToHighResSegment(Structure structure)
        {
            bool result = true;
            if ((bool)this.CheckBoxConvertHighRes.IsChecked)
            {
                if (!structure.IsHighResolution)
                {
                    if (structure.CanConvertToHighResolution())
                    {
                        structure.ConvertToHighResolution();
                        ErrorMessages.Add("Structure " + structure.Id + " converted to High Resolution");
                    }
                    else
                    {
                        ErrorMessages.Add("Cannot convert to HiRes: " + structure.Id);
                        result = false;
                    }
                }
            }
            else
            {
                ErrorMessages.Add("Conversion to HiRes not enabled. Stopping operation.");
                result = false;
            }
            return result;
        }


        public SegmentVolume BooleanOperator(Structure structure1, Structure structure2, string boolean)
        {
            SegmentVolume result = (SegmentVolume)null;
            if (structure1.IsHighResolution || structure2.IsHighResolution)
            {
                if (!ConvertToHighResSegment(structure1))
                {
                    ErrorMessages.Add("Cannot perform boolean on: " + structure1.Id + ", " + structure2.Id);
                    return result;
                }
                if (!ConvertToHighResSegment(structure2))
                {
                    ErrorMessages.Add("Cannot perform boolean on: " + structure1.Id + ", " + structure2.Id);
                    return result;
                }
            }

            if (boolean == "Sub")
            {
                result = structure1.SegmentVolume.Sub(structure2.SegmentVolume);
            }
            else if (boolean == "Or")
            {
                result = structure1.SegmentVolume.Or(structure2.SegmentVolume);
            }
            else if (boolean == "And")
            {
                result = structure1.SegmentVolume.And(structure2.SegmentVolume);
            }

            return result;
        }



        public void RemoveStructure(Structure structure, StructureSet structureset)
        {
            if (structureset.CanRemoveStructure(structure))
            {
                structureset.RemoveStructure(structure);
            }
            else
            {
                ErrorMessages.Add("Cannot delete structure " + structure.Id);
            }
        }

        public void RemoveStructuresInList(List<Structure> structures)
        {
            foreach (var s in structures)
            {
                RemoveStructure(s, this.NewStructureSet);
            }
        }

        public Structure AddStructure(string structureId, string dicomType, StructureSet structureset)
        {
            if (structureset.CanAddStructure(dicomType, structureId))
            {
                return structureset.AddStructure(dicomType, structureId);
            }
            else
            {
                ErrorMessages.Add("Cannot create structure " + structureId);
                return (Structure)null;
            }
        }


        public void AddListViewVirtBolus()
        {
            this.ListViewVirtBolus.ItemsSource = GetTargets();
        }

        public List<string> GetListViewVirtBolus()
        {
            List<string> targetList = new List<string>() { };

            foreach (var element in this.ListViewVirtBolus.SelectedItems)
            {
                targetList.Add(element.ToString());
            }
            return targetList;
        }

        private void CheckBoxStructureSetDuplicateChanged(object sender, RoutedEventArgs e)
        {
            if (!(bool)this.CheckBoxStructureSetDuplicate.IsChecked)
            {
                this.TextBoxStructureSetNew.IsEnabled = false;
                this.CheckBoxRemoveEmptyStructures.IsEnabled = false;
                this.CheckBoxAddVirtualBolus.IsEnabled = false;
                this.ListViewVirtBolus.IsEnabled = false;
            }
            else
            {
                this.TextBoxStructureSetNew.IsEnabled = true;
                this.CheckBoxRemoveEmptyStructures.IsEnabled = true;
                this.CheckBoxAddVirtualBolus.IsEnabled = true;
                this.ListViewVirtBolus.IsEnabled = true;
            }
        }

        private void CheckBoxAddTransitionStructuresChanged(object sender, RoutedEventArgs e)
        {
            if (!(bool)this.CheckBoxAddTransitionStructures.IsChecked)
            {
                this.DataGrid1.IsEnabled = false;
            }
            else
            {
                this.DataGrid1.IsEnabled = true;
            }
        }

        private void CheckBoxAddVirtualBolusChanged(object sender, RoutedEventArgs e)
        {
            if (!(bool)this.CheckBoxAddVirtualBolus.IsChecked)
            {
                this.ListViewVirtBolus.IsEnabled = false;
            }
            else
            {
                this.ListViewVirtBolus.IsEnabled = true;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Create Plan
            this.Cursor = Cursors.Wait;

            var waitWindow = new WaitingWindow();
            waitWindow.Show();

            try
            {
                if ((bool)this.CheckBoxStructureSetDuplicate.IsChecked)
                {
                    DuplicateStructureSet();

                    if ((bool)this.CheckBoxRemoveEmptyStructures.IsChecked)
                    {
                        RemoveEmptyStructures();
                    }

                    if ((bool)this.CheckBoxAddVirtualBolus.IsChecked)
                    {
                        CreateNewBodyAndAddBolus();
                    }
                }

                if ((bool)this.CheckBoxAddTransitionStructures.IsChecked)
                {
                    CreatePTVTransitions();
                }

                if ((bool)this.CheckBoxPlan.IsChecked)
                {
                    if (this.context.Patient.Courses.Count() < 1)
                    {
                        MessageBox.Show("A new Course will be added since none exists.", "Message");
                        if (this.context.Patient.CanAddCourse())
                        {
                            this.NewCourse = this.context.Patient.AddCourse();
                        }
                    }
                    CreateExternalPlan();
                }
            }

            catch (Exception f)
            {
                ErrorMessages.Add("A serious error has been caught:\n" + f.Message);
            }

            this.CreateTextBlock.Text = String.Join("\n", this.ErrorMessages);
            //this.CreateButton.IsEnabled = false;
            waitWindow.Close();
            this.Cursor = null;
        }


        // ################################################################################################################
        // ########################################## PLAN ################################################################
        // ################################################################################################################

        private void CheckBoxPlanChanged(object sender, RoutedEventArgs e)
        {
            if (!(bool)this.CheckBoxPlan.IsChecked)
            {
                this.ComboBoxCourse.IsEnabled = false;
                this.TextBoxPlanName.IsEnabled = false;
                this.DataGridPrescription.IsEnabled = false;
                this.ComboBoxMachine.IsEnabled = false;
                this.ComboBoxEnergy.IsEnabled = false;
                this.ComboBoxTechnique.IsEnabled = false;
                this.ComboBoxAlgorithmDose.IsEnabled = false;
                this.ComboBoxAlgorithmOpt.IsEnabled = false;
                this.CheckBoxAddOptimizationObjectives.IsEnabled = false;
            }
            else
            {
                this.ComboBoxCourse.IsEnabled = true;
                this.TextBoxPlanName.IsEnabled = true;
                this.DataGridPrescription.IsEnabled = true;
                this.ComboBoxMachine.IsEnabled = true;
                this.ComboBoxEnergy.IsEnabled = true;
                this.ComboBoxTechnique.IsEnabled = true;
                this.ComboBoxAlgorithmDose.IsEnabled = true;
                this.ComboBoxAlgorithmOpt.IsEnabled = true;
                this.CheckBoxAddOptimizationObjectives.IsEnabled = true;
            }
        }

        public void InitiateComboBoxCourse()
        {
            if (this.context.Patient.Courses.Count() > 0)
            {
                this.ComboBoxCourse.ItemsSource = this.context.Patient.Courses.Select(u => u.Id).ToList();
                this.ComboBoxCourse.SelectedIndex = 0;
            }
        }


        private void PlanNameOnChange(object sender, TextChangedEventArgs e)
        {
            CheckIfPlanExist();
        }

        private void ComboBoxCourseSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.NewCourse = this.context.Patient.Courses.First(u => u.Id == this.ComboBoxCourse.SelectedItem.ToString());
            CheckIfPlanExist();
        }

        private void CheckIfPlanExist()
        {
            List<string> planIds = this.NewCourse.PlanSetups.Select(u => u.Id).ToList();
            if (planIds.Contains(this.TextBoxPlanName.Text))
            {
                this.LabelPlanName.Content = "\u274C";
                this.LabelPlanName.Foreground = Brushes.Red;
            }
            else
            {
                this.LabelPlanName.Content = "\u2714";
                this.LabelPlanName.Foreground = Brushes.Green;
            }
        }


        public void CreateExternalPlan()
        {
            this.NewPlan = this.NewCourse.AddExternalPlanSetup(this.NewStructureSet);
            try
            {
                this.NewPlan.Id = this.TextBoxPlanName.Text;
            }
            catch
            {
                ErrorMessages.Add("Unable to set plan name to " + this.TextBoxPlanName.Text + ". Instead using: " + this.NewPlan.Id);
            }

            SetPrescription();
            AddOptimizationObjectivesToPlan();
            AddBeams();
            AddCalculationModels();
        }

        public void SetPrescription()
        {
            int numFraction = (int)this.DataGridDosePrescription.First().NumFractions;
            double dosePerFraction = this.DataGridDosePrescription.First().DosePerFraction;

            this.NewPlan.SetPrescription(numFraction, new DoseValue(dosePerFraction, DoseValue.DoseUnit.cGy), 1);
        }

        private void PrescriptionTextChanged1(object sender, TextChangedEventArgs e)
        {
            TextBox txt = sender as TextBox;
            int ind = txt.CaretIndex;
            txt.Text = txt.Text.Replace(",", "");
            txt.CaretIndex = ind;
        }

        private void PrescriptionTextChanged2(object sender, TextChangedEventArgs e)
        {
            TextBox txt = sender as TextBox;
            int ind = txt.CaretIndex;
            txt.Text = txt.Text.Replace(",", ".");
            txt.CaretIndex = ind;
        }

        private void PrescriptionSourceUpdate(object sender, DataTransferEventArgs e)
        {
            int numFractions = (int)this.DataGridDosePrescription.First().NumFractions;
            double dosePerFraction = this.DataGridDosePrescription.First().DosePerFraction;

            this.DataGridDosePrescription.First().TotalDose = numFractions * dosePerFraction;
        }

        private void AddOptimizationObjectivesToPlan()
        {
            if ((bool)this.CheckBoxAddOptimizationObjectives.IsChecked == true)
            {
                try
                {
                    var addOptimizationObjectives = new AddOptimizationObjectives(this.dosimetryPanel, this.NewPlan);
                    addOptimizationObjectives.DeleteObjectives();
                    addOptimizationObjectives.AddOrganObjectives();
                    addOptimizationObjectives.AddNTOObjective();

                    if ((bool)this.CheckBoxAddTransitionStructures.IsChecked == true)
                    {
                        // add PTV objectives from trans volumes
                        addOptimizationObjectives.AddPTVObjectivesFromTransVolumes(this.finalDoses, this.finalTargets, this.finalTransitionalPTVs);
                    }
                    else
                    {
                        addOptimizationObjectives.AddPTVObjectives();
                    }
                }
                catch (Exception g)
                {
                    ErrorMessages.Add("Cannot add optimization objectives to plan. " + g.Message);
                }
            }
        }

        public void InitiateComboBoxMachine()
        {
            List<string> machines = new List<string>() { };
            foreach (var m in this.machineSettings.MachineSettingsData.Machine)
            {
                machines.Add(m.MachineID.Trim());
            }
            this.ComboBoxMachine.ItemsSource = machines;
        }


        private void ComboBoxMachineSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.ComboBoxMachine.SelectedIndex >= 0)
            {
                string machine = this.ComboBoxMachine.SelectedValue.ToString();
                List<string> energies = new List<string>() { };
                var machineObject = this.machineSettings.MachineSettingsData.Machine.First(u => u.MachineID.Trim() == machine);

                foreach (var en in machineObject.Energy)
                {
                    energies.Add(en.EnergyID.Trim());
                }
                this.ComboBoxEnergy.ItemsSource = energies;
                this.ComboBoxEnergy.SelectedIndex = -1;
                this.ComboBoxTechnique.SelectedIndex = -1;
                this.ComboBoxAlgorithmDose.SelectedIndex = -1;
                this.ComboBoxAlgorithmOpt.SelectedIndex = -1;
                this.ComboBoxAlgorithmPD.SelectedIndex = -1;
            }
        }


        private void ComboBoxEnergySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.ComboBoxMachine.SelectedIndex >= 0 & this.ComboBoxEnergy.SelectedIndex >= 0)
            {
                string machine = this.ComboBoxMachine.SelectedValue.ToString();
                string energy = this.ComboBoxEnergy.SelectedValue.ToString();

                var techniqueObject = this.machineSettings.MachineSettingsData.Machine
                    .First(u => u.MachineID.Trim() == machine)
                    .Energy
                    .First(w => w.EnergyID.Trim() == energy);

                List<string> techniques = new List<string>() { };
                foreach (var t in techniqueObject.Technique)
                {
                    techniques.Add(t.TechniqueType.Trim());
                }
                this.ComboBoxTechnique.ItemsSource = techniques;
                this.ComboBoxTechnique.SelectedIndex = -1;
                this.ComboBoxAlgorithmDose.SelectedIndex = -1;
                this.ComboBoxAlgorithmOpt.SelectedIndex = -1;
                this.ComboBoxAlgorithmPD.SelectedIndex = -1;
            }
        }

        private void ComboBoxTechniqueSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.ComboBoxMachine.SelectedIndex >= 0 & this.ComboBoxEnergy.SelectedIndex >= 0 & this.ComboBoxTechnique.SelectedIndex >= 0)
            {
                // Volume Dose
                string machine = this.ComboBoxMachine.SelectedValue.ToString();
                string energy = this.ComboBoxEnergy.SelectedValue.ToString();
                string technique = this.ComboBoxTechnique.SelectedValue.ToString();

                var algDoseObject = this.machineSettings.MachineSettingsData.Machine
                    .First(u => u.MachineID.Trim() == machine)
                    .Energy
                    .First(w => w.EnergyID.Trim() == energy)
                    .Technique
                    .First(z => z.TechniqueType.Trim() == technique);

                List<string> algorithmsDose = new List<string>() { };
                foreach (var a in algDoseObject.Algorithms.VolumeDose)
                {
                    algorithmsDose.Add(a.Trim());
                }
                this.ComboBoxAlgorithmDose.ItemsSource = algorithmsDose;
                this.ComboBoxAlgorithmDose.SelectedIndex = -1;

                // Optimization Dose
                List<string> algorithmsOpt = new List<string>() { };
                foreach (var a in algDoseObject.Algorithms.Optimization)
                {
                    algorithmsOpt.Add(a.Trim());
                }
                this.ComboBoxAlgorithmOpt.ItemsSource = algorithmsOpt;
                this.ComboBoxAlgorithmOpt.SelectedIndex = -1;

                // PD
                List<string> algorithmsPD = new List<string>() { };
                foreach (var a in algDoseObject.Algorithms.PortalDose)
                {
                    algorithmsPD.Add(a.Trim());
                }
                this.ComboBoxAlgorithmPD.ItemsSource = algorithmsPD;
                this.ComboBoxAlgorithmPD.SelectedIndex = -1;
            }
        }

        public void AddBeams()
        {
            if (this.ComboBoxMachine.SelectedIndex >= 0 & this.ComboBoxEnergy.SelectedIndex >= 0 & this.ComboBoxTechnique.SelectedIndex >= 0)
            {
                string machine = this.ComboBoxMachine.SelectedValue.ToString();
                string energy = this.ComboBoxEnergy.SelectedValue.ToString();
                string technique = this.ComboBoxTechnique.SelectedValue.ToString();

                int doseRate = Int32.Parse(this.machineSettings.MachineSettingsData.Machine
                                            .First(u => u.MachineID.Trim() == machine)
                                            .Energy
                                            .First(w => w.EnergyID.Trim() == energy)
                                            .Technique
                                            .First(z => z.TechniqueType.Trim() == technique)
                                            .DoseRate.First().Trim());


                string energyModeId;
                string techniqueId;
                string primaryFluenceMode;

                if (energy == "6X-FFF")
                {
                    energyModeId = "6X";
                    primaryFluenceMode = "FFF";
                }
                else if (energy == "10X-FFF")
                {
                    energyModeId = "10X";
                    primaryFluenceMode = "FFF";
                }
                else
                {
                    energyModeId = energy;
                    primaryFluenceMode = "";
                }

                if (technique == "VMAT" || technique == "ARC")
                {
                    techniqueId = "ARC";
                }
                else if (technique == "VMAT SRS" || technique == "ARC SRS")
                {
                    techniqueId = "SRS ARC";
                }
                else if (technique == "STATIC SRS")
                {
                    techniqueId = "SRS STATIC";
                }
                else
                {
                    techniqueId = "STATIC";
                }

                ExternalBeamMachineParameters machineParameters = new ExternalBeamMachineParameters(
                    machine,
                    energyModeId,
                    doseRate,
                    techniqueId,
                    primaryFluenceMode);

                VRect<double> jawPositions = new VRect<double>(-50, -50, 50, 50);

                if (technique.Contains("VMAT") || technique.Contains("ARC"))
                {
                    this.NewPlan.AddMLCArcBeam(machineParameters, null, jawPositions, 0, 181, 179, GantryDirection.Clockwise, 0, GetCenterOfPTVs());
                }
                else if (technique.Contains("IMRT"))
                {
                    this.NewPlan.AddStaticBeam(machineParameters, jawPositions, 0, 180, 0, GetCenterOfPTVs());
                    this.NewPlan.AddStaticBeam(machineParameters, jawPositions, 0, 270, 0, GetCenterOfPTVs());
                    this.NewPlan.AddStaticBeam(machineParameters, jawPositions, 0, 0, 0, GetCenterOfPTVs());
                    this.NewPlan.AddStaticBeam(machineParameters, jawPositions, 0, 90, 0, GetCenterOfPTVs());
                }
                else if (technique.Contains("STATIC"))
                {
                    this.NewPlan.AddMLCBeam(machineParameters, null, jawPositions, 0, 180, 0, GetCenterOfPTVs());
                    this.NewPlan.AddMLCBeam(machineParameters, null, jawPositions, 0, 270, 0, GetCenterOfPTVs());
                    this.NewPlan.AddMLCBeam(machineParameters, null, jawPositions, 0, 0, 0, GetCenterOfPTVs());
                    this.NewPlan.AddMLCBeam(machineParameters, null, jawPositions, 0, 90, 0, GetCenterOfPTVs());
                }

            }
        }

        public VVector GetCenterOfPTVs()
        {
            List<Structure> targets = new List<Structure>() { };
            if (this.finalTargets.Count > 0)
            {
                targets.Add(this.finalTargets.Last());
            }
            else
            {
                foreach (var t in GetTargets())
                {
                    Structure structure = this.NewStructureSet.Structures.First(id => id.Id == t);
                    targets.Add(structure);
                }
            }

            VVector PTVcenter = new VVector(0, 0, 0);
            int num = 0;
            foreach (var structure in targets)
            {
                if (structure.DicomType == "PTV")
                {
                    PTVcenter += structure.CenterPoint;
                    num += 1;
                }
            }

            if (num > 0)
            {
                PTVcenter /= num;
            }
            return PTVcenter;
        }


        public void AddCalculationModels()
        {
            if (this.ComboBoxAlgorithmDose.SelectedIndex >= 0)
            {
                this.NewPlan.SetCalculationModel(CalculationType.PhotonVolumeDose, this.ComboBoxAlgorithmDose.SelectedValue.ToString());
            }
            if (this.ComboBoxAlgorithmOpt.SelectedIndex >= 0)
            {
                this.NewPlan.SetCalculationModel(CalculationType.PhotonVMATOptimization, this.ComboBoxAlgorithmOpt.SelectedValue.ToString());
                this.NewPlan.SetCalculationModel(CalculationType.PhotonIMRTOptimization, this.ComboBoxAlgorithmOpt.SelectedValue.ToString());
            }
        }
    }
}

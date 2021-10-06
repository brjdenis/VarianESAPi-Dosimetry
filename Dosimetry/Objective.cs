using System;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using System.Reflection;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;
using System.Xml;

namespace Dosimetry
{
    public class Objective
    {
        public PlanningItem planningitem;
        public string protocol;
        public string structure;
        public bool prv;
        public Structure structureFromSet;
        public Structure auxRefStructueFromSet; // used to calculate BODY-(PTV+2cm) objective where PTV volume is unknown (stands for ref.target)
        public string structuretype;
        public string type;
        public string at;
        public string exp;
        public string exp2;
        public string than;
        public string comment;
        public int haspassed;
        public string haspassedmark;
        public VolumePresentation volumepresentation;
        public DoseValue.DoseUnit doseunitpresentation;
        public DoseValuePresentation dosevaluepresentation;
        public double value;
        public string objectivestring;
        public double at_value;  // same as at but withut unit
        public double than_value;
        public bool at_eqd;
        public bool than_eqd;
        public bool exists;
        public string fullstructurename;
        public double dosecover;

        public Dosimetry dosimetry;


        public Objective(Dosimetry dosimetry, string protocol, string structure, bool prv, string structuretype, string type, string at,
                         string exp, string than, string comment)
        {
            this.dosimetry = dosimetry;
            this.planningitem = this.dosimetry.planningitem;
            this.protocol = protocol;
            this.structure = structure;
            this.prv = prv;
            this.structuretype = structuretype;
            this.type = type;
            this.at = at;
            this.exp = exp;
            this.than = than;
            this.comment = comment;
            this.haspassed = 0;
            this.haspassedmark = "\u274C";
            this.structureFromSet = (Structure)null;
            this.auxRefStructueFromSet = (Structure)null;
            this.doseunitpresentation = DoseValue.DoseUnit.cGy;
            this.dosevaluepresentation = DoseValuePresentation.Absolute;
            this.volumepresentation = VolumePresentation.AbsoluteCm3;
            this.exists = true;
            this.fullstructurename = structure;
            this.dosecover = 1.0;
            this.value = Double.NaN; //evaluation
            this.at_value = Double.NaN; // double of at
            this.than_value = Double.NaN;  // double of than

            this.at_eqd = false;
            this.than_eqd = false;  // for converting nominal evaluated dose (this.value) to eqd2

            if (exp == "less")
            {
                this.exp2 = " < ";
            }
            else
            {
                this.exp2 = " > ";
            }
            this.objectivestring = type + at + this.exp2 + than;

            if (this.prv)
            {
                this.fullstructurename += " PRV";
            }

            if (type == "V" || type == "V-V" || type == "V(BODY-PTV)")
            {
                if (at.Contains("cGy"))
                {
                    if (at.Contains("cGy2"))
                    {
                        this.at_value = Eqd2ToNominal(Convert.ToDouble(at.Replace("cGy2", ""), System.Globalization.CultureInfo.InvariantCulture));
                        this.at_eqd = true;
                    }
                    else
                    {
                        this.at_value = Convert.ToDouble(at.Replace("cGy", ""), System.Globalization.CultureInfo.InvariantCulture);
                    }

                }
                else
                {
                    this.doseunitpresentation = DoseValue.DoseUnit.Percent;
                    this.dosevaluepresentation = DoseValuePresentation.Relative;
                    this.at_value = Convert.ToDouble(at.Replace("%", ""), System.Globalization.CultureInfo.InvariantCulture);
                }

                if (than.Contains("cm3"))
                {
                    this.than_value = Convert.ToDouble(than.Replace("cm3", ""), System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    this.volumepresentation = VolumePresentation.Relative;
                    this.than_value = Convert.ToDouble(than.Replace("%", ""), System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            else if (type == "D" || type == "D(BODY-PTV)")
            {
                if (at.Contains("cm3"))
                {
                    this.at_value = Convert.ToDouble(at.Replace("cm3", ""), System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    this.volumepresentation = VolumePresentation.Relative;
                    this.at_value = Convert.ToDouble(at.Replace("%", ""), System.Globalization.CultureInfo.InvariantCulture);
                }

                if (than.Contains("cGy"))
                {
                    if (than.Contains("cGy2"))
                    {
                        this.than_value = Convert.ToDouble(than.Replace("cGy2", ""), System.Globalization.CultureInfo.InvariantCulture);
                        this.than_eqd = true;
                    }
                    else
                    {
                        this.than_value = Convert.ToDouble(than.Replace("cGy", ""), System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
                else
                {
                    this.doseunitpresentation = DoseValue.DoseUnit.Percent;
                    this.dosevaluepresentation = DoseValuePresentation.Relative;
                    this.than_value = Convert.ToDouble(than.Replace("%", ""), System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            else if ((type == "Dmax") || (type == "Dmean"))
            {
                if (than.Contains("cGy"))
                {
                    if (than.Contains("cGy2"))
                    {
                        this.than_value = Convert.ToDouble(than.Replace("cGy2", ""), System.Globalization.CultureInfo.InvariantCulture);
                        this.than_eqd = true;
                    }
                    else
                    {
                        this.than_value = Convert.ToDouble(than.Replace("cGy", ""), System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
                else
                {
                    this.doseunitpresentation = DoseValue.DoseUnit.Percent;
                    this.dosevaluepresentation = DoseValuePresentation.Relative;
                    this.than_value = Convert.ToDouble(than.Replace("%", ""), System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            else if (type == "R50" || type == "R100" || type == "BODY-(PTV+2cm)")
            {
                this.than_value = 0;
            }
        }


        public void SetBodyMinusPTVPlus2cmObjective(string tableString, double ptvVolume=Double.NaN)
        {
            // set the at value: max dose for the structure (BODY-ptv+2cm)
            //List<double> volumes = new List<double>() { 1.8, 3.8, 7.4, 13.2, 22.0, 34.0, 50.0, 70.0, 95.0, 126.0, 163.0 };
            List<double> volumes = this.dosimetry.BodyMinusPTVTables["Volume"].First();
            List<double> table = new List<double>() { };

            if (tableString == "table1") // tolerance
            {
                table = this.dosimetry.BodyMinusPTVTables["BODY-(PTV+2cm)"].First();
                //table = new List<double>() { 50, 50, 50, 50, 54, 58, 62, 66, 70, 73, 77 };
            }
            else
            {
                table = this.dosimetry.BodyMinusPTVTables["BODY-(PTV+2cm)"].Last();
                //table = new List<double>() { 57, 57, 58, 58, 63, 68, 77, 86, 89, 91, 94 };
            }

            if (Double.IsNaN(ptvVolume))
            {
                ptvVolume = this.structureFromSet.Volume;
            }
            

            if (ptvVolume < volumes.First())
            {
                this.than_value = table.First();
            }
            else if (ptvVolume > volumes.Last())
            {
                this.than_value = table.Last();
            }
            else
            {
                int i = 0;
                for (i = 0; i < volumes.Count; i++)
                {
                    if (ptvVolume < volumes[i])
                    {
                        break;
                    }
                }
                this.than_value = Math.Round(table[i - 1] + ((table[i] - table[i - 1]) / (volumes[i] - volumes[i - 1])) * (ptvVolume - volumes[i - 1]), 1);
            }
            this.than = this.than_value.ToString() + "%";
            this.objectivestring = this.type + this.exp2 + this.than;
        }


        public void SetR50R100Objective(string tableString)
        {
            // set the at value for R50 and R100
            //List<double> volumes = new List<double>() { 1.8, 3.8, 7.4, 13.2, 22.0, 34.0, 50.0, 70.0, 95.0, 126.0, 163.0 };
            List<double> volumes = this.dosimetry.BodyMinusPTVTables["Volume"].First();
            List<double> table = new List<double>() { };

            if (tableString.Contains("table1")) // tolerance
            {
                if (this.type == "R100")
                {
                    table = this.dosimetry.BodyMinusPTVTables["R100"].First();
                    //table = new List<double>() {1.2, 1.2, 1.2, 1.2, 1.2, 1.2, 1.2, 1.2, 1.2, 1.2, 1.2 };
                }
                else // R50
                {
                    table = this.dosimetry.BodyMinusPTVTables["R50"].First();
                    //table = new List<double>() { 5.9, 5.5, 5.1, 4.7, 4.5, 4.3, 4.0, 3.5, 3.3, 3.1, 2.9 };
                }
            }
            else // table2
            {
                if (this.type == "R100")
                {
                    table = this.dosimetry.BodyMinusPTVTables["R100"].Last();
                    //table = new List<double>() { 1.5, 1.5, 1.5, 1.5, 1.5, 1.5, 1.5, 1.5, 1.5, 1.5, 1.5 };
                }
                else // R50
                {
                    table = this.dosimetry.BodyMinusPTVTables["R50"].Last();
                    //table = new List<double>() { 7.5, 6.5, 6.0, 5.8, 5.5, 5.3, 5.0, 4.8, 4.4, 4.0, 3.7};
                }
            }

            double ptvVolume = this.structureFromSet.Volume;

            if (ptvVolume < volumes.First())
            {
                this.than_value = table.First();
            }
            else if (ptvVolume > volumes.Last())
            {
                this.than_value = table.Last();
            }
            else
            {
                int i = 0;
                for (i = 0; i < volumes.Count; i++)
                {
                    if (ptvVolume < volumes[i])
                    {
                        break;
                    }
                }
                this.than_value = Math.Round(table[i - 1] + ((table[i] - table[i - 1]) / (volumes[i] - volumes[i - 1])) * (ptvVolume - volumes[i - 1]), 2);
            }
            this.than = this.than_value.ToString();
            this.objectivestring = this.type + this.exp2 + this.than;
        }


        private bool IsPRVInProtocol(List<Objective> objectives)
        {
            foreach (var obj in objectives)
            {
                if (obj.structure.Contains(this.structure) && obj.prv)
                {
                    return true;
                }
            }
            return false;
        }

        private Structure MatchStructureFromProtocol(StructureSet structureset, bool prv2)
        {
            
            Dictionary<string, List<Structure>> possibleOrganStrDict = new Dictionary<string, List<Structure>> { };

            foreach (var possibleOrgan in this.dosimetry.StructureMapping[this.structure])
            {
                List<Structure> tempStr = new List<Structure> { };

                foreach (var possibleStr in structureset.Structures)
                {
                    if (!possibleStr.IsEmpty)
                    {
                        if (possibleStr.Id.IndexOf(possibleOrgan, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            tempStr.Add(possibleStr);
                        }
                    }
                }
                possibleOrganStrDict.Add(possibleOrgan, tempStr);
            }

            Dictionary<string, List<Structure>> OrganStrFinal = new Dictionary<string, List<Structure>> { };

            foreach (var possibleOrganStr in possibleOrganStrDict)
            {
                List<Structure> strTemp = new List<Structure> { };
                
                foreach (var s in possibleOrganStr.Value.OrderByDescending(x => x.Id))
                {
                    if (!s.IsEmpty)
                    {
                        if (prv2)
                        {
                            if (s.Id.IndexOf(possibleOrganStr.Key, StringComparison.OrdinalIgnoreCase) >= 0 &&
                                (s.Id.IndexOf("PRV", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 s.Id.IndexOf("PORV", StringComparison.OrdinalIgnoreCase) >= 0))
                            {
                                strTemp.Add(s);
                            }
                        }
                        else
                        {
                            if (s.Id.IndexOf(possibleOrganStr.Key, StringComparison.OrdinalIgnoreCase) >= 0 &&
                                (s.Id.IndexOf("PRV", StringComparison.OrdinalIgnoreCase) < 0 &&
                                 s.Id.IndexOf("PORV", StringComparison.OrdinalIgnoreCase) < 0))
                            {
                                strTemp.Add(s);
                            }
                        }
                    }
                }
                if (strTemp.Count() > 0)
                {
                    OrganStrFinal.Add(possibleOrganStr.Key, strTemp);
                }
            }

            if (OrganStrFinal.Count() == 0)
            {
                return (Structure)null;
            }
            else
            {
                List<int> distances = new List<int> { };
                List<Structure> strDist = new List<Structure> { };
                foreach (var p in OrganStrFinal)
                {
                    Fastenshtein.Levenshtein lev = new Fastenshtein.Levenshtein(p.Key);

                    foreach (var item in p.Value)
                    {
                        distances.Add(lev.DistanceFrom(item.Id));
                        strDist.Add(item);
                    }
                }

                int minDist = distances.Min();
                int minDistIndex = distances.IndexOf(minDist);

                return strDist.ElementAt(minDistIndex);
            }
        }


        public Structure FindStructure(StructureSet structureset, List<Objective> objectives)
        {
            Structure str = (Structure)null;
            // this.structure is the name of the organ at risk in the protocol
            // StructureMapping[this.structure] are the possible contours of the organs in the strctset
            // str is the structure from strctset that is identified as corresponding to this.structure

            if (this.prv == false)
            {
                if (IsPRVInProtocol(objectives) == true)
                {
                    str = MatchStructureFromProtocol(structureset, false);
                }
                else
                {
                    str = MatchStructureFromProtocol(structureset, true);

                    if (str is null)
                    {
                        str = MatchStructureFromProtocol(structureset, false);
                    }
                }
            }
            else
            {
                str = MatchStructureFromProtocol(structureset, true);
            }

            return str;
        }


        private void DetermineHasPassed()
        {
            if (double.IsNaN(this.value))
            {
                this.haspassed = 2;
                this.haspassedmark = "\u2753";
                return;
            }

            if (this.exp == "less")
            {
                if (this.value <= this.than_value)
                {
                    this.haspassed = 1;
                    this.haspassedmark = "\u2714";
                }
            }
            else
            {
                if (this.value > this.than_value)
                {
                    this.haspassed = 1;
                    this.haspassedmark = "\u2714";
                }
            }
        }

        public double Eqd2ToNominal(double EQD)
        {
            // convert EQD to nominal dose
            double result;

            if (planningitem is ExternalPlanSetup & (double)this.dosimetry.numberoffractions.First() > 0)
            {
                double n = (double)this.dosimetry.numberoffractions.First();
                double ab = this.dosimetry.StructureAlphaBeta[this.structure];
                result = -n * ab / 2.0 + Math.Sqrt((n * ab / 2.0) * (n * ab / 2.0) + n * (EQD / 100.0) * (ab + 2.0));
                result *= 100.0;
            }
            else
            {
                result = EQD;
            }
            return result;
        }

        public double Nominal2EQD(double nominal)
        {
            // convert EQD to nominal dose
            double result;

            if (planningitem is ExternalPlanSetup & (double)this.dosimetry.numberoffractions.First() > 0)
            {
                double n = (double)this.dosimetry.numberoffractions.First();
                double ab = this.dosimetry.StructureAlphaBeta[this.structure];
                result = nominal * (ab + nominal / (100.0*n)) / (ab + 2.0);
            }
            else
            {
                result = nominal;
            }
            return result;
        }

        public void CalculateObjectiveOrgan(StructureSet structureset, List<Objective> objectives, double Normalization, string knownStructure="")
        {
            // First get the appropriate structure in the structureset
            // If knownStructure is passed do not do any searching

            

            if (knownStructure == "")
            {
                this.structureFromSet = FindStructure(structureset, objectives);
            }
            else
            {
                this.structureFromSet = structureset.Structures.Where(s => s.Id == knownStructure).First();
            }

            if (this.structureFromSet == (Structure)null)
            {
                this.exists = false;
                return;
            }

            if (Normalization <= 0 | Double.IsNaN(Normalization))
            {
                this.value = Double.NaN;
                DetermineHasPassed();
                return;
            }

            // check for valid dose. If none, set value to Nan
            if (this.planningitem is ExternalPlanSetup)
            {
                var planTemp = this.planningitem as ExternalPlanSetup;

                if (!planTemp.IsDoseValid)
                {
                    this.value = Double.NaN;
                    DetermineHasPassed();
                    return;
                }
            }

            if (this.planningitem == (ExternalPlanSetup)null)
            {
                this.value = Double.NaN;
                DetermineHasPassed();
                return;
            }

            // Not possible to get relative dose from PlanSum:
            if (this.planningitem is PlanSum)
            {
                if (this.dosevaluepresentation == DoseValuePresentation.Relative)
                {
                    this.exists = false;
                    return;
                }
            }

            // DoseNormalization defines percent dose. 
            // If Prescribed dose = 5000cGy, but DoseNormalization dose=7500cGy, then
            // 60% => 0.6 * DoseNormalization/PrescribedDose = Normalization
            if (this.dosevaluepresentation == DoseValuePresentation.Relative)
            {
                if (this.type == "V" | this.type == "V-V")
                {
                    this.at_value = this.at_value * Normalization;
                }
                else if (this.type == "D" | this.type == "Dmean" | this.type == "Dmax")
                {
                    this.than_value = this.than_value * Normalization;
                }
            }

            DVHData dvh = this.planningitem.GetDVHCumulativeData(
                                                                 this.structureFromSet,
                                                                 this.dosevaluepresentation,
                                                                 this.volumepresentation,
                                                                 0.1);
            this.dosecover = dvh.Coverage;
            
            if (this.type == "V")
            {
                this.value = this.planningitem.GetVolumeAtDose(
                                                               this.structureFromSet,
                                                               new DoseValue(this.at_value, this.doseunitpresentation),
                                                               this.volumepresentation);
                if (Double.IsNaN(this.value))
                {
                    DVHPoint[] dvdData = dvh.CurveData;
                    this.value = Dosimetry.calculateDVHPoint(dvdData, this.at_value, "V");
                    
                    if (Double.IsInfinity(this.value))
                    {
                        this.value = Double.NaN;
                    }
                }
            }
            else if (this.type == "V-V")
            {
                this.value = this.planningitem.GetVolumeAtDose(
                                                               this.structureFromSet,
                                                               new DoseValue(this.at_value, this.doseunitpresentation),
                                                               this.volumepresentation);
                if (Double.IsNaN(this.value))
                {
                    DVHPoint[] dvdData = dvh.CurveData;
                    this.value = Dosimetry.calculateDVHPoint(dvdData, this.at_value, "V");

                    if (Double.IsInfinity(this.value))
                    {
                        this.value = Double.NaN;
                    }
                }
                //subtract from organ volume
                if (this.volumepresentation == VolumePresentation.AbsoluteCm3)
                {
                    this.value = this.structureFromSet.Volume - this.value;
                }
                else
                {
                    this.value = 100.0 - this.value;
                }
            }
            else if (this.type == "D")
            {
                DoseValue dose = this.planningitem.GetDoseAtVolume(
                                                                   this.structureFromSet,
                                                                   this.at_value,
                                                                   this.volumepresentation,
                                                                   this.dosevaluepresentation);
                this.value = dose.Dose;

                if (Double.IsNaN(this.value))
                {
                    DVHPoint[] dvdData = dvh.CurveData;
                    this.value = Dosimetry.calculateDVHPoint(dvdData, this.at_value, "D");
                    
                    if (Double.IsInfinity(this.value))
                    {
                        this.value = Double.NaN;
                    }
                }

                // convert to eqd for comparison
                if (this.than_eqd)
                {
                    this.value = Nominal2EQD(this.value);
                }
            }
            else if (this.type == "Dmax")
            {
                DoseValue dose = this.planningitem.GetDoseAtVolume(
                                                                   this.structureFromSet,
                                                                   0.001,
                                                                   this.volumepresentation,
                                                                   this.dosevaluepresentation);
                this.value = dose.Dose;

                if (Double.IsNaN(this.value))
                {
                    DVHPoint[] dvdData = dvh.CurveData;
                    this.value = Dosimetry.calculateDVHPoint(dvdData, 0.001, "D");
                    
                    if (Double.IsInfinity(this.value))
                    {
                        this.value = Double.NaN;
                    }
                }

                // convert to eqd for comparison
                if (this.than_eqd)
                {
                    this.value = Nominal2EQD(this.value);
                }
            }
            else if (this.type == "Dmean")
            {
                DoseValue dose = dvh.MeanDose;
                this.value = dose.Dose;
                
                // convert to eqd for comparison
                if (this.than_eqd)
                {
                    this.value = Nominal2EQD(this.value);
                }
            }

            // Renormalize value if in percentage
            if (this.dosevaluepresentation == DoseValuePresentation.Relative)
            {
                if (this.type == "D" | this.type == "Dmean" | this.type == "Dmax")
                {
                    this.value = this.value / Normalization;
                }
            }

            DetermineHasPassed();
        }

        public Structure CreateBodyMinusPTVPlus2cm(StructureSet structureset, Structure PTV)
        {
            // create the empty "ptv+2cm" structure
            Structure BodyMinusPTVPlus2cm = (Structure)null;
            if (structureset.CanAddStructure("IRRAD_VOLUME", "PlChkTemp1"))
            {
                Structure ptvPlus2cm = structureset.AddStructure("IRRAD_VOLUME", "PlChkTemp1");
                ptvPlus2cm.SegmentVolume = PTV.SegmentVolume.Margin(20.0);
               
                if (structureset.CanAddStructure("IRRAD_VOLUME", "PlChkTemp2"))
                {
                    BodyMinusPTVPlus2cm = structureset.AddStructure("IRRAD_VOLUME", "PlChkTemp2");
                    Structure Body = structureset.Structures.First(e => e.DicomType == "EXTERNAL");
                    
                    BodyMinusPTVPlus2cm.SegmentVolume = Body.SegmentVolume.Sub(ptvPlus2cm.SegmentVolume);

                    // Remove temp structures, except for last one.
                    if (structureset.CanRemoveStructure(ptvPlus2cm))
                    {
                        structureset.RemoveStructure(ptvPlus2cm);
                    }
                }
            }
            return BodyMinusPTVPlus2cm;
        }

        public Structure CreateBodyMinusPTV(StructureSet structureset, Structure PTV)
        {
            // create the body-ptv structure
            Structure BodyMinusPTV = (Structure)null;

            if (structureset.CanAddStructure("IRRAD_VOLUME", "PlChkTemp3"))
            {
                BodyMinusPTV = structureset.AddStructure("IRRAD_VOLUME", "PlChkTemp3");
                Structure Body = structureset.Structures.First(e => e.DicomType == "EXTERNAL");
                BodyMinusPTV.SegmentVolume = Body.SegmentVolume.Sub(PTV.SegmentVolume);
            }
            return BodyMinusPTV;
        }

        public void RemoveStructure(StructureSet structureset, Structure structureToRemove)
        {
            if (structureset.CanRemoveStructure(structureToRemove))
            {
                structureset.RemoveStructure(structureToRemove);
            }
        }


        public void CalculateObjectivePTV(StructureSet structureset, string knownStructure, double prescDose)
        {
            this.structureFromSet = structureset.Structures.Where(s => s.Id == knownStructure).First();

            if (this.type == "BODY-(PTV+2cm)")
            {
                if (this.auxRefStructueFromSet != (Structure)null)
                {
                    double ptvVol = this.auxRefStructueFromSet.Volume;
                    SetBodyMinusPTVPlus2cmObjective(this.than, ptvVol); // set than_value
                    CalculateObjectivePTVHelper(prescDose, structureset, ptvVol);
                }
                else
                {
                    this.structureFromSet = (Structure)null;
                    CalculateObjectivePTVHelper(prescDose, structureset);
                }
            }
            else if (this.type == "V(BODY-PTV)" || this.type == "D(BODY-PTV)")
            {
                if (this.auxRefStructueFromSet != (Structure)null)
                {
                    double ptvVol = this.auxRefStructueFromSet.Volume;
                    CalculateObjectivePTVHelper(prescDose, structureset, ptvVol);
                }
                else
                {
                    this.structureFromSet = (Structure)null;
                    CalculateObjectivePTVHelper(prescDose, structureset);
                }
            }
            else if (this.type == "R50" || this.type == "R100")
            {
                SetR50R100Objective(this.than);
                Structure body = structureset.Structures.First(e => e.DicomType == "EXTERNAL");
                double ptvVol = this.structureFromSet.Volume;
                this.structureFromSet = body;
                CalculateObjectivePTVHelper(prescDose, structureset, ptvVol);
            }
            else
            {
                CalculateObjectivePTVHelper(prescDose, structureset);
            }
        }

        public void CalculateObjectivePTVHelper(double prescDose, StructureSet structureset, double ptvVol=0)
        {

            if (this.structureFromSet == (Structure)null)
            {
                this.exists = false;
                DetermineHasPassed();
                return;
            }

            // check for valid dose. If none, set value to Nan
            if (this.planningitem is ExternalPlanSetup)
            {
                var planTemp = this.planningitem as ExternalPlanSetup;

                if (!planTemp.IsDoseValid)
                {
                    this.value = Double.NaN;
                    DetermineHasPassed();
                    return;
                }
            }

            if (this.planningitem == (ExternalPlanSetup)null)
            {
                this.value = Double.NaN;
                DetermineHasPassed();
                return;
            }

            bool isDoseRelative = false;
            bool isVolumeRelative = false;

            if (this.dosevaluepresentation == DoseValuePresentation.Relative)
            {
                isDoseRelative = true;
                
                if (this.type == "V" || this.type == "V(BODY-PTV)")
                {
                    this.at_value = this.at_value * prescDose / 100.0; // cGy
                }
                                
                this.doseunitpresentation = DoseValue.DoseUnit.cGy;
                this.dosevaluepresentation = DoseValuePresentation.Absolute;
            }

            if (this.volumepresentation == VolumePresentation.Relative)
            {
                if (this.type == "V(BODY-PTV)")
                {
                    this.volumepresentation = VolumePresentation.AbsoluteCm3;
                    this.than_value = this.than_value * ptvVol / 100.0;
                    isVolumeRelative = true;
                }
                else if (this.type == "D(BODY-PTV)")
                {
                    this.volumepresentation = VolumePresentation.AbsoluteCm3;
                    this.at_value = this.at_value * ptvVol / 100.0;
                    isVolumeRelative = true;
                }
            }
            

            DVHData dvh = this.planningitem.GetDVHCumulativeData(
                                                                 this.structureFromSet,
                                                                 this.dosevaluepresentation,
                                                                 this.volumepresentation,
                                                                 0.1);
            this.dosecover = dvh.Coverage;

            if (this.type == "V" || this.type == "V(BODY-PTV)")
            {
                this.value = this.planningitem.GetVolumeAtDose(
                                                               this.structureFromSet,
                                                               new DoseValue(this.at_value, this.doseunitpresentation),
                                                               this.volumepresentation);
                if (Double.IsNaN(this.value))
                {
                    DVHPoint[] dvdData = dvh.CurveData;
                    this.value = Dosimetry.calculateDVHPoint(dvdData, this.at_value, "V");

                    if (Double.IsInfinity(this.value))
                    {
                        this.value = Double.NaN;
                    }
                }

                if (this.type == "V(BODY-PTV)")
                {
                    if (isVolumeRelative)
                    {
                        this.value = 100 * this.value / ptvVol;
                        this.than_value = 100 * this.than_value / ptvVol; // convert back to % of ptv
                    }
                }
            }

            else if (this.type == "D" || this.type == "D(BODY-PTV)")
            {
                DoseValue dose = this.planningitem.GetDoseAtVolume(
                                                                   this.structureFromSet,
                                                                   this.at_value,
                                                                   this.volumepresentation,
                                                                   this.dosevaluepresentation);

                this.value = dose.Dose;

                if (Double.IsNaN(this.value))
                {
                    DVHPoint[] dvdData = dvh.CurveData;
                    this.value = Dosimetry.calculateDVHPoint(dvdData, this.at_value, "D");

                    if (Double.IsInfinity(this.value))
                    {
                        this.value = Double.NaN;
                    }
                }

                if (isDoseRelative)
                {
                    this.value = 100 * this.value / prescDose;
                }
            }

            else if (this.type == "Dmax")
            {
                DoseValue dose = this.planningitem.GetDoseAtVolume(
                                                                   this.structureFromSet,
                                                                   0.0,
                                                                   this.volumepresentation,
                                                                   this.dosevaluepresentation);

                this.value = dose.Dose;

                if (Double.IsNaN(this.value))
                {
                    DVHPoint[] dvdData = dvh.CurveData;
                    this.value = Dosimetry.calculateDVHPoint(dvdData, 0.0, "D");

                    if (Double.IsInfinity(this.value))
                    {
                        this.value = Double.NaN;
                    }
                }

                if (isDoseRelative)
                {
                    this.value = 100 * this.value / prescDose;
                }
            }

            else if (this.type == "Dmean")
            {
                DoseValue dose = dvh.MeanDose;
                this.value = dose.Dose;

                if (isDoseRelative)
                {
                    this.value = 100 * this.value / prescDose;
                }
            }
            
            else if (this.type == "R50" || this.type == "R100")
            {
               if (this.type == "R50")
                {
                    this.at_value = 0.5 * prescDose;
                }
                else // R100
                {
                    this.at_value = prescDose;
                }

                this.value = this.planningitem.GetVolumeAtDose(
                                                               this.structureFromSet,
                                                               new DoseValue(this.at_value, this.doseunitpresentation),
                                                               this.volumepresentation);

                if (Double.IsNaN(this.value))
                {
                    DVHPoint[] dvdData = dvh.CurveData;
                    this.value = Dosimetry.calculateDVHPoint(dvdData, this.at_value, "V");

                    if (Double.IsInfinity(this.value))
                    {
                        this.value = Double.NaN;
                    }
                }

                this.value = this.value / ptvVol;
            }
            else if (this.type == "BODY-(PTV+2cm)")
            {
                // similar to Dmax
                DoseValue dose = this.planningitem.GetDoseAtVolume(
                                                                    this.structureFromSet,
                                                                    0.0,
                                                                    this.volumepresentation,
                                                                    this.dosevaluepresentation);
                this.value = dose.Dose;
                
                if (Double.IsNaN(this.value))
                {
                    DVHPoint[] dvdData = dvh.CurveData;
                    this.value = Dosimetry.calculateDVHPoint(dvdData, 0.0, "D");

                    if (Double.IsInfinity(this.value))
                    {
                        this.value = Double.NaN;
                    }
                }
                this.value = 100 * this.value / prescDose;  // convert to relative
            }
            DetermineHasPassed();
        }
    }
}

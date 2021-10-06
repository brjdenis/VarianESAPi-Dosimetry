// Add optimization objectives from Dosimetry datagrids into ExternalPlanSetup
// Only Lower, Upper and gEUD (a=1) objectives can be added.
using Dosimetry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace Dosimetry
{
    public class AddOptimizationObjectives
    {
        public MainWindow dosimetryPanel;
        public ExternalPlanSetup plan;
        public AddOptimizationObjectives(MainWindow dosimetryPanel, ExternalPlanSetup plan)
        {
            this.dosimetryPanel = dosimetryPanel;
            this.plan = plan;
        }


        public void DeleteObjectives()
        {
            foreach (var obj in this.plan.OptimizationSetup.Objectives)
            {
                this.plan.OptimizationSetup.RemoveObjective(obj);
            }
        }

        public void AddOrganObjectives()
        {
            foreach (var row in this.dosimetryPanel.DataGridSpecialOrgansList)
            {
                if (row.Structure != null && row.Structure != "")
                {
                    OptimizationObjectiveOperator optimizationOperator;

                    if (row.ObjectiveExp == "<")
                    {
                        optimizationOperator = OptimizationObjectiveOperator.Upper;
                    }
                    else
                    {
                        optimizationOperator = OptimizationObjectiveOperator.Lower;
                    }

                    double dose = 0;
                    double volume = 0;
                    double priority = 0;

                    Structure structure = this.plan.StructureSet.Structures.Where(s => s.Id == row.Structure).First();

                    if (row.ObjectiveType == "V" & (row.AtUnit == "cGy" || row.AtUnit == "%") & (row.ThanUnit == "cm3" || row.ThanUnit == "%"))
                    {
                        if (row.AtUnit == "cGy")
                        {
                            dose = ConvertTextToDouble(row.AtValue);
                        }
                        else if (row.AtUnit == "%")
                        {
                            dose = ConvertTextToDouble(row.AtValue) * this.dosimetryPanel.DoseNormalizationSpecial * this.plan.TotalDose.Dose / 100.0;
                        }

                        if (row.ThanUnit == "%")
                        {
                            volume = ConvertTextToDouble(row.ThanValue);
                        }
                        else if (row.ThanUnit == "cm3")
                        {
                            volume = Math.Round(100 * ConvertTextToDouble(row.ThanValue) / structure.Volume, 1);
                        }
                    }

                    else if (row.ObjectiveType == "D" & (row.AtUnit == "%" || row.AtUnit == "cm3") & (row.ThanUnit == "cGy" || row.ThanUnit == "%"))
                    {
                        if (row.AtUnit == "%")
                        {
                            volume = ConvertTextToDouble(row.AtValue);
                        }
                        else if (row.AtUnit == "cm3")
                        {
                            volume = Math.Round(100 * ConvertTextToDouble(row.AtValue) / structure.Volume, 1);
                        }

                        if (row.ThanUnit == "cGy")
                        {
                            dose = ConvertTextToDouble(row.ThanValue);
                        }
                        else if (row.ThanUnit == "%")
                        {
                            dose = ConvertTextToDouble(row.ThanValue) * this.dosimetryPanel.DoseNormalizationSpecial * this.plan.TotalDose.Dose / 100.0;
                        }
                    }

                    else if ((row.ObjectiveType == "Dmax" || row.ObjectiveType == "Dmean") & (row.ThanUnit == "cGy" || row.ThanUnit == "%"))
                    {
                        volume = 0;
                        if (row.ThanUnit == "cGy")
                        {
                            dose = ConvertTextToDouble(row.ThanValue);
                        }
                        else if (row.ThanUnit == "%")
                        {
                            dose = ConvertTextToDouble(row.ThanValue) * this.dosimetryPanel.DoseNormalizationSpecial * this.plan.TotalDose.Dose / 100.0;
                        }
                    }

                    else if (row.ObjectiveType == "V-V" & (row.AtUnit == "cGy" || row.AtUnit == "%") & (row.ThanUnit == "cm3" || row.ThanUnit == "%"))
                    {
                        // V-Vd > V' is equal to Vd < V0 - V'
                        if (row.AtUnit == "cGy")
                        {
                            dose = ConvertTextToDouble(row.AtValue);
                        }
                        else if (row.AtUnit == "%")
                        {
                            dose = ConvertTextToDouble(row.AtValue) * this.dosimetryPanel.DoseNormalizationSpecial * this.plan.TotalDose.Dose / 100.0;
                        }

                        if (row.ThanUnit == "%")
                        {
                            volume = 100.0 - ConvertTextToDouble(row.ThanValue);
                        }
                        else if (row.ThanUnit == "cm3")
                        {
                            volume = Math.Round(100.0 - 100.0 * ConvertTextToDouble(row.ThanValue) / structure.Volume, 1);
                        }

                        if (optimizationOperator == OptimizationObjectiveOperator.Lower)
                        {
                            optimizationOperator = OptimizationObjectiveOperator.Upper;
                        }
                        else if (optimizationOperator == OptimizationObjectiveOperator.Upper)
                        {
                            optimizationOperator = OptimizationObjectiveOperator.Lower;
                        }
                    }

                    // fix volumes larger than 100%
                    if (volume > 100.0)
                    {
                        volume = 100.0;
                    }

                    if (row.ObjectiveType == "Dmean")
                    {
                        this.plan.OptimizationSetup.AddEUDObjective(
                                    structure,
                                    OptimizationObjectiveOperator.Upper,
                                    new DoseValue(dose, DoseValue.DoseUnit.cGy.ToString()),
                                    1.0,
                                    priority
                                    );
                    }
                    else
                    {
                        this.plan.OptimizationSetup.AddPointObjective(
                                    structure,
                                    optimizationOperator,
                                    new DoseValue(dose, DoseValue.DoseUnit.cGy.ToString()),
                                    volume,
                                    priority
                                    );
                    }
                }
            }
        }


        public void AddPTVObjectives()
        {
            if (this.dosimetryPanel.DataGridSpecialPTV2List.Count < 1)
            {
                // useful when using autocrop for ptvs
                CreateDefaultPTVObjectives();
                return;
            }

            foreach (var row in this.dosimetryPanel.DataGridSpecialPTV2List)
            {
                if (row.Structure != null && row.Structure != "")
                {
                    OptimizationObjectiveOperator optimizationOperator;

                    if (row.ObjectiveExp == "<")
                    {
                        optimizationOperator = OptimizationObjectiveOperator.Upper;
                    }
                    else
                    {
                        optimizationOperator = OptimizationObjectiveOperator.Lower;
                    }

                    double dose = 0;
                    double volume = 0;
                    double priority = 175;

                    Structure structure = this.plan.StructureSet.Structures.Where(s => s.Id == row.Structure).First();
                    double totalDose = FindPTVDose(row.Structure);

                    if (row.ObjectiveType == "V" & (row.AtUnit == "cGy" || row.AtUnit == "%") & (row.ThanUnit == "cm3" || row.ThanUnit == "%"))
                    {
                        if (row.AtUnit == "cGy")
                        {
                            dose = ConvertTextToDouble(row.AtValue);
                        }
                        else if (row.AtUnit == "%")
                        {

                            dose = ConvertTextToDouble(row.AtValue) * totalDose / 100.0;
                        }

                        if (row.ThanUnit == "%")
                        {
                            volume = ConvertTextToDouble(row.ThanValue);
                        }
                        else if (row.ThanUnit == "cm3")
                        {
                            volume = Math.Round(100 * ConvertTextToDouble(row.ThanValue) / structure.Volume, 1);
                        }
                    }

                    else if (row.ObjectiveType == "D" & (row.AtUnit == "%" || row.AtUnit == "cm3") & (row.ThanUnit == "cGy" || row.ThanUnit == "%"))
                    {
                        if (row.AtUnit == "%")
                        {
                            volume = ConvertTextToDouble(row.AtValue);
                        }
                        else if (row.AtUnit == "cm3")
                        {
                            volume = Math.Round(100 * ConvertTextToDouble(row.AtValue) / structure.Volume, 1);
                        }

                        if (row.ThanUnit == "cGy")
                        {
                            dose = ConvertTextToDouble(row.ThanValue);
                        }
                        else if (row.ThanUnit == "%")
                        {
                            dose = ConvertTextToDouble(row.ThanValue) * totalDose / 100.0;
                        }
                    }

                    else if ((row.ObjectiveType == "Dmax" || row.ObjectiveType == "Dmean") & (row.ThanUnit == "cGy" || row.ThanUnit == "%"))
                    {
                        volume = 0;
                        if (row.ThanUnit == "cGy")
                        {
                            dose = ConvertTextToDouble(row.ThanValue);
                        }
                        else if (row.ThanUnit == "%")
                        {
                            dose = ConvertTextToDouble(row.ThanValue) * totalDose / 100.0;
                        }
                    }

                    // fix volumes larger than 100%
                    if (volume > 100.0)
                    {
                        volume = 100.0;
                    }

                    // if volume/dose close to 0 or 100%, re-set
                    if (volume <= 100.0 & volume >= 80)
                    {
                        volume = 100.0;
                    }
                    else if (volume >= 0 & volume <= 20)
                    {
                        volume = 0;
                    }

                    if (dose > totalDose)
                    {
                        dose = totalDose * 1.015;
                    }
                    else if (dose < totalDose)
                    {
                        dose = totalDose * 0.990;
                    }
                    else
                    {
                        dose = totalDose;
                    }

                    if (row.ObjectiveType == "Dmean")
                    {
                        this.plan.OptimizationSetup.AddEUDObjective(
                                    structure,
                                    OptimizationObjectiveOperator.Upper,
                                    new DoseValue(dose, DoseValue.DoseUnit.cGy.ToString()),
                                    1.0,
                                    priority
                                    );
                    }
                    else
                    {
                        this.plan.OptimizationSetup.AddPointObjective(
                                    structure,
                                    optimizationOperator,
                                    new DoseValue(dose, DoseValue.DoseUnit.cGy.ToString()),
                                    volume,
                                    priority
                                    );
                    }
                }
            }
        }

        public void CreateDefaultPTVObjectives()
        {
            foreach (var row in this.dosimetryPanel.DataGridSpecialPTV1List)
            {
                if (row.Structure != null && row.Structure != ""
                    && row.TotalDose != null && row.TotalDose != "")
                {
                    Structure structure = this.plan.StructureSet.Structures.Where(s => s.Id == row.Structure).First();
                    double totalDose = FindPTVDose(row.Structure);

                    this.plan.OptimizationSetup.AddPointObjective(
                                    structure,
                                    OptimizationObjectiveOperator.Upper,
                                    new DoseValue(totalDose * 1.015, DoseValue.DoseUnit.cGy.ToString()),
                                    0,
                                    175
                                    );
                    this.plan.OptimizationSetup.AddPointObjective(
                                    structure,
                                    OptimizationObjectiveOperator.Lower,
                                    new DoseValue(totalDose * 0.99, DoseValue.DoseUnit.cGy.ToString()),
                                    100.0,
                                    175
                                    );
                }
            }
        }

        public void AddPTVObjectivesFromTransVolumes(List<double> doses, List<Structure> targets, List<Structure> transitionals)
        {
            if (targets.Count < 1 | doses.Count < 1)
            {
                return;
            }
            // Upper and Lower for max dose PTV:
            this.plan.OptimizationSetup.AddPointObjective(
                                targets[0],
                                OptimizationObjectiveOperator.Upper,
                                new DoseValue(doses[0] * 1.015, DoseValue.DoseUnit.cGy.ToString()),
                                0,
                                175
                                );
            this.plan.OptimizationSetup.AddPointObjective(
                                targets[0],
                                OptimizationObjectiveOperator.Lower,
                                new DoseValue(doses[0] * 0.99, DoseValue.DoseUnit.cGy.ToString()),
                                100.0,
                                175
                                );

            // For the rest of the targets put only lower objectives:

            for (int i = 1; i < targets.Count; i++)
            {
                this.plan.OptimizationSetup.AddPointObjective(
                                targets[i],
                                OptimizationObjectiveOperator.Lower,
                                new DoseValue(doses[i] * 0.99, DoseValue.DoseUnit.cGy.ToString()),
                                100.0,
                                175
                                );
            }

            // For transitional volumes add only upper objectives
            for (int i = 0; i < transitionals.Count; i++)
            {
                this.plan.OptimizationSetup.AddPointObjective(
                                transitionals[i],
                                OptimizationObjectiveOperator.Upper,
                                new DoseValue(doses[i + 1] * 1.015, DoseValue.DoseUnit.cGy.ToString()),
                                0,
                                175
                                );
            }
        }

        private double FindPTVDose(string structure)
        {
            double totalDose = 0;
            foreach (var row in this.dosimetryPanel.DataGridSpecialPTV1List)
            {
                if (row.Structure != null && row.Structure != "" && row.Structure == structure)
                {
                    if (row.TotalDose != null && row.TotalDose != "")
                    {
                        totalDose = ConvertTextToDouble(row.TotalDose);
                        break;
                    }

                }
            }
            return totalDose;
        }


        public void AddNTOObjective()
        {
            this.plan.OptimizationSetup.AddNormalTissueObjective(
                150,
                3,
                105,
                60,
                0.3
                );
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
    }
}


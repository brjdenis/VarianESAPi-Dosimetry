using System;
using System.Collections.Generic;
using System.Windows;
using VMS.TPS.Common.Model.API;
using System.Windows.Controls;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: ESAPIScript(IsWriteable = true)]

namespace VMS.TPS
{
    public class Script
    {
        public ScriptContext scriptcontext;
        public PlanningItem planningitem;
        public StructureSet structureset;

        public List<int> numberoffractions = new List<int> { };
        public List<double> doseperfractions = new List<double> { };
        public List<string> targetvolumes = new List<string> { };

        public string WorkingFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public string protocolPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Settings\\Protocols.xml";
        public string protocolGroupsPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Settings\\Protocol_groups.xml";
        public string structureMappingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Settings\\Structure_mapping.xml";
        public string planTypesPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Settings\\Plan_types.xml";

        public string DatabasePathTxtPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Settings\\Exchange_path.csv";
        public string DatabasePath;

        public string MachineSettingsPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Settings\\Machine_settings.xml";

        public List<string> BodyMinutPTVTablesPath = new List<string>()
        {
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Settings\\Table1.csv", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Settings\\Table2.csv"
        };

        private void GetDatabasePath()
        {
            List<string> paths = new List<string>() { };
            var lines = File.ReadAllLines(this.DatabasePathTxtPath);
            this.DatabasePath = lines[0];
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Execute(ScriptContext scriptcontext)
        {
            this.scriptcontext = scriptcontext;
            ExternalPlanSetup plan = scriptcontext.ExternalPlanSetup;
            this.structureset = scriptcontext.StructureSet;

            //If the plan and structureset is null do nothing
            if (this.structureset == null)
            {
                MessageBox.Show("StructureSet not active.", "Error");
                return;
            }

            if (plan != null)
            {
                if (plan.IsDoseValid & plan.DosePerFraction.ValueAsString == "" & !plan.NumberOfFractions.HasValue)
                {
                    MessageBox.Show("The plan must either be without calculated dose, or with calculated dose and with a prescription.", "Error");
                    return;
                }

                if (plan.DosePerFraction.ValueAsString != "")
                {
                    doseperfractions.Add(plan.DosePerFraction.Dose);
                }
                else
                {
                    doseperfractions.Add(Double.NaN);
                }

                if (plan.NumberOfFractions.HasValue)
                {
                    numberoffractions.Add(plan.NumberOfFractions.Value);
                }
                else
                {
                    numberoffractions.Add(0);
                }
                targetvolumes.Add(plan.TargetVolumeID);
                planningitem = plan;
            }
            else
            {
                numberoffractions.Add(0);
                doseperfractions.Add(Double.NaN);
                targetvolumes.Add("");
                planningitem = (PlanningItem)null;
            }
            this.scriptcontext.Patient.BeginModifications();
            Run();
        }

        public void Run()
        {
            string runType = "Dosimetry";
            try
            {
                GetDatabasePath();
                Dosimetry.Dosimetry dosimetry = new Dosimetry.Dosimetry(
                    this.scriptcontext,
                    this.planningitem,
                    this.structureset,
                    this.numberoffractions,
                    this.doseperfractions,
                    this.targetvolumes,
                    this.WorkingFolder,
                    this.protocolPath,
                    this.DatabasePath,
                    this.protocolGroupsPath,
                    this.structureMappingPath,
                    this.BodyMinutPTVTablesPath,
                    this.MachineSettingsPath,
                    runType
                    );
                dosimetry.Initiate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n\n" + ex.StackTrace);
            }
        }

        
    }
}
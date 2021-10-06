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
using System.Xml.Schema;

namespace Dosimetry
{
    public class Dosimetry
    {
        public ScriptContext scriptcontext;
        public PlanningItem planningitem;
        public StructureSet structureset;
        public List<int> numberoffractions;
        public List<double> doseperfractions;
        public List<string> targetvolumes;
        public string WorkingFolder;
        public Dictionary<string, List<string>> StructureMapping;
        public Dictionary<string, double> StructureAlphaBeta;
        public Dictionary<string, List<List<double>>> BodyMinusPTVTables;

        public string protocolPath;
        public string protocolGroupsPath;
        public string databasePath;
        public string structureMappingPath;
        public List<string> BodyMinutPTVTablesPath;
        public string machineSettingsPath;

        public string runType;

        public Dosimetry(ScriptContext scriptcontext,
            PlanningItem planningitem,
            StructureSet structureset,
            List<int> numberoffractions,
            List<double> doseperfractions,
            List<string> targetvolumes,
            string WorkingFolder,
            string protocolPath,
            string databasePath,
            string protocolGroupsPath,
            string structureMappingPath,
            List<string> BodyMinutPTVTablesPath,
            string machineSettingsPath,
            string runType)
        {
            this.scriptcontext = scriptcontext;
            this.planningitem = planningitem;
            this.structureset = structureset;
            this.numberoffractions = numberoffractions;
            this.doseperfractions = doseperfractions;
            this.targetvolumes = targetvolumes;
            this.WorkingFolder = WorkingFolder;
            this.protocolPath = protocolPath;
            this.databasePath = databasePath;
            this.protocolGroupsPath = protocolGroupsPath;
            this.structureMappingPath = structureMappingPath;
            this.BodyMinutPTVTablesPath = BodyMinutPTVTablesPath;
            this.machineSettingsPath = machineSettingsPath;
            this.runType = runType;
        }

        public Tuple<Dictionary<string, List<string>>, Dictionary<string, double>> GetStructureMapping()
        {
            Dictionary<string, List<string>> structures = new Dictionary<string, List<string>> { };
            Dictionary<string, double> alphabeta = new Dictionary<string, double> { };

            XmlDocument xml = new XmlDocument();

            using (FileStream fileStream = new FileStream(this.structureMappingPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                xml.Load(fileStream);
            }

            XmlNodeList resources = xml.SelectNodes("root/Structure");

            foreach (XmlNode node in resources)
            {
                List<string> temp = new List<string> { };
                foreach (XmlNode r in node.SelectNodes("Alias"))
                {
                    temp.Add(r.InnerText);
                }
                structures.Add(node.Attributes["name"].Value, temp);
                alphabeta.Add(node.Attributes["name"].Value, Convert.ToDouble(node.Attributes["alphabeta"].Value, System.Globalization.CultureInfo.InvariantCulture));
            }
            return Tuple.Create(structures, alphabeta);
        }


        public Tuple<Dictionary<string, List<string>>, Dictionary<string, List<string>>> GetProtocolGroup()
        {
            XmlDocument xml = new XmlDocument();
            
            using (FileStream fileStream = new FileStream(this.protocolGroupsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                xml.Load(fileStream);
            }

            Dictionary<string, List<string>> names = new Dictionary<string, List<string>> { };
            Dictionary<string, List<string>> namesfull = new Dictionary<string, List<string>> { };

            XmlNodeList resources = xml.DocumentElement.SelectNodes("/root/ClinicalProtocol");

            foreach (XmlNode node_st in resources)
            {
                string protocolname = node_st.Attributes["name"].Value;

                XmlNodeList nodes_st = node_st.SelectNodes("./Protocol");

                List<string> protTemp = new List<string> { };
                List<string> protTemp2 = new List<string> { };

                foreach (XmlNode node_obj in nodes_st)
                {
                    string tt = node_obj.InnerText;
                    string fullname;

                    if (tt == "Default")
                    {
                        tt = protocolname;
                        fullname = protocolname;
                    }
                    else
                    {
                        fullname = protocolname + " " + tt;
                    }
                    protTemp.Add(tt);
                    protTemp2.Add(fullname);
                }

                names.Add(protocolname, protTemp);
                namesfull.Add(protocolname, protTemp2);
            }
            return Tuple.Create(names, namesfull);
        }


        public List<Objective> GetProtocol(string protocol)
        {
            XmlDocument xml = new XmlDocument();

            List<Objective> objectives = new List<Objective> { };

            using (FileStream fileStream = new FileStream(this.protocolPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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
                    bool isprv = false;

                    // Remove PRV tag from protocol structure name if it exists
                    if (structurename.Contains(" PRV"))
                    {
                        isprv = true;
                        structurename = structurename.Replace(" PRV", "");
                    }

                    Objective objective = new Objective(
                        this,
                        protocol,
                        structurename,
                        isprv,
                        structuretype,
                        node_obj.Attributes["type"].Value,
                        node_obj.Attributes["at"].Value,
                        node_obj.Attributes["exp"].Value,
                        node_obj.Attributes["than"].Value,
                        //node_obj.InnerText
                        node_obj.Attributes["comment"].Value
                        );
                    objectives.Add(objective);
                }
            }

            return objectives;
        }

        public static double calculateDVHPoint(DVHPoint[] dvh, double x0, string type)
        {
            double[] D = new double[dvh.Length];
            double[] V = new double[dvh.Length];

            for (int i = 0; i < dvh.Length; i++)
            {
                D[i] = dvh[i].DoseValue.Dose;
                V[i] = dvh[i].Volume;
            }

            if (type == "V")
            {
                double dD = D[1] - D[0];
                int ind = (int)(x0 / dD);

                if (ind > D.Length)
                {
                    return 0;
                }
                else if (D[ind] > x0)
                {
                    return V[ind] + ((V[ind] - V[ind - 1]) / (D[ind] - D[ind - 1])) * (x0 - D[ind]);
                }
                else if (D[ind] < x0)
                {
                    return V[ind] + ((V[ind] - V[ind + 1]) / (D[ind] - D[ind + 1])) * (x0 - D[ind]);
                }
                else
                {
                    return V[ind];
                }
            }
            else
            {
                if (x0 <= V.Last())
                {
                    return D.Last();
                }
                else if (x0 >= V.First())
                {
                    return D.First();
                }
                else
                {
                    int right = V.Length;
                    int left = 0;
                    int middle = (left + right) / 2;
                    // Search by bisection, then interpolate
                    int i = 0;
                    while (right - left > 1 && i < 1000)
                    {
                        if (x0 > V[middle])
                        {
                            right = middle;
                        }
                        else if (x0 < V[middle])
                        {
                            left = middle;
                        }
                        else
                        {
                            return D[middle];
                        }
                        middle = (left + right) / 2;
                        i++;
                    }
                    return D[left] + ((D[right] - D[left]) / (V[right] - V[left])) * (x0 - V[left]);
                }
                
            }
        }


        public void ReadBODYminusPTVTables()
        {

            string[] table1 = File.ReadAllLines(this.BodyMinutPTVTablesPath.ElementAt(0));
            string[] table2 = File.ReadAllLines(this.BodyMinutPTVTablesPath.ElementAt(1));

            Dictionary<string, List<List<double>>> table_dict = new Dictionary<string, List<List<double>>>() { };

            List<double> Vol = new List<double>() { };
            List<double> R100_1 = new List<double>() { };
            List<double> R50_1 = new List<double>() { };
            List<double> BodyMPTV_1 = new List<double>() { };

            for (int i = 1; i < table1.Count(); i++)
            {
                string[] columns = table1[i].Split(',');
                Vol.Add(Convert.ToDouble(columns[0], System.Globalization.CultureInfo.InvariantCulture));
                R100_1.Add(Convert.ToDouble(columns[1], System.Globalization.CultureInfo.InvariantCulture));
                R50_1.Add(Convert.ToDouble(columns[2], System.Globalization.CultureInfo.InvariantCulture));
                BodyMPTV_1.Add(Convert.ToDouble(columns[3], System.Globalization.CultureInfo.InvariantCulture));
            }

            List<double> R100_2 = new List<double>() { };
            List<double> R50_2 = new List<double>() { };
            List<double> BodyMPTV_2 = new List<double>() { };

            for (int i = 1; i < table2.Count(); i++)
            {
                string[] columns = table2[i].Split(',');
                R100_2.Add(Convert.ToDouble(columns[1], System.Globalization.CultureInfo.InvariantCulture));
                R50_2.Add(Convert.ToDouble(columns[2], System.Globalization.CultureInfo.InvariantCulture));
                BodyMPTV_2.Add(Convert.ToDouble(columns[3], System.Globalization.CultureInfo.InvariantCulture));
            }

            table_dict["Volume"] = new List<List<double>> { Vol };
            table_dict["R100"] = new List<List<double>> { R100_1, R100_2 };
            table_dict["R50"] = new List<List<double>> { R50_1, R50_2 };
            table_dict["BODY-(PTV+2cm)"] = new List<List<double>> { BodyMPTV_1, BodyMPTV_2 };

            this.BodyMinusPTVTables = table_dict;
        }

        public void Initiate()
        {

            var temp = GetStructureMapping();
            StructureMapping = temp.Item1;
            StructureAlphaBeta = temp.Item2;

            try
            {
                ReadBODYminusPTVTables();
            }
            catch
            {
                MessageBox.Show("Cannot read Table1.csv and Table2.csv", "Error");
                return;
            }

            MainWindow dosimetrypanel = new MainWindow(this);
            dosimetrypanel.ShowDialog();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Dosimetry
{
    public class MachineSettings
    {
        public string MachineSettingsPath;
        public Root MachineSettingsData;
        public string XMLValidationString = "";

        public MachineSettings(string machineSettings)
        {
            this.MachineSettingsPath = machineSettings;
        }

        [XmlRoot(ElementName = "Algorithms")]
        public class Algorithms
        {
            [XmlElement(ElementName = "VolumeDose")]
            public List<string> VolumeDose { get; set; }
            [XmlElement(ElementName = "Optimization")]
            public List<string> Optimization { get; set; }
            [XmlElement(ElementName = "PortalDose")]
            public List<string> PortalDose { get; set; }
        }

        [XmlRoot(ElementName = "Technique")]
        public class Technique
        {
            [XmlElement(ElementName = "TechniqueType")]
            public string TechniqueType { get; set; }
            [XmlElement(ElementName = "Algorithms")]
            public Algorithms Algorithms { get; set; }
            [XmlElement(ElementName = "DoseRate")]
            public List<string> DoseRate { get; set; }
        }

        [XmlRoot(ElementName = "Energy")]
        public class Energy
        {
            [XmlElement(ElementName = "EnergyID")]
            public string EnergyID { get; set; }
            [XmlElement(ElementName = "Technique")]
            public List<Technique> Technique { get; set; }
        }

        [XmlRoot(ElementName = "Machine")]
        public class Machine
        {
            [XmlElement(ElementName = "MachineID")]
            public string MachineID { get; set; }
            [XmlElement(ElementName = "MachineTolerance")]
            public List<string> MachineTolerance { get; set; }
            [XmlElement(ElementName = "CouchName")]
            public List<string> CouchName { get; set; }
            [XmlElement(ElementName = "Energy")]
            public List<Energy> Energy { get; set; }
        }

        [XmlRoot(ElementName = "Root")]
        public class Root
        {
            [XmlElement(ElementName = "Machine")]
            public List<Machine> Machine { get; set; }
        }



        public void Read()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Root));
 
            using (FileStream fileStream = new FileStream(this.MachineSettingsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                this.MachineSettingsData = (Root)serializer.Deserialize(fileStream);
            }
        }

        public void ValidateAgainstSchema(string xmlString, string schemaPath)
        {
            // Check against schema
            XmlDocument xmlDoc = new XmlDocument();

            try
            {
                xmlDoc.LoadXml(xmlString);
            }
            catch (Exception ex)
            {
                this.XMLValidationString += "\n" + ex.ToString();
                return;
            }

            XmlTextReader schemaReader = new XmlTextReader(schemaPath);
            XmlSchema schema = XmlSchema.Read(schemaReader, null);
            xmlDoc.Schemas.Add(schema);

            xmlDoc.Validate(DocumentValidationHandler);
        }


        public void DocumentValidationHandler(object sender, ValidationEventArgs e)
        {
            this.XMLValidationString += "\n" + e.Message;
        }


        public Root DeserializeFromString(string xmlString)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Root));

            using (TextReader reader = new StringReader(xmlString))
            {
                return (Root)serializer.Deserialize(reader);
            }
        }

        public void Validate(string xmlString, string schemaPath)
        {
            ValidateAgainstSchema(xmlString, schemaPath);

        }
    }
}

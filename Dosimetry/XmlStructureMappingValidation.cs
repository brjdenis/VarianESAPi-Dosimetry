using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Dosimetry
{
    public class XmlStructureMappingValidation
    {
        public string schemaPath;
        public string xmlString;
        public Root xml;
        public string XMLValidationString = "";

        public XmlStructureMappingValidation(string xmlstring, string schemapath)
        {
            this.xmlString = xmlstring;
            this.schemaPath = schemapath;
        }


        [XmlRoot(ElementName = "Structure")]
        public class Structure
        {
            [XmlElement(ElementName = "Alias")]
            public List<string> Alias { get; set; }
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; }
            [XmlAttribute(AttributeName = "alphabeta")]
            public string Alphabeta { get; set; }
        }

        [XmlRoot(ElementName = "root")]
        public class Root
        {
            [XmlElement(ElementName = "Structure")]
            public List<Structure> Structure { get; set; }
        }




        public void ValidateAgainstSchema()
        {
            // Check against schema
            XmlDocument xmlDoc = new XmlDocument();

            try
            {
                xmlDoc.LoadXml(this.xmlString);
            }
            catch (Exception ex)
            {
                this.XMLValidationString += "\n" + ex.ToString();
                return;
            }

            XmlTextReader schemaReader = new XmlTextReader(this.schemaPath);
            XmlSchema schema = XmlSchema.Read(schemaReader, null);
            xmlDoc.Schemas.Add(schema);

            xmlDoc.Validate(DocumentValidationHandler);
        }


        public void DocumentValidationHandler(object sender, ValidationEventArgs e)
        {
            this.XMLValidationString += "\n" + e.Message;
        }


        public void DeserializeFromString()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Root));

            using (TextReader reader = new StringReader(this.xmlString))
            {
                this.xml = (Root)serializer.Deserialize(reader);
            }
        }

        public void Validate()
        {
            ValidateAgainstSchema();
            if (XMLValidationString != "")
            {
                return;
            }
            else
            {
                ValidateDetails();
            }
        }


        public void ValidateDetails()
        {
            DeserializeFromString();

            // unique structures
            List<string> structures = new List<string>() { };

            foreach(var s in this.xml.Structure)
            {
                structures.Add(s.Name);
                if (s.Alphabeta == "")
                {
                    this.XMLValidationString += "\n(" + s.Name + ") Alphabeta must be a number.";
                }
            }

            if (structures.Count != structures.Distinct().Count())
            {
                this.XMLValidationString += "\nStructures must not be duplicated.";
            }
        }
    }
}

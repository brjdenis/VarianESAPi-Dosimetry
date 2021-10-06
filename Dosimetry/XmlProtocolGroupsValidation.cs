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
    public class XmlProtocolGroupsValidation
    {
        public string schemaPath;
        public string xmlString;
        public Root xml;
        public string XMLValidationString = "";

        public XmlProtocolGroupsValidation(string xmlstring, string schemapath)
        {
            this.xmlString = xmlstring;
            this.schemaPath = schemapath;
        }


        [XmlRoot(ElementName = "ClinicalProtocol")]
        public class ClinicalProtocol
        {
            [XmlElement(ElementName = "Protocol")]
            public List<string> Protocol { get; set; }
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; }
        }

        [XmlRoot(ElementName = "root")]
        public class Root
        {
            [XmlElement(ElementName = "ClinicalProtocol")]
            public List<ClinicalProtocol> ClinicalProtocol { get; set; }
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

            // all ClinicalProtocols should be unique
            List<string> protocols = new List<string>() { };

            foreach(var p in this.xml.ClinicalProtocol)
            {
                protocols.Add(p.Name);
            }

            if (protocols.Count != protocols.Distinct().Count())
            {
                this.XMLValidationString += "\nClinical Protocols must not be duplicated.";
            }
        }
    }
}

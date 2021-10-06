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
    public class XmlProtocolValidation
    {
        public string schemaPath;
        public string xmlString;
        public Root xml;
        public string XMLValidationString = "";

        public XmlProtocolValidation(string xmlstring, string schemapath)
        {
            this.xmlString = xmlstring;
            this.schemaPath = schemapath;
        }


        [XmlRoot(ElementName = "Objective")]
        public class Objective
        {
            [XmlAttribute(AttributeName = "type")]
            public string Type { get; set; }
            [XmlAttribute(AttributeName = "at")]
            public string At { get; set; }
            [XmlAttribute(AttributeName = "exp")]
            public string Exp { get; set; }
            [XmlAttribute(AttributeName = "than")]
            public string Than { get; set; }
            [XmlAttribute(AttributeName = "comment")]
            public string Comment { get; set; }
        }

        [XmlRoot(ElementName = "Structure")]
        public class Structure
        {
            [XmlElement(ElementName = "Objective")]
            public List<Objective> Objective { get; set; }
            [XmlAttribute(AttributeName = "type")]
            public string Type { get; set; }
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; }
        }

        [XmlRoot(ElementName = "Protocol")]
        public class Protocol
        {
            [XmlElement(ElementName = "Structure")]
            public List<Structure> Structure { get; set; }
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; }
        }

        [XmlRoot(ElementName = "root")]
        public class Root
        {
            [XmlElement(ElementName = "Protocol")]
            public List<Protocol> Protocol { get; set; }
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
                ValidateObjectives();
            }
        }


        public void ValidateObjectives()
        {
            DeserializeFromString();

            foreach (var protocol in this.xml.Protocol)
            {
                if (protocol.Name.Trim() == "")
                {
                    this.XMLValidationString += "\nProtocol name must not be an empty string.";
                }

                foreach (var structure in protocol.Structure)
                {
                    string structuretype = structure.Type.Trim();

                    if (structure.Name.Trim() == "")
                    {
                        this.XMLValidationString += "\n(" + protocol.Name.Trim() + ") Structure name must not be an empty string.";
                    }

                    if (!(structuretype == "Organ" || structuretype == "PTV" || structuretype == "CTV" || structuretype == "GTV" || structuretype == "ITV"))
                    {
                        this.XMLValidationString += "\n(" + structure.Name.Trim() + ") Structure type can only be Organ, PTV, CTV, GTV, ITV.";
                    }

                    foreach (var objective in structure.Objective)
                    {
                        string objectivetype = objective.Type.Trim();
                        string at = objective.At.Trim();
                        string exp = objective.Exp.Trim();
                        string than = objective.Than.Trim();

                        if (structuretype == "Organ")
                        {
                            if (!(objectivetype == "V" || objectivetype == "D" || objectivetype == "Dmax" || objectivetype == "Dmean" || objectivetype == "V-V"))
                            {
                                this.XMLValidationString += "\n(" + protocol.Name.Trim() + ", " + structure.Name.Trim() +") For organs objective type can only be V, D, Dmean, Dmax and V-V.";
                            }

                            if (objectivetype == "V" || objectivetype == "V-V")
                            {
                                if (at == "" | !(exp == "less" || exp == "more") | than == "")
                                {
                                    this.XMLValidationString += "\n(" + protocol.Name.Trim() + ", " + structure.Name.Trim() + ") At and Than must not be empty. Exp must be either 'less' or 'more'";
                                }
                                else
                                {
                                    if (!(at.Contains("cGy") & than.Contains("cm3") || at.Contains("cGy2") & than.Contains("cm3") || at.Contains("cGy") & than.Contains("%") ||
                                          at.Contains("cGy2") & than.Contains("%") || at.Contains("%") & than.Contains("%") || at.Contains("%") & than.Contains("cm3")))
                                    {
                                        this.XMLValidationString += "\n(" + protocol.Name.Trim() + ", " + structure.Name.Trim() + ") Combination of At and Than is not proper.";
                                    }
                                }
                            }
                            if (objectivetype == "D")
                            {
                                if (at == "" | !(exp == "less" || exp == "more") | than == "")
                                {
                                    this.XMLValidationString += "\n(" + protocol.Name.Trim() + ", " + structure.Name.Trim() + ") At and Than must not be empty. Exp must be either 'less' or 'more'";
                                }
                                else
                                {
                                    if (!(than.Contains("cGy") & at.Contains("cm3") || than.Contains("cGy2") & at.Contains("cm3") || than.Contains("cGy") & at.Contains("%") ||
                                          than.Contains("cGy2") & at.Contains("%") || than.Contains("%") & at.Contains("%") || than.Contains("%") & at.Contains("cm3")))
                                    {
                                        this.XMLValidationString += "\n(" + protocol.Name.Trim() + ", " + structure.Name.Trim() + ") Combination of At and Than is not proper.";
                                    }
                                }
                            }
                            if (objectivetype == "Dmean" || objectivetype == "Dmax")
                            {
                                if (!(exp == "less" || exp == "more") | than == "")
                                {
                                    this.XMLValidationString += "\n(" + protocol.Name.Trim() + ", " + structure.Name.Trim() + ") Than must not be empty. Exp must be either 'less' or 'more'";
                                }
                                else
                                {
                                    if (!(than.Contains("cGy") || than.Contains("cGy2") || than.Contains("%")))
                                    {
                                        this.XMLValidationString += "\n(" + protocol.Name.Trim() + ", " + structure.Name.Trim() + ") Than must be either cGy, cGy2 or %.";
                                    }
                                }
                            }
                        }

                        else if (structuretype == "PTV" || structuretype == "CTV" || structuretype == "GTV" || structuretype == "ITV")
                        {
                            if (!(objectivetype == "V" || objectivetype == "D" || objectivetype == "Dmax" || objectivetype == "Dmean" || objectivetype == "V(BODY-PTV)" ||
                                objectivetype == "D(BODY-PTV)" || objectivetype == "BODY-(PTV+2cm)" || objectivetype == "R50" || objectivetype == "R100"))
                            {
                                this.XMLValidationString += "\n(" + protocol.Name.Trim() + ", " + structure.Name.Trim() + ") For targets objective type can only be V, D, Dmean, Dmax, V(BODY-PTV), D(BODY-PTV), BODY-(PTV+2cm), R50 or R100.";
                            }

                            if (objectivetype == "V")
                            {
                                if (at == "" | !(exp == "less" || exp == "more") | than == "")
                                {
                                    this.XMLValidationString += "\n(" + protocol.Name.Trim() + ", " + structure.Name.Trim() + ") At and Than must not be empty. Exp must be either 'less' or 'more'";
                                }
                                else
                                {
                                    if (!(at.Contains("cGy") & than.Contains("cm3") || at.Contains("cGy") & than.Contains("%") ||
                                          at.Contains("%") & than.Contains("%") || at.Contains("%") & than.Contains("cm3")))
                                    {
                                        this.XMLValidationString += "\n(" + protocol.Name.Trim() + ", " + structure.Name.Trim() + ") Combination of At and Than is not proper.";
                                    }
                                }
                            }
                            if (objectivetype == "D")
                            {
                                if (at == "" | !(exp == "less" || exp == "more") | than == "")
                                {
                                    this.XMLValidationString += "\n(" + protocol.Name.Trim() + ", " + structure.Name.Trim() + ") At and Than must not be empty. Exp must be either 'less' or 'more'";
                                }
                                else
                                {
                                    if (!(than.Contains("cGy") & at.Contains("cm3") || than.Contains("cGy") & at.Contains("%") ||
                                          than.Contains("%") & at.Contains("%") || than.Contains("%") & at.Contains("cm3")))
                                    {
                                        this.XMLValidationString += "\n(" + protocol.Name.Trim() + ", " + structure.Name.Trim() + ") Combination of At and Than is not proper.";
                                    }
                                }
                            }
                            if (objectivetype == "Dmean" || objectivetype == "Dmax")
                            {
                                if (!(exp == "less" || exp == "more") | than == "")
                                {
                                    this.XMLValidationString += "\n(" + protocol.Name.Trim() + ", " + structure.Name.Trim() + ") Than must not be empty. Exp must be either 'less' or 'more'";
                                }
                                else
                                {
                                    if (!(than.Contains("cGy") || than.Contains("%")))
                                    {
                                        this.XMLValidationString += "\n(" + protocol.Name.Trim() + ", " + structure.Name.Trim() + ") Than must be either cGy, cGy2 or %.";
                                    }
                                }
                            }
                            if (objectivetype == "V(BODY-PTV)")
                            {
                                if (at == "" | !(exp == "less" || exp == "more") | than == "")
                                {
                                    this.XMLValidationString += "\n(" + protocol.Name.Trim() + ", " + structure.Name.Trim() + ") At and Than must not be empty. Exp must be either 'less' or 'more'";
                                }
                                else
                                {
                                    if (!(at.Contains("cGy") & than.Contains("%") || at.Contains("%") & than.Contains("%")))
                                    {
                                        this.XMLValidationString += "\n(" + protocol.Name.Trim() + ", " + structure.Name.Trim() + ") Combination of At and Than is not proper.";
                                    }
                                }
                            }
                            if (objectivetype == "D(BODY-PTV)")
                            {
                                if (at == "" | !(exp == "less" || exp == "more") | than == "")
                                {
                                    this.XMLValidationString += "\n(" + protocol.Name.Trim() + ", " + structure.Name.Trim() + ") At and Than must not be empty. Exp must be either 'less' or 'more'";
                                }
                                else
                                {
                                    if (!(than.Contains("cGy") & at.Contains("%") || than.Contains("%") & at.Contains("%")))
                                    {
                                        this.XMLValidationString += "\n(" + protocol.Name.Trim() + ", " + structure.Name.Trim() + ") Combination of At and Than is not proper.";
                                    }
                                }
                            }
                            if (objectivetype == "BODY-(PTV+2cm)" || objectivetype == "R50" || objectivetype == "R100")
                            {
                                if (!(exp == "less" || exp == "more") | than == "")
                                {
                                    this.XMLValidationString += "\n(" + protocol.Name.Trim() + ", " + structure.Name.Trim() + ") Than must not be empty. Exp must be either 'less' or 'more'";
                                }
                                else
                                {
                                    if(!(than == "table1" || than == "table2"))
                                    {
                                        this.XMLValidationString += "\n(" + protocol.Name.Trim() + ", " + structure.Name.Trim() + ") Than must either 'table1' or 'table2'";
                                    }
                                }
                            }
                        }
                    }
                }
            }
            // check that protocols and organs per protocol are not duplicated
            List<string> protocols = new List<string>() { };
            foreach(var p in this.xml.Protocol)
            {
                protocols.Add(p.Name);
                List<string> organ = new List<string>() { };
                foreach(var s in p.Structure)
                {
                    organ.Add(s.Name);
                }
                if (organ.Count != organ.Distinct().Count())
                {
                    this.XMLValidationString += "\n(" + p.Name + ") Organs must not be duplicated.";
                }
            }

            if (protocols.Count != protocols.Distinct().Count())
            {
                this.XMLValidationString += "\nProtocols must not be duplicated.";
            }
            
        }
    }
}

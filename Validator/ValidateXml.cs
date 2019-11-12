using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Validator
{
    class ValidateXml
    {
        public void ValidateQueries(string path)
        {
            var successMessage = new string[]
            {
                "YOU'VE GONE AND DONE IT!"
                , "YOU ARE ON A ROLL MEOW!"
                , "LOOK AT ALL YOU'VE ACCOMPLISHED!"
                , "GO GET THAT 5 STAR REVIEW!"
                , "YOU'RE THE PRIDE AND JOY OF AZARA!"
                , "HOW DO YOU KEEP ALL THAT BRAIN FROM SPILLING OUT!?"
                , "YOU'RE THE WHOLE PACKAGE, BEAUTIFUL, SMART, FUNNY, AND YOU CAN FOLLOW CODING STANDARDS!"
                , "KNOCK KNOCK!! WHO'S THERE? A SUPER STAAAHHHHH!!"
            };

            var validTables = new string[]
            {
                "allergy"
                , "problem"
                , "appointment"
                , "charge"
                , "diagnosis"
                , "encounter"
                , "encounterpayerxref"
                , "lab"
                , "result"
                , "maintenance"
                , "immunization"
                , "medication"
                , "payer"
                , "patient_payer"
                , "patient"
                , "provider"
                , "assessment"
                , "medicationlist"
                , "prescription"
                , "order"
                , "vitals"
                , "claim"
                , "users"
                , "balance"
                , "document"
                , "chargesummary"
                , "payment"
                , "eventlog"
                , "adjustment"
                , "denial"
                , "hours"
                , "employee"
                , "rx_provider"
                , "rx_transaction"
                , "drug"
                , "work_hours"
                , "provider_order"
                , "immunizationcombodetails"
                , "chargediagnosis"
                , "availableslots"
                , "patientcohort"
                , "obepisode"
                , "oboutcome"
            };

            XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
            xmlReaderSettings.IgnoreComments = true;

            XDocument doc;

            try
            {
                doc = XDocument.Load(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nERROR: {0}", ex.Message);
                return;
            }
            
            var strDoc = doc.ToString().ToLower();

            XmlDocument xml = new XmlDocument();
            XmlReader reader = XmlReader.Create(new StringReader(strDoc), xmlReaderSettings);

            List<string> elementNames = new List<string>();
            HashSet<string> eleHash = new HashSet<string>(); 

            int errorCount = 0;

            try
            {
                xml.Load(reader);
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nERROR: " + ex.Message);
                return;
            }

            //ignore XmlDeclaration
            if (xml.FirstChild.NodeType == XmlNodeType.XmlDeclaration)
            {
                xml.RemoveChild(xml.FirstChild);
            }

            XmlNode root = xml.FirstChild;

            foreach (XmlNode node in root.ChildNodes)
            {
                elementNames.Add(node.Name);
                //query cannot have nested queries
                if (HasChildElements(node))
                {
                    Console.WriteLine("\nERROR: " + "<" + node.Name + ">" + " cannot have child " + "<" + node.FirstChild.Name + ">");
                    errorCount++;
                }


                var table = node.Attributes?["table"]?.Value;
                var type = node.Attributes?["type"]?.Value;
                

                //verify that the table and type attributes are specified
                if (table == null || type == null)
                {
                    Console.WriteLine("\nERROR: missing attribute in " + "<" + node.Name + " table=\"" + table + "\" type=\"" + type + "\">");
                    errorCount++;
                }

                //verify the table name is valid
                if (!validTables.Contains(table))
                {
                    Console.WriteLine("\nERROR: table=\"" + table + "\" is not a valid table. Refer to the Schema file for help") ;
                    errorCount++;
                }

                //verify that the query element name matches the table, and is seperated by an underscore <maintenance_example table="maintenance"
                if (!DoesTableMatchElement(node.Name, table))
                {
                    Console.WriteLine("\nERROR: The name of the element, preceding an underscore, must match the table attribute value \n\n" + "<" + node.Name + " table=\"" + table + "\"");
                    errorCount++;
                }

                RemoveSqlComments(node);

                if (HasRequiredFields(table, node) == false)
                {
                    errorCount++;
                }
            }


            //ensure that there are not any duplicate elements
            foreach (var elementName in elementNames.Where(x => !eleHash.Add(x)).ToList().Distinct())
            {
                Console.WriteLine("ERROR: You cannot have duplicate element " + "<" + elementName + ">");
                errorCount++;
            }

            //print random success message
            Random rnd = new Random();

            if (errorCount == 0)
            {
                Console.WriteLine("\n" + successMessage[rnd.Next(0, successMessage.Length)] +  " VALIDATION SUCCESSFUL!!!");
            }

            Console.WriteLine();
            reader.Dispose();

        }

        private bool DoesTableMatchElement(string elementName, string table)
        {
            //TODO: update to take into acount other tables like patient_payer and provider_order where elements are providerorder
            if (table == null)
                return false;

            return (elementName.IndexOf(table + "_") >= 0 || elementName == table);
        }

        private void RemoveSqlComments(XmlNode node)
        {
            var openBlock = "/*";
            var closeBlock = "*/";
            var newline = '\n';
            char dash = '-';
            bool inInlineComment = false;
            bool inBlockComment = false;
            bool hasInvalidComment = false;

            var sql = new StringBuilder();

            if (!node.InnerXml.Contains("--") && !node.InnerXml.Contains(openBlock))
                return;

            for(int i = 0; i < node.InnerXml.Length - 1; i++)
            {
                //inline comments
                if (node.InnerXml[i] == dash && node.InnerXml[i + 1] == dash)
                {
                    inInlineComment = true;
                    hasInvalidComment = true;
                }
                else if (node.InnerXml[i] == newline)
                {
                    inInlineComment = false;
                }

                //block comments
                if (node.InnerXml[i] == openBlock[0] && node.InnerXml[i + 1] == openBlock[1])
                {
                    inBlockComment = true;
                }
                else if (node.InnerXml[i] == closeBlock[0] && node.InnerXml[i + 1] == closeBlock[1])
                {
                    inBlockComment = false;
                }

                //add characters to new string
                if (!inInlineComment && !inBlockComment)
                {
                    sql.Append(node.InnerXml[i]);
                }
            }
            node.InnerXml = sql.ToString();

            if (hasInvalidComment)
                Console.WriteLine("\nERROR: " + "<" + node.Name + ">" + " contains inline commets \"--\". Please use block comments \"/*\"");
        }

        private bool HasRequiredFields(string tableName, XmlNode node)
        {
            if (tableName is null)
                return false;

            var source = node.Attributes?["source"]?.Value;
            var type = node.Attributes?["type"]?.Value;

            if (source != null || type == "command")
                return true;

            //TODO: update to take into account not using alias as. Check for the specific terms and certain cases, such as not inside parens, etc.
            var key = "as " + tableName + "_id";
            var create = "as create_timestamp";
            var modify = "as modify_timestamp";
            var delete = "as delete_ind";
            var errorCount = 0;
            var elementName = node.Name;

            if (node.InnerXml.Contains(key) == false)
            {
                Console.WriteLine("\nERROR: Missing alias \"" + key + "\" in " + "<" + elementName + ">");
                errorCount++;
            }
            if (node.InnerXml.Contains(create) == false)
            {
                Console.WriteLine("\nERROR: Missing alias \"" + create + "\" in " + "<" + elementName + ">");
                errorCount++;
            }
            if (node.InnerXml.Contains(modify) == false)
            {
                Console.WriteLine("\nERROR: Missing alias \"" + modify + "\" in " + "<" + elementName + ">");
                errorCount++;
            }

            if (node.InnerXml.Contains(delete) == false)
            {
                Console.WriteLine("\nERROR: Missing alias \"" + delete + "\" in " + "<" + elementName + ">");
                errorCount++;
            }

            return errorCount == 0;
        }

        private bool HasChildElements(XmlNode node)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                    return true;
            }
            return false;
        }
    }
}

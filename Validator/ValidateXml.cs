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

            };

            XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
            xmlReaderSettings.IgnoreComments = true;

            var doc = XDocument.Load(path);
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

                //verify that the query element name matches the table, and is seperated by an underscore <maintenance_example table="maintenance"
                if (table == null || (node.Name.IndexOf(table + "_") < 0 && node.Name != table))
                {
                    if (table != "patient_payer" && node.Name != "patientpayer" && !table.Contains("provider_order") && node.Name != "providerorder")
                    {
                        Console.WriteLine("\nERROR: The name of the element, preceding an underscore, must match the table attribute value \n\n" + "<" + node.Name + " table=\"" + table + "\"");
                        errorCount++;
                    }
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

            Random rnd = new Random();

            if (errorCount == 0)
            {
                Console.WriteLine("\n" + successMessage[rnd.Next(0, successMessage.Length)] +  " VALIDATION SUCCESSFUL!!!");
            }

            Console.WriteLine();
            reader.Dispose();

        }

        private void RemoveSqlComments(XmlNode node)
        {
            var openBlock = "/*";
            var closeBlock = "*/";
            var newline = '\n';
            char dash = '-';
            bool inInlineComment = false;
            bool inBlockComment = false;

            var sql = new StringBuilder();

            if (!node.InnerXml.Contains("--") && !node.InnerXml.Contains(openBlock))
                return;

            for(int i = 0; i < node.InnerXml.Length - 1; i++)
            {
                //inline comments
                if (node.InnerXml[i] == dash && node.InnerXml[i + 1] == dash)
                {
                    inInlineComment = true;
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
        }

        private bool HasRequiredFields(string tableName, XmlNode node)
        {
            if (tableName is null)
                return false;

            var source = node.Attributes?["source"]?.Value;
            var type = node.Attributes?["type"]?.Value;

            if (source != null || type == "command")
                return true;

            var key = "as " + tableName + "_id";
            var create = "as create_timestamp";
            var modify = "as modify_timestamp";
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

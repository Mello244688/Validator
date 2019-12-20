﻿using System;
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
        private readonly string[] successMessage = new string[]
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

        /*{table, element}*/
        private readonly Dictionary<string, string> validTableAndElement = new Dictionary<string, string>()
        {
            {"adjustment", "adjustment"}
            , {"allergy", "allergy" }
            , {"appointment", "appointment"}
            , {"balance", "balance"}
            , {"charge", "charge"}
            , {"chargediagnosis", "chargediagnosis"}
            , {"chargesummary", "chargesummary"}
            , {"claim", "claim"}
            , {"denial", "denial"}
            , {"diagnosis", "diagnosis"}
            , {"encounter", "encounter"}
            , {"encounterpayerxref", "encounterpayerxref"}
            , {"obepisode", "obepisode"}
            , {"oboutcome", "oboutcome"}
            , {"lab", "lab"}
            , {"maintenance", "maintenance"}
            , {"medication", "medication"}
            , {"immunization", "immunization"}
            , {"payer", "payer"}
            , {"patient", "patient"}
            , {"patient_payer", "patientpayer"}
            , {"payment", "payment"}
            , {"prescription", "prescription"}
            , {"provider", "provider"}
            , {"provider_order", "providerorder"}
            , {"vitals", "vitals"}
        };

        /*{table, id}*/
        private readonly Dictionary<string, string> nonStandardTableId = new Dictionary<string, string>()
        {
            {"encounterpayerxref", "encounter_payer_id" }
            , {"chargediagnosis", "charge_diagnosis_id" }
            , {"obepisode", "episode_id"}
            , {"oboutcome", "episode_id"}
        };

        public void ValidateQueries(string path)
        {

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
                var table = node.Attributes?["table"]?.Value;
                var type = node.Attributes?["type"]?.Value;

                // ignore text in root element
                if (node.NodeType == XmlNodeType.Text || type == "command")
                {
                    continue;
                }

                elementNames.Add(node.Name);
                //query cannot have nested queries
                if (HasChildElements(node))
                {
                    Console.WriteLine("\nERROR: " + "<" + node.Name + ">" + " cannot have child " + "<" + node.FirstChild.Name + ">");
                    errorCount++;
                }
                
                //verify that the table and type attributes are specified
                if (table == null || type == null)
                {
                    Console.WriteLine("\nERROR: missing attribute in " + "<" + node.Name + " table=\"" + table + "\" type=\"" + type + "\">");
                    errorCount++;
                }

                //verify the table name is valid
                if (table == null || !validTableAndElement.ContainsKey(table))
                {
                    Console.WriteLine("\nERROR: table=\"" + table + "\" is not a valid table. Refer to the Schema file for help") ;
                    errorCount++;
                }

                //verify that the query element name matches the table, and is seperated by an underscore <maintenance_example table="maintenance"
                if (!isElementNameValid(table, node.Name))
                {
                    Console.WriteLine("\nERROR: The name of the element, preceding an underscore, must match the table attribute value, and cannot contain another table name. Ex. <lab_patient> \n\n" + "<" + node.Name + " table=\"" + table + "\"");
                    errorCount++;
                }
                

                //remove sql comments from innerXML before validating fields
                RemoveSqlComments(node);

                //check for required fields
                if (!HasRequiredFields(table, node))
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

        private bool isElementNameValid(string table, string elementName)
        {
            //false if table is null or dictionary does not contain key
            if (table == null || !validTableAndElement.ContainsKey(table))
                return false;

            var tableUnderscore = validTableAndElement[table] + "_";

            // does not contain table_ , check if table is equal to element
            if (elementName.IndexOf(tableUnderscore) != 0)
                return validTableAndElement[table] == elementName;

            var restOfName = elementName.Substring(elementName.IndexOf(tableUnderscore) + tableUnderscore.Length);

            //check if everything after the underscore does not match a diffrent table
            return !(validTableAndElement.ContainsKey(restOfName) && restOfName != elementName);
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

            //don't check for required fields on centralized tags
            if (source != null)
                return true;

            var errorCount = 0;
            var elementName = node.Name;
            string key;

            List<string> requiredFields = new List<string>
            {
                "create_timestamp"
                , "modify_timestamp"
                , "center_id"
            };

            //provider_order table has "deleted_ind" and not "delete_ind" ...
            if (tableName == "provider_order")
            {
                requiredFields.Add("deleted_ind");
            }
            else
            {
                requiredFields.Add("delete_ind");
            }

            //define key: some keys do not follow standard naming conventions
            if (nonStandardTableId.ContainsKey(tableName))
            {
                key = nonStandardTableId[tableName];
            }
            else
            {
                key = tableName + "_id"; //the standard
            }

            requiredFields.Add(key);

            foreach (var field in requiredFields)
            {
                if (node.InnerXml.Contains(field) == false)
                {
                    Console.WriteLine("\nERROR: Missing alias \"" + field + "\" in " + "<" + elementName + ">");
                    errorCount++;
                }
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

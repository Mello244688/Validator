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
        /*{table, element}*/
        private readonly Dictionary<string, string> validTableAndElement = new Dictionary<string, string>()
        {
            {"adjustment", "adjustment"}
            , {"allergy", "allergy" }
            , {"assessment","assessment"}
            , {"appointment", "appointment"}
            , {"availableslots", "availableslots"}
            , {"balance", "balance"}
            , {"charge", "charge"}
            , {"chargediagnosis", "chargediagnosis"}
            , {"chargesummary", "chargesummary"}
            , {"claim", "claim"}
            , {"denial", "denial"}
            , {"diagnosis", "diagnosis"}
            , {"employee", "employee"}
            , {"encounter", "encounter"}
            , {"encounterpayerxref", "encounterpayerxref"}
            , {"hiepatient","hiepatient"}
            , { "hieencounter","hieencounter"}
            , { "hieencounterdiagnosis","hieencounterdiagnosis"}
            , { "hieencounterprovider","hieencounterprovider"}
            , {"hours", "hours"}
            , {"obepisode", "obepisode"}
            , {"order", "order"}
            , {"oboutcome", "oboutcome"}
            , {"lab", "lab"}
            , {"maintenance", "maintenance"}
            , {"medication", "medication"}
            , {"medicationlist", "medicationlist"}
            , {"immunization", "immunization"}
            , {"payer", "payer"}
            , {"patient", "patient"}
            , {"patientcohort", "patientcohort"}
            , {"patient_payer", "patientpayer"}
            , {"payment", "payment"}
            , {"prescription", "prescription"}
            , {"problem", "problem"}
            , {"provider", "provider"}
            , {"provider_order", "providerorder"}
            , {"result", "result"}
            , {"users", "users"}
            , {"vitals", "vitals"}
        };

        /*{table, id}*/
        private readonly Dictionary<string, string> nonStandardTableId = new Dictionary<string, string>()
        {
            {"encounterpayerxref", "encounter_payer_id" }
            , {"chargediagnosis", "charge_diagnosis_id" }
            , {"employee", "emp_id"}
            , {"hiepatient","patient_id"}
            , { "hieencounter","encounter_id"}
            , { "hieencounterdiagnosis","encounter_diagnosis_id"}
            , { "hieencounterprovider","provider_id"}
            , {"obepisode", "episode_id"}
            , {"oboutcome", "episode_id"}
            , {"patientcohort", "patient_cohort_id"}
        };

        public List<string> ValidateQueries(string path)
        {

            XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
            xmlReaderSettings.IgnoreComments = true;

            List<string> errorMessages = new List<string>();

            XDocument doc;

            try
            {
                doc = XDocument.Load(path);
            }
            catch (Exception ex)
            {
                errorMessages.Add("ERROR: " + ex.Message);
                return errorMessages;
            }
            
            var strDoc = doc.ToString().ToLower();

            XmlDocument xml = new XmlDocument();
            XmlReader reader = XmlReader.Create(new StringReader(strDoc), xmlReaderSettings);

            List<string> elementNames = new List<string>();
            HashSet<string> eleHash = new HashSet<string>();
            List<KeyTable> keyTable = new List<KeyTable>();

            try
            {
                xml.Load(reader);
            }
            catch (Exception ex)
            {
                errorMessages.Add("ERROR: " + ex.Message);
                return errorMessages;
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
                    errorMessages.Add("ERROR: " + "<" + node.Name + ">" + " cannot have child " + "<" + node.FirstChild.Name + ">");
                }
                
                //verify that the table and type attributes are specified
                if (table == null || type == null)
                {
                    errorMessages.Add("ERROR: missing attribute in " + "<" + node.Name + " table=\"" + table + "\" type=\"" + type + "\">");
                }

                //verify the table name is valid
                if (table == null || !validTableAndElement.ContainsKey(table))
                {
                    errorMessages.Add("ERROR: table=\"" + table + "\" is not a valid table. Refer to the Schema file for help") ;
                }

                //verify that the query element name matches the table, and is seperated by an underscore <maintenance_example table="maintenance"
                if (!isElementNameValid(table, node.Name))
                {
                    errorMessages.Add("ERROR: " + "<" + node.Name + " table=\"" + table + "\"" + " The name of the element, preceding an underscore, must match the table attribute value, and cannot contain another table name. Ex. <lab_patient>");
                }
                

                //remove sql comments from innerXML before validating fields
                RemoveSqlComments(node, errorMessages);

                //addKeyCodeToDict(table, node, keyTable, errorMessages);

                //check for required fields
                HasRequiredFields(table, node, errorMessages);
  
            }


            //ensure that there are not any duplicate elements
            foreach (var elementName in elementNames.Where(x => !eleHash.Add(x)).ToList().Distinct())
            {
                errorMessages.Add("ERROR: You cannot have duplicate element " + "<" + elementName + ">");
            }

            reader.Dispose();
            return errorMessages;
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

            //check if everything after the underscore does not match a different table
            // same table is ok
            if (restOfName == table)
            {
                return true;
            }
            return !validTableAndElement.ContainsKey(restOfName);
        }

        private void RemoveSqlComments(XmlNode node, List<string> errorMessages)
        {
            var openBlock = "/*";
            var closeBlock = "*/";
            var newline = '\n';
            char dash = '-';
            bool inInlineComment = false;
            bool inBlockComment = false;
            bool hasInvalidComment = false;
            bool inSingleQuotes = false;

            var sql = new StringBuilder();

            if (!node.InnerXml.Contains("--") && !node.InnerXml.Contains(openBlock))
                return;

            for(int i = 0; i < node.InnerXml.Length - 1; i++)
            {
                //some people like putting inline comments in the type field
                // need to keep track of when we are in quotes, as '--' will be valid
                if (node.InnerXml[i] == '\'' && !inSingleQuotes)
                {
                    inSingleQuotes = true;
                }
                else if (node.InnerXml[i] == '\'' && inSingleQuotes)
                {
                    inSingleQuotes = false;
                }

                //inline comments
                if (node.InnerXml[i] == dash && node.InnerXml[i + 1] == dash && !inBlockComment && !inSingleQuotes)
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
            {
                errorMessages.Add("ERROR: " + "<" + node.Name + ">" + " contains inline commets \"--\". Please use block comments \"/*\"");
            }
        }

        private void HasRequiredFields(string tableName, XmlNode node, List<string> errorMessages)
        {
            if (tableName is null || tableName == "availableslots")
            {
                return;
            }

            var source = node.Attributes?["source"]?.Value;
            var type = node.Attributes?["type"]?.Value;

            //don't check for required fields on centralized tags
            if (source != null)
                return;

            var elementName = node.Name;
            string key;

            List<string> requiredFields = new List<string>
            {
                "create_timestamp"
                , "modify_timestamp"
                //, "center_id"
            };

            if (tableName.IndexOf("hie") == 0)
            {
                requiredFields.Add("hie_id");
            }
            else
            {
                requiredFields.Add("center_id");
            }
            //provider_order table has "deleted_ind" and not "delete_ind" ...
            /*if (tableName == "provider_order")
            {
                requiredFields.Add("deleted_ind");
            }
            else
            {
                requiredFields.Add("delete_ind");
            }*/

            //get the table key
            key = getKey(tableName);

            requiredFields.Add(key);

            foreach (var field in requiredFields)
            {
                if (node.InnerXml.Contains(field) == false)
                {
                    errorMessages.Add("ERROR: Missing alias \"" + field + "\" in " + "<" + elementName + ">");
                }
            }
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

        string getKey(string tableName)
        {
            string key;

            if (nonStandardTableId.ContainsKey(tableName))
            {
                key = nonStandardTableId[tableName];
            }
            else
            {
                key = tableName + "_id"; //the standard
            }

            return key;
        }

        void addKeyCodeToDict(string tableName, XmlNode node, List<KeyTable> keyTable, List<string> errorMessages)
        {
            var start = 0;
            var end = node.InnerXml.IndexOf("as " + getKey(tableName)) - 1;

            Stack<char> delimiterCheck = new Stack<char>();

            char openingParen = '(';
            char closingParen = ')';
            char singleQuote = '\'';
            char space = ' ';

            bool inQuotes = false;
            bool canSave = true;

            string key = "";
            string trimKey = "";

            if (end > 0)
            {
                key = node.InnerXml.Substring(0, end);
            }

            for (int i = key.Length - 1; i > start; i--)
            {
                if (key[i] == closingParen)
                {
                    delimiterCheck.Push(node.InnerXml[i]);
                }
                else if (key[i] == openingParen)
                {
                    delimiterCheck.Pop();
                }
                else if (key[i] == singleQuote && !inQuotes)
                {
                    inQuotes = true;
                }
                else if (key[i] == singleQuote && inQuotes)
                {
                    inQuotes = false;
                }
                
                //if we are not in quotes it must be the end (start) of an alias, ex: alias.
                if(key[i] == '.' && !inQuotes)
                {
                    canSave = false;
                }
                //if we are parsing an alias, a comma or space would signal the start
                else if (!canSave && key[i] == space || key[i] == ',' || key[i] == openingParen)
                {
                    canSave = true;
                }

                //removng alias
                if (!canSave)
                {
                    key = key.Remove(i, 1);
                }

                if (delimiterCheck.Count == 0 && key[i] == ',')
                {
                    start = i; // end loop
                    key = key.Substring(start + 1, key.Length - (start +1)).Trim();
                    trimKey = key.Replace(" ", "");
                }
            }

            if (key.Length > 0)
            {
                KeyTable keyToAdd = new KeyTable();
                keyToAdd.Table = tableName;
                keyToAdd.Key = trimKey;
                if (keyTable.Contains(keyToAdd))
                {
                    errorMessages.Add("Duplicate key: " + key + " in " + "<" + node.Name + ">");
                }
                keyTable.Add(keyToAdd);
            }
        }
    }
}

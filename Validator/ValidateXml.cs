using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            , {"plan_member", "plan_member"}
            , {"plan_membercost", "plan_membercost"}
            , {"plan_plansite", "plan_plansite"}
            , {"plan_memberplansite", "plan_memberplansite"}
            , {"plan_membereligibility", "plan_membereligibility"}
            , {"plan_memberepisode", "plan_memberepisode"}
            , {"plan_site", "plan_site"}
            , {"plan_memberrisk", "plan_memberrisk"}
            , {"plan_membercaregap", "plan_membercaregap"}
            , {"planclaim", "planclaim"}
            , {"plan_claimline", "plan_claimline"}
            , {"plan_claimlinemodifier", "plan_claimlinemodifier"}
            , {"plan_rxclaim", "plan_rxclaim"}
            , {"plan_claimdiagnosis", "plan_claimdiagnosis"}
            , {"plan_claimprocedure", "plan_claimprocedure"}
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
            , {"plan_member", "plan_id"}
            , {"plan_membercost", "plan_id"}
            , {"plan_plansite", "plan_id"}
            , {"plan_memberplansite", "plan_id"}
            , {"plan_membereligibility", "plan_id"}
            , {"plan_memberepisode", "plan_id"}
            , {"plan_site", "plan_id"}
            , {"plan_memberrisk", "plan_id"}
            , {"plan_membercaregap", "plan_id"}
            , {"planclaim", "plan_id"}
            , {"plan_claimline", "plan_id"}
            , {"plan_claimlinemodifier", "plan_id"}
            , {"plan_rxclaim", "plan_id"}
            , {"plan_claimdiagnosis", "plan_id"}
            , {"plan_claimprocedure", "claim_procedure_id"}
        };

        public ErrorWarning ValidateQueries(string path)
        {

            XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
            xmlReaderSettings.IgnoreComments = true;

            ErrorWarning errorWarning = new ErrorWarning();

            XDocument doc;

            try
            {
                doc = XDocument.Load(path);
            }
            catch (Exception ex)
            {
                errorWarning.Errors.Add("ERROR: " + ex.Message);
                return errorWarning;
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
                errorWarning.Errors.Add("ERROR: " + ex.Message);
                return errorWarning;
            }

            //ignore XmlDeclaration
            if (xml.FirstChild.NodeType == XmlNodeType.XmlDeclaration)
            {
                xml.RemoveChild(xml.FirstChild);
            }

            XmlNode root = xml.FirstChild;

            if (CanDoAdditionalChecks(path))
            {
                AdditionalChecks(root, errorWarning, elementNames, eleHash);
            }

            reader.Dispose();
            return errorWarning;
        }

        void AdditionalChecks(XmlNode root, ErrorWarning errorWarning, List<string> elementNames, HashSet<string> eleHash)
        {
            foreach (XmlNode node in root.ChildNodes)
            {
                var table = node.Attributes?["table"]?.Value;
                var type = node.Attributes?["type"]?.Value;

                //a valid table should have a type="query"
                if (table != null && validTableAndElement.ContainsKey(table) && type == "command")
                {
                    errorWarning.Errors.Add("ERROR: " + "<" + node.Name + " table=\"" + table + "\"" + " type=\"command\">" + " A valid table should not have a command attribute, if you expect to return this data, use type=\"query\"");
                }

                // ignore text in root element and commands if the table is not valid
                if (node.NodeType == XmlNodeType.Text || type == "command" && !validTableAndElement.ContainsKey(table))
                {
                    continue;
                }

                elementNames.Add(node.Name);
                //query cannot have nested queries
                if (HasChildElements(node))
                {
                    errorWarning.Errors.Add("ERROR: " + "<" + node.Name + ">" + " cannot have child " + "<" + node.FirstChild.Name + ">");
                }

                //verify that the table and type attributes are specified
                if (table == null || type == null)
                {
                    errorWarning.Errors.Add("ERROR: missing attribute in " + "<" + node.Name + " table=\"" + table + "\" type=\"" + type + "\">");
                }

                //verify the table name is valid
                if (table == null || !validTableAndElement.ContainsKey(table))
                {
                    errorWarning.Errors.Add("ERROR: table=\"" + table + "\" is not a valid table. Refer to the Schema file for help");
                }

                //verify that the query element name matches the table, and is seperated by an underscore <maintenance_example table="maintenance"
                if (!isElementNameValid(table, node.Name))
                {
                    errorWarning.Errors.Add("ERROR: " + "<" + node.Name + " table=\"" + table + "\"" + " The name of the element, preceding an underscore, must match the table attribute value, and cannot contain another table name. Ex. <lab_patient>");
                }

                //remove sql comments from innerXML before validating fields
                RemoveComments(node, errorWarning);

                //check for required fields
                HasRequiredFields(table, node, errorWarning);
            }


            //ensure that there are not any duplicate elements
            foreach (var elementName in elementNames.Where(x => !eleHash.Add(x)).ToList().Distinct())
            {
                errorWarning.Errors.Add("ERROR: You cannot have duplicate element " + "<" + elementName + ">");
            }
        }

        private bool CanDoAdditionalChecks(string path)
        {

            // trying to minimize number of errors on a full run

            return !path.ToLower().Contains("cbha")
                && !path.ToLower().Contains("extract")
                && !path.ToLower().Contains("oldclient")
                && !path.ToLower().Contains("initial")
                && !path.ToLower().Contains("archive")
                && !path.ToLower().Contains("alert")
                && !path.ToLower().Contains("save")
                && !path.ToLower().Contains("migration")
                && !path.ToLower().Contains("review")
                && !path.ToLower().Contains("lab")
                && !path.ToLower().Contains("charge")
                && !path.ToLower().Contains("medication")
                && !path.ToLower().Contains("fix")
                && !path.ToLower().Contains("test")
                && !path.ToLower().Contains("golive")
                && !path.ToLower().Contains("config")
                && !path.ToLower().Contains("\\bp")
                && !path.ToLower().Contains("\\azr")
                && !Regex.IsMatch(path.ToLower(), "p[0-9]_")
                && !Regex.IsMatch(path.ToLower(), "p[0-9][0-9]_")
                && !Regex.IsMatch(path.ToLower(), "p[0-9]-");
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

        private void RemoveSqlComments(XmlNode node, ErrorWarning errorWarning)
        {
            var openBlock = "/*";
            var closeBlock = "*/";
            var newline = '\n';
            char dash = '-';
            bool inInlineComment = false;
            bool inBlockComment = false;
            bool hasInvalidComment = false;
            bool inSingleQuotes = false;

            if (!node.InnerXml.Contains(openBlock) && !node.InnerXml.Contains(dash))
                return;

            RegexOptions options = RegexOptions.None;
            Regex regex = new Regex("[ ]{2,}", options);
            node.InnerXml = regex.Replace(node.InnerXml, " ");

            var sql = new StringBuilder(node.InnerXml.Length);

            if (!node.InnerXml.Contains("--") && !node.InnerXml.Contains(openBlock))
                return;

            for(int i = 0; i < node.InnerXml.Length - 1; i++)
            {
                //some people like putting inline comments in the type field
                // need to keep track of when we are in quotes, as '--' will be valid
                if (node.InnerXml[i] == '\'' && !inSingleQuotes && !inBlockComment)
                {
                    inSingleQuotes = true;
                }
                else if (node.InnerXml[i] == '\'' && inSingleQuotes && !inBlockComment)
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
                if (node.InnerXml[i] == openBlock[0] && node.InnerXml[i + 1] == openBlock[1] && !inSingleQuotes)
                {
                    inBlockComment = true;
                }
                else if (node.InnerXml[i] == closeBlock[0] && node.InnerXml[i + 1] == closeBlock[1] && !inSingleQuotes)
                {
                    inBlockComment = false;
                    i++; /*after finding closing block we want to continue after the block*/
                    continue;

                }

                //add characters to new string
                if (!inInlineComment && !inBlockComment)
                {
                    if (!node.InnerXml.Substring(i).Contains(openBlock) && !node.InnerXml.Substring(i).Contains(dash))
                    {
                        sql.Append(node.InnerXml.Substring(i));
                    }
                    else
                    {
                        sql.Append(node.InnerXml[i]);
                    }
                }

            }
            node.InnerXml = sql.ToString();

            if (hasInvalidComment)
            {
                errorWarning.Errors.Add("ERROR: " + "<" + node.Name + ">" + " contains inline commets \"--\". Please use block comments \"/*\"");
            }
        }

        private void RemoveComments(XmlNode node, ErrorWarning errorWarning)
        {
            List<int> allSingleQuotes = node.InnerText.FindAllIndexof("'");
            List<int> allOpeningBlockQuotes = node.InnerText.FindAllIndexof("/*");
            List<int> allClosingBlockQuotes = node.InnerText.FindAllIndexof("*/");
            List<int> allDashQuotes = node.InnerText.FindAllIndexof("--");

            string sql = node.InnerText;

            /*if there are no comments return or there are not an equal number of opening and closing*/
            if (allOpeningBlockQuotes.Count == 0 && allClosingBlockQuotes.Count == 0 || allOpeningBlockQuotes.Count != allClosingBlockQuotes.Count)
                return;

            if (allClosingBlockQuotes[0] < allOpeningBlockQuotes[0])
                return; //should probably error

            List<int> blockRange = GetBlockCommentRange(allOpeningBlockQuotes, allClosingBlockQuotes);

            for (int i = blockRange.Count- 1; i - 1 >= 0; i-=2)
            {
                int length = blockRange[i] - blockRange[i - 1] + 2;
                sql = sql.Remove(blockRange[i - 1], length);
            }

            node.InnerText = sql;

            //TODO: check for dash -- comments, need to take into account they may be in block comments or single quotes

        }

        private void HasRequiredFields(string tableName, XmlNode node, ErrorWarning errorWarning)
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
                    errorWarning.Errors.Add("ERROR: Missing alias \"" + field + "\" in " + "<" + elementName + ">");
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

        List<int> GetBlockCommentRange(List<int> open, List<int> close)
        {
            if (open.Count != close.Count)
                return null;

            List<int> allBlocks = new List<int>();
            List<int> result = new List<int>();

            for(int i = 0; i < open.Count; i++)
            {
                allBlocks.Add(open[i]);
                allBlocks.Add(close[i]);
            }

            result.Add(allBlocks[0]);
            for(int i = 0; i + 2 < allBlocks.Count; i+=2)
            {
                //in order: can keep (not nested comments)
                if (allBlocks[i + 1] < allBlocks[i + 2])
                {
                    result.Add(allBlocks[i + 1]);
                    result.Add(allBlocks[i + 2]);
                }

                //at the end of nested comments, can add to list
                if (i + 4 == allBlocks.Count)
                {
                    result.Add(allBlocks[i + 3]);
                }
            }
            return result;
        }
        void addKeyCodeToDict(string tableName, XmlNode node, List<KeyTable> keyTable, ErrorWarning errorWarning)
        {
            var start = 0;
            var end = node.InnerXml.LastIndexOf("as " + getKey(tableName)) - 1;

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
                key = node.InnerXml.Substring(0, end + 1);
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
                /*KeyTable keyToAdd = new KeyTable();
                keyToAdd.Table = tableName;
                keyToAdd.Key = trimKey;
                if (keyTable.Contains(keyToAdd))
                {
                    errorMessages.Add("Duplicate key: " + key + " in " + "<" + node.Name + ">");
                }
                keyTable.Add(keyToAdd);*/
                if (key.Contains("date") || key.Contains("time"))
                    errorWarning.Warnings.Add("WARNING: Primary Key \"" + key + "\" in " + "<" + node.Name + ">" + " includes a date field");
            }
        }
    }
}

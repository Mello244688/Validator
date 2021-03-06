﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            , {"eventlog", "eventlog"}
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
                if (node.NodeType == XmlNodeType.Text || type == "command" && (table == null || !validTableAndElement.ContainsKey(table)))
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

                if (node.Name == "immunizationall")
                {
                    Console.WriteLine();
                }
                //verify the table name is valid
                if (table == null || !validTableAndElement.ContainsKey(table))
                {
                    errorWarning.Errors.Add("ERROR: table=\"" + table + "\" is not a valid table. Refer to the Schema file for help");
                }

                //verify that the query element name matches the table, and is seperated by an underscore <maintenance_example table="maintenance"
                isElementNameValid(table, node.Name, errorWarning);

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

            // restrict the files and directories we want to do additional checks on

            return !path.ToLower().Contains("config")
                && !path.ToLower().Contains("test")
                && !path.ToLower().Contains("\\bp")
                && !path.ToLower().Contains("\\azr")
                && !Regex.IsMatch(path.ToLower(), "p[0-9]_")
                && !Regex.IsMatch(path.ToLower(), "p[0-9][0-9]_")
                && !Regex.IsMatch(path.ToLower(), "p[0-9]-")
                && File.GetLastWriteTime(path) >= DateTime.Now.AddYears(-1); //file has been modified in the past 1 years
        }

        private void isElementNameValid(string table, string elementName, ErrorWarning errorWarning)
        {
            //false if table is null or dictionary does not contain key, don't run checks
            if (table == null || !validTableAndElement.ContainsKey(table))
                return;

            var tableUnderscore = validTableAndElement[table] + "_";

            // does not contain table_ , check if table is equal to element
            if (elementName.IndexOf(tableUnderscore) != 0)
            {
                if (validTableAndElement[table] != elementName)
                {
                    errorWarning.Errors.Add("ERROR: " + "<" + elementName + "> needs to correspond to table=\"" + table + "\", and be seperated by an underscore for additional text. EX: <" + table + "_text>");
                }
                else
                {
                    return; //table equals element so return
                }
            }

            List<string> elementSplit = elementName.Split('_').ToList();

            var allElements = validTableAndElement.Values;
            List<string> extraElementsInTag = elementSplit.Intersect(allElements).Distinct().ToList();
            extraElementsInTag.Remove(validTableAndElement[table]);

            if (extraElementsInTag.Count > 0)
            {
                string error = "ERROR: " + "<" + elementName + " table=\"" + table + "\"" + " Cannot have more than one table name in element tag. Additional tables in tag: ";

                foreach (var ele in extraElementsInTag)
                {
                    if (ele.Equals(extraElementsInTag.Last()))
                    {
                        error += ele;
                    }
                    else
                    {
                        error += ele + ", ";
                    }
                }
                errorWarning.Errors.Add(error);
            }
        }

        private void RemoveComments(XmlNode node, ErrorWarning errorWarning)
        {
            List<int> allOpeningBlockQuotes = node.InnerText.FindAllIndexof("/*");
            List<int> allClosingBlockQuotes = node.InnerText.FindAllIndexof("*/");

            string sql = node.InnerText;

            /*return if there are no comments or there are not an equal number of opening and closing*/
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
            };

            if (tableName.IndexOf("hie") == 0)
            {
                requiredFields.Add("hie_id");
            }
            else
            {
                requiredFields.Add("center_id");
            }

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
    }
}

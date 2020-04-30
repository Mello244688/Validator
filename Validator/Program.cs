using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Validator
{
    class Program
    {
        static void Main(string[] args)
        {
            ValidateXml validate = new ValidateXml();
            ErrorWarning res;
            List<ErrorWarning> errorWarnings = new List<ErrorWarning>();
            Random rnd = new Random();

            string[] successMessage = new string[]
            {
                "YOU'VE GONE AND DONE IT!"
                , "YOU ARE ON A ROLL MEOW!"
                , "LOOK AT ALL YOU'VE ACCOMPLISHED!"
                , "GO GET THAT 5 STAR REVIEW!"
                , "YOU'RE THE PRIDE AND JOY OF AZARA!"
                , "HOW DO YOU KEEP ALL THAT BRAIN FROM SPILLING OUT!?"
                , "YOU'RE THE WHOLE PACKAGE, BEAUTIFUL, SMART, FUNNY, AND YOU CAN FOLLOW CODING STANDARDS!"
                , "KNOCK KNOCK!! WHO'S THERE? A SUPER STAAAHHHHH!!"
                , "YOU'RE SMARTER THAN GOOGLE AND MARY POPPINS COMBINED!!"
                , "I KNOW THIS IS CORNY, BUT YOU ARE A-MAIZING-ING!"
                , "YOU'RE DOING SUCH A GREAT JOB, YOU SHOULD TAKE THE REST OF THE DAY OFF!!"
            };

            FileAttributes attr = File.GetAttributes(args[0]);

            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                string path = args[0];
                string[] files;
                try
                {
                    files = Directory.GetFiles(path, "*.xml", SearchOption.AllDirectories).ToArray();
                }
                catch (Exception)
                {
                    return;
                }

                foreach (var file in files)
                {
                    res = validate.ValidateQueries(file);

                    if (res.Errors.Count > 0 || res.Warnings.Count > 0)
                    {
                        res.Filename = file;
                        errorWarnings.Add(res);
                    }
                }

                if (errorWarnings.Count > 0)
                {
                    GenerateReport(errorWarnings, path);
                }
            }
            else //path
            {
                res = validate.ValidateQueries(args[0]);

                if (res.Errors.Count > 0)
                {
                    foreach (var err in res.Errors)
                    {
                        Console.WriteLine(err);
                    }
                }
                else
                {
                    Console.WriteLine("\n" + successMessage[rnd.Next(0, successMessage.Length)] + " VALIDATION SUCCESSFUL!!!");
                }
                return;
            }
        }
        static void GenerateReport(List<ErrorWarning> errorWarnings, string clientPath)
        {
            string gReportHeader = @"<html>
            <head>
            <style>
            table, th, td {
                border: 1px solid black;
                border-collapse: collapse;
                font-family: Arial, Verdana, sans-serif;
                font-size: 12px;
            }

            th, td {
                padding: 5px;
            }

            th {
                text-align: left;
            }
            </style>
            </head>
            <h2>Validation Results for ###Date###</h2>
            <table>
            <tr>
                <!-- <th>Error</th> -->
                <th>File</th>
                <th>Description</th>
            </tr>";

            string gReportFooter = @"</tr></table></html>";

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"Results.html"))
            {
                string header = gReportHeader;
                string curTime = DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt");

                header = header.Replace(@"###Date###", curTime);

                file.WriteLine(header);

                
                foreach (var errorWarning in errorWarnings)
                {
                    foreach (var errorInfo in errorWarning.Errors)
                    {
                        string errorMessage = errorInfo
                            .Replace(">", "&gt;")
                            .Replace("<", "&lt;")
                            .Replace("ERROR: ", "");

                        file.WriteLine("<tr>");
                       // file.WriteLine("<td>" + errorInfo.errorCode + "</td>");
                        file.WriteLine("<td>" + errorWarning.Filename.Replace(clientPath, "") + "</td>");
                        file.WriteLine("<td>" + errorMessage + "</td>");
                        file.WriteLine("</tr>");
                    }
                }

                file.WriteLine(gReportFooter);

            }

        }
    }
}

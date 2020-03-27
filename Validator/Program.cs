using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Validator
{
    class Program
    {
        static void Main(string[] args)
        {
            ValidateXml validate = new ValidateXml();
            ErrorWarning res;
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
            
            if (args.Length == 1)
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
            
            if (args.Length == 2 && args[1] == "all")
            {
                string path = args[0];
                string[] files;
                try
                {
                    files = Directory.GetFiles(path, "*queries.xml", SearchOption.AllDirectories)
                    .Where(d => !d.ToLower().Contains("adhoc")
                        && !d.ToLower().Contains("ad-hoc")
                        && !d.ToLower().Contains("cbha")
                        && !d.ToLower().Contains("extract")
                        && !d.ToLower().Contains("oldclient")
                        && !d.ToLower().Contains("initial")
                        && !d.ToLower().Contains("archive")
                        && !d.ToLower().Contains("alert")
                        && !d.ToLower().Contains("save")
                        && !d.ToLower().Contains("migration")
                        && !d.ToLower().Contains("review")
                        && !d.ToLower().Contains("lab")
                        && !d.ToLower().Contains("charge")
                        && !d.ToLower().Contains("medication")
                        && !d.ToLower().Contains("ahs")
                        && !d.ToLower().Contains("ccp")
                        && !d.ToLower().Contains("fix")
                        && !d.ToLower().Contains("test")
                        && !d.ToLower().Contains("golive")
                        && !d.ToLower().Contains("\\bp")
                        && !d.ToLower().Contains("\\azr")
                        && !Regex.IsMatch(d.ToLower(), "p[0-9]_")
                        && !Regex.IsMatch(d.ToLower(), "p[0-9][0-9]_")
                        && !Regex.IsMatch(d.ToLower(), "p[0-9]-")).ToArray();
                }
                catch (Exception)
                {
                    return;
                }

                using (StreamWriter sw = new StreamWriter(@"..\..\..\..\validatorErrors.txt"))
                {
                    foreach (var file in files)
                    {
                        res = validate.ValidateQueries(file);

                        if (res.Errors.Count > 0 || res.Warnings.Count > 0)
                        {
                            sw.WriteLine("\n*************************************************************************************");
                            sw.WriteLine(file.Replace(path, "") + " Errors: {0} Warnings {1}", res.Errors.Count, res.Warnings.Count);
                            sw.WriteLine("*************************************************************************************\n");
                            foreach (string s in res.Errors)
                            {
                                sw.WriteLine(s);
                            }

                            if (res.Warnings.Count > 0)
                            {
                                sw.WriteLine();
                                foreach (var s in res.Warnings)
                                {
                                    sw.WriteLine(s);
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine(file.Replace(path, "") + ": PASSED");
                        }
                    }
                }
            }          
        }
    }
}

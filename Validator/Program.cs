using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Validator
{
    class Program
    {
        static void Main(string[] args)
        {
            ValidateXml validate = new ValidateXml();
            List<string> res;
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
            };

            if (args.Length > 0)
            {
                res = validate.ValidateQueries(args[0]);

                if (res.Count > 0)
                {
                    foreach (var err in res)
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

            string path = "C:\\Users\\scott\\DRVS-Clients";
            string[] files;
            try
            {
                files = Directory.GetFiles(path, "*queries*.xml", SearchOption.AllDirectories)
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

            using (StreamWriter sw = new StreamWriter("validatorErrors.txt"))
            {
                foreach (var file in files)
                {
                    res = validate.ValidateQueries(file);
                    
                    if (res.Count > 0)
                    {
                        Console.WriteLine(file.Replace(path, "") + ": FAILED");

                        sw.WriteLine("\n**********************************************");
                        sw.WriteLine(file.Replace(path, ""));
                        sw.WriteLine("**********************************************\n");
                        foreach (string s in res)
                        {
                            sw.WriteLine(s);
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

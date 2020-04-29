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

            var test = "C:/users/scott/drvs-clients";

            FileAttributes attr = File.GetAttributes(test);

            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                string path = "C:/users/scott/drvs-clients";
                string[] files;
                try
                {
                    files = Directory.GetFiles(path, "*.xml", SearchOption.AllDirectories).ToArray();
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
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Validator
{
    class Program
    {
        static void Main(string[] args)
        {
            ValidateXml validate = new ValidateXml();

            if (args != null)
                validate.ValidateQueries(args[0]);
            
            Console.WriteLine("Press any key to close...");
            Console.ReadLine();
        }
    }
}

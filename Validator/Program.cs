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

            var test = "C:/Users/Scott/Documents/xmlTest.xml";
            var test2 = "C:/Users/Scott/DRVS-Clients/mlchc/1_Lynn/queries.xml";
            var test3 = "C:/Users/Scott/DRVS-Clients/mlchc/7_Lowell/queries.xml";

            validate.ValidateQueries(test2);
        }
    }
}

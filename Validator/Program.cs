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

            if (args.Length > 0)
                validate.ValidateQueries(args[0]);
        }
    }
}

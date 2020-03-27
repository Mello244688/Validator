using System.Collections.Generic;

namespace Validator
{
    class ErrorWarning
    {
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }

        public ErrorWarning()
        {
            Errors = new List<string>();
            Warnings = new List<string>();
        }
    }
}

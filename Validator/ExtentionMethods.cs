using System.Collections.Generic;

namespace Validator
{
    public static class ExtentionMethods
    {
        public static List<int> FindAllIndexof(this string values, string val)
        {
            int lastFind = values.IndexOf(val);
            List<int> indexes = new List<int>();

            while (lastFind  > -1)
            {
                indexes.Add(lastFind);
                lastFind = values.IndexOf(val, lastFind + 1);
            }

            return indexes;
        }
    }
}

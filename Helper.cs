using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace PyRZyBot
{
    public class Helper
    {
        public static string DotDotDotPattern = @"^(\.)+$";

        public static string Ending(int x)
        {
            switch (x)
            {
                case 1:

                    return "Pyrę";

                case 2:
                case 3:
                case 4:

                    return "Pyry";

                default:

                    return "pyr";
            }
        }
        public static bool IsUsername(string username)
        {
            string pattern = @"^[a-zA-Z0-9]{3,24}";
            Regex regex = new Regex(pattern);
            return regex.IsMatch(username);
        }

        public static bool IsNumeric(string number)
        {
            return int.TryParse(number, out _);
        }

    }
}

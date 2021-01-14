using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace PyRZyBot
{
    public class Helper
    {
        public static string DotDotDotPattern = @"^(\.)+$";

        public static int FightCooldown = 20;

        public static bool IsNumeric(string number)
        {
            return int.TryParse(number, out _);
        }
        public static string EndingPyry(int x)
        {
            if (x == 1)
                return "pyrę";

            if (x % 100 > 10 && x % 100 < 20)
                return "pyr";

            switch (x % 10)
            {
                case 2:
                case 3:
                case 4:
                    return "pyry";

                default:
                    return "pyr";
            }
        }
        public static string EndingOther(string IdName, string x)
        {
            if (IdName == "ananieana" || IdName == "thyvir" || IdName == "domilano" || IdName == "rinah24")
                switch (x)
                {
                    case "zapisany":
                        return "zapisana";

                    case "wyzwał":
                        return "wyzwała";

                    case "wyzwany":
                        return "wyzwana";

                    case "byłeś":
                        return "byłaś";

                    case "wygrał":
                        return "wygrała";

                    case "anulował":
                        return "anulowała";

                    case "podjął":
                        return "podjęła";

                    case "dał":
                        return "dała";

                    case "przegrał":
                        return "przegrała";

                    case "odebrał":
                        return "odebrała";
                    
                    case "wyzwałeś":
                        return "wyzwałaś";
                }
            return x;
        }
    }
}

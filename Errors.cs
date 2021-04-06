using System;

namespace PyRZyBot_2._0
{
    class Errors
    {
        public static void UserNotFound(int Line, string Class, string Name, string Channel)
        {
            if (string.IsNullOrEmpty(Channel))
            {
                ErrorMessage($"{Class}|{Line} Nie znaleziono użytkownika {Name} we wspólnej bazie");
            }
            else { ErrorMessage($"{Class}|{Line} Nie znaleziono użytkownika {Name} w bazie kanału {Channel}"); }
        }
        public static void ElementNotFound(int Line, string Class, string Element, string Channel)
        {
            ErrorMessage($"{Class}|{Line} Nie znaleziono elementu {Element} w bazie kanału {Channel}");
        }
        public static void UnexpectedInput(int Line, string Class, string Name, string Channel)
        {
            if (string.IsNullOrEmpty(Channel))
            {
                ErrorMessage($"{Class}|{Line} Niespodziewane dane wejściowe dla użytkownika {Name} ze wspólnej bazy");
            }
            else { ErrorMessage($"{Class}|{Line} Niespodziewane dane wejściowe dla użytkownika {Name} z bazy kanału {Channel}"); }
        }
        public static void UnaccountedForInput(int Line, string Class, string Argument)
        {
            ErrorMessage($"{Class}|{Line} Brakująca opcja dla argumentu: {Argument}");
        }
        static void ErrorMessage(string Error)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"\nERROR> {Error} \n");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
/*                     Errors.UserNotFound((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                        MethodBase.GetCurrentMethod().DeclaringType.Name, Name, Channel);
                    Bot.SendMessage(Channel, 1, false, $"Wystąpił niespodziewany błąd!");
                    return;

*/
using System.Collections.Generic;
using System.Linq;
using System;
using System.Timers;
using PyRZyBot_2._0.Entities;
using System.Runtime.Loader;

namespace PyRZyBot_2._0
{
    class Program
    {
        internal static Bot PyRZyBot = new Bot();
        static Timer FailSafe;

        static void Main(string[] args)
        {
            string Input = "";
            string Connected = "";

            AssemblyLoadContext.Default.Unloading += Default_Unloading; ;

            PyRZyBot.Connect();

            while (Input.ToLower() != "shut down")
            {
                if (!string.IsNullOrEmpty(Input))
                    ConsoleCommands.Commands(ref Input, ref Connected);

                if (!string.IsNullOrEmpty(Input))
                {
                    if (!string.IsNullOrEmpty(Connected))
                    {
                        PyRZyBot.ConsoleCommands(Connected, "PyRZyBot", Input);
                    }
                    else { StatusMessage("Nie połączono konsoli z czatem!"); }
                }
                Input = Console.ReadLine();
            }
        }

        public static string TimeNow()
        {
            DateTime TimeNow = DateTime.Now;
            string Time = "";

            if (TimeNow.Hour < 10)
                Time += 0;

            Time += $"{TimeNow.Hour}:";

            if (TimeNow.Minute < 10)
                Time += 0;

            Time += $"{TimeNow.Minute}:";

            if (TimeNow.Second < 10)
                Time += 0;

            Time += TimeNow.Second;

            return Time;
        }
        public static void StatusMessage(string Message)
        {
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.Write($"{TimeNow()} > {Message}");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine();
        }
        public static void StartFailSafe()
        {
            FailSafe = new Timer(15000);
            FailSafe.Start();
            FailSafe.AutoReset = false;
            FailSafe.Elapsed += CheckConnection;
        }
        public static void FailSafeDispose()
        {
            if (FailSafe != null)
            {
                FailSafe.Stop();
                FailSafe.Dispose();
            }
        }
        private static void CheckConnection(object sender, ElapsedEventArgs e)
        {
            if (!Bot.IsConnected)
            {
                Program.StatusMessage("Nie udało się połączyć z czatem! Ponawianie!\n");
                PyRZyBot.Disconnect();
                StartFailSafe();
                PyRZyBot.Connect();
            }
        }
        private static void Default_Unloading(AssemblyLoadContext obj)
        {
            PyRZyBot.Disconnect();
        }
    }

    class ConsoleCommands
    {
        public static void Commands(ref string Input, ref string Connected)
        {
            List<string> Arguments = Input.ToLower().Split(" ").ToList();
            var Channels = new List<string> { "kyrzy", "ananieana" };

            switch (Arguments[0])
            {
                case "connect":
                    Connect(ref Connected, Arguments);
                    break;

                case "disconnect":
                    Disconnect(ref Connected);
                    break;

                case "restart":
                    Restart();
                    break;

                case "feedback":
                    Feedback(ref Arguments);
                    break;

                default:
                    return;
            }
            Input = "";
        }

        static void Connect(ref string Connected, List<string> Arguments)
        {
            if (Arguments.Count != 2)
            {
                Program.StatusMessage("Niewłaściwa liczba argumentów");
                return;
            }
            if (!Bot.Channels.Contains(Arguments[1]))
            {
                Program.StatusMessage($"Nieznany kanał");
                return;
            }
            Program.StatusMessage($"Połączono z czatem: {Arguments[1]}");
            Connected = Arguments[1];
        }
        static void Disconnect(ref string Connected)
        {
            if (!string.IsNullOrEmpty(Connected))
            {
                Program.StatusMessage("Nie połączono konsoli z czatem");
                return;
            }
            Program.StatusMessage($"Rozłączono z czatem: {Connected}");
            Connected = "";
        }
        static void Restart()
        {
            Program.StatusMessage("Restartowanie\n");
            Program.PyRZyBot.Disconnect();
            Program.PyRZyBot.Connect();
        }

        private static void Feedback(ref List<string> Arguments)
        {
            if (Arguments.Count != 2)
            {
                Program.StatusMessage("Niewłaściwa liczba argumentów");
                return;
            }
            if (!Enums.IsFeedbackLevelDefined(Arguments[1]))
            {
                Program.StatusMessage("Niewłaściwy poziom feedbacku");
                return;
            }
            int FeedbackLevel = (int)Enum.Parse(typeof(FeedbackLevels), Arguments[1].ToLower());
            using (var context = new Database())
            {
                var CurrentFeedbackLevel = context.ChannelInfo.FirstOrDefault(x => x.Channel == "pyrzybot" && x.Info == "ConsoleFeedback");
                CurrentFeedbackLevel.Value = FeedbackLevel.ToString();
                context.Update(CurrentFeedbackLevel);
                context.SaveChanges();
                Program.StatusMessage($"Zmieniono poziom feedbacku na {Enum.GetName(typeof(FeedbackLevels), FeedbackLevel)}");
            }
        }
    }
}

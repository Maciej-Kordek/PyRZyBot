using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PyRZyBot_2._0.Entities
{
    public class Quotes
    {
        public Quotes() { }
        public Quotes(string quote, string channel)
        {
            Quote = quote;
            Channel = channel;
        }

        public int Id { get; set; }
        public string Quote { get; set; }
        public string Channel { get; set; }

        public static void QuotesMenu(string Channel, string Name, List<string> Arguments)
        {
            if (Arguments.Count == 1)
            {
                DisplayRandomQuote(Channel);
                return;
            }

            switch (Arguments[1].ToLower())
            {
                case "add":
                    QuoteAdd(Channel, Name, Arguments);
                    break;

                case "delete":

                    break;

                case "edit":

                    break;
            }
        }

        static void DisplayRandomQuote(string Channel)
        {
            using (var context = new Database())
            {
                var ChannelQuotes = context.Quotes.ToList();
                var Random = new Random();
                var RandomQuote = ChannelQuotes[Random.Next(ChannelQuotes.Count)];

                Bot.SendMessage(Channel, 2, true, RandomQuote.Quote);
            }
        }

        static void QuoteAdd(string Channel, string Name, List<string> Arguments)
        {
            if (Database.GetAccessLevel(Channel, Name) < (int)AccessLevels.mod)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !command add użytkownikowi {Name} (Brak uprawnień)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz odpowiednich uprawnień");
                return;
            }
            if (Arguments.Count < 3)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !command add użytkownikowi {Name} (Niewłaściwa liczba argumentów)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Podano niewłaściwą liczbę argumentów (!command add <wywołanie> <odpowiedź>)");
                return;
            }
            using (var context = new Database())
            {
                var StringBuilder = new StringBuilder($"{Arguments[2]}");
                for (int i = 3; i < Arguments.Count; i++)
                    StringBuilder.Append($" {Arguments[i]}");

                string Quote = StringBuilder.ToString();

                Quotes NewQuote = new Quotes(Quote, Channel);
                context.Add(NewQuote);
                context.SaveChanges();
                Bot.SendMessage(Channel, 2, true, $"@{Name}, Cytat #{NewQuote.Id} został dodany");
            }
        }
    }
}

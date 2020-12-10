using System;
using System.Collections.Generic;
using System.Text;

namespace PyRZyBot
{
    public class ChatUser
    {
        public string IdName { get; set; }
        public string DisplayedName { get; set; }
        public string NickName { get; set; }
        public int Pyry { get; set; }
        public int Stawka { get; set; }
        public string Wyzwania { get; set; }
        public int tldr { get; set; }
        public int CzySpam { get; set; }
        public ChatUser(string idName, string displayedName)
        {
            IdName = idName;
            DisplayedName = displayedName;

            Pyry = 0;
            Stawka = 0;
            Wyzwania = "";

            tldr = 0;
            CzySpam = 0;
        }
    }
}

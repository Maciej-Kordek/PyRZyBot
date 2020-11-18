using System;

namespace PyRZyBot
{
    class Program
    {
        static void Main(string[] args)
        {
            Bot MedBot = new Bot();

            MedBot.Connect(true);

            Console.ReadLine();

            MedBot.Disconnect();
        }
    }
}

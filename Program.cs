using System;

namespace PyRZyBot
{
    class Program
    {
        static void Main(string[] args)
        {
            Bot PyRZyBot = new Bot();

            PyRZyBot.Connect(true);

            Console.ReadLine();

            PyRZyBot.Disconnect();
        }
    }
}

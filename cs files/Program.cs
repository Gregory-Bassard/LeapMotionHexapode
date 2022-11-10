using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Leap;
using System.IO.Ports;
using System.Threading;
using System.IO.Packaging;
using System.Diagnostics;

namespace LeapMotionDataSender

{
    internal class Program
    {
        public const bool SP_TRANSMISSION = false;
        public static int MYDEBUG = -1; //-1 => off | 0 => on | 1 => LEAP | 2 => HEXAPODE
        public static bool RESTARTING = false;
        public const bool HEXAPODE = true;
        public const int HEXAPODE_TRANSLATION_METHODE = 2; //1 & 2
        private static int textWaitingTime = 10;
        private static string line = "";
        private static string serialPort = "COM6";
        private static int serialSpeed = 115200;
        private object controller;
        private static Random rnd = new Random();
        private static string admin = "admin";
        public static ControllerListener listener = new ControllerListener();

        public static void Main(string[] args)
        {
            IntroText();
            if (line == "admin")
                admin = "admin";

            WriteText("Press \"R\" to restart Hexapode position & program or \"Enter\" to exit... >", 100, admin);
            Init();
            line = Console.ReadLine();
            while (line.ToUpper() == "R")
            {
                WriteText("Restarting Hexapode position & program... >", 100, admin);
                Thread.Sleep(100);
                Console.WriteLine();
                listener.hexapode.connect();
                Hexapode newHexapode = new Hexapode();
                newHexapode.Init();
                for (int i = 0; i < 10; i++)
                {
                    newHexapode.Update(0, 0, 0, 0, 0, 0, true);
                    Thread.Sleep(200);
                }
                listener.hexapode = newHexapode;
                WriteText("Hexapode position & program restarted... >", 100, admin);
                Thread.Sleep(250);
                Console.Clear();
                MyWriteLine("\r\n\r\n  ____  _   _ _   _ _   _ ___ _   _  ____   ____  ____   ___   ____ ____      _    __  __ \r\n |  _ \\| | | | \\ | | \\ | |_ _| \\ | |/ ___| |  _ \\|  _ \\ / _ \\ / ___|  _ \\    / \\  |  \\/  |\r\n | |_) | | | |  \\| |  \\| || ||  \\| | |  _  | |_) | |_) | | | | |  _| |_) |  / _ \\ | |\\/| |\r\n |  _ <| |_| | |\\  | |\\  || || |\\  | |_| | |  __/|  _ <| |_| | |_| |  _ <  / ___ \\| |  | |\r\n |_| \\_\\\\___/|_| \\_|_| \\_|___|_| \\_|\\____| |_|   |_| \\_\\\\___/ \\____|_| \\_\\/_/   \\_\\_|  |_|\r\n                                                                                          \r\n\r\n", 500);
                WriteText("Press \"R\" to restart Hexapode position & program or \"Enter\" to exit... >", 75, admin);
                line = Console.ReadLine();
            }
            ProgramExit();
        }
        private static void IntroText()
        {
            Console.Clear();

            HexapodeTitle(textWaitingTime);
            WriteText("Press Enter to start program >", 100, admin);
            line = Console.ReadLine();

            if (line == "admin")
            {
                MYDEBUG = 2;
                Console.Clear();
                MyWriteLine("\r\n\r\n  ____  _   _ _   _ _   _ ___ _   _  ____   ____  ____   ___   ____ ____      _    __  __ \r\n |  _ \\| | | | \\ | | \\ | |_ _| \\ | |/ ___| |  _ \\|  _ \\ / _ \\ / ___|  _ \\    / \\  |  \\/  |\r\n | |_) | | | |  \\| |  \\| || ||  \\| | |  _  | |_) | |_) | | | | |  _| |_) |  / _ \\ | |\\/| |\r\n |  _ <| |_| | |\\  | |\\  || || |\\  | |_| | |  __/|  _ <| |_| | |_| |  _ <  / ___ \\| |  | |\r\n |_| \\_\\\\___/|_| \\_|_| \\_|___|_| \\_|\\____| |_|   |_| \\_\\\\___/ \\____|_| \\_\\/_/   \\_\\_|  |_|\r\n                                                                                          \r\n\r\n", 500);
            }
            else
            {
                MYDEBUG = -1;
                ProgramStartTitle(150);
            }
        }
        private static void WriteText(string text, int speed, string admin = "")
        {
            if (admin == "admin")
            {
                Console.Write(text);
            }
            else
            {
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] != ' ')
                    {
                        Console.Beep(800, rnd.Next(25, speed + 50));
                        Thread.Sleep(15);
                    }
                    Console.Write(text[i]);
                }
            }
        }
        private static void HexapodeTitle(int textSleepTime)
        {
            Console.WriteLine($"  _   _ _______  __    _    ____   ___  ____  _____");
            Thread.Sleep(textSleepTime);
            Console.WriteLine($" | | | | ____\\ \\/ /   / \\  |  _ \\ / _ \\|  _ \\| ____|");
            Thread.Sleep(textSleepTime);
            Console.WriteLine($" | |_| |  _|  \\  /   / _ \\ | |_) | | | | | | |  _|  ");
            Thread.Sleep(textSleepTime);
            Console.WriteLine($" |  _  | |___ /  \\  / ___ \\|  __/| |_| | |_| | |___ ");
            Thread.Sleep(textSleepTime);
            Console.WriteLine($" |_| |_|_____/_/\\_\\/_/   \\_\\_|    \\___/|____/|_____|");
            Thread.Sleep(textSleepTime);
            for (int i = 0; i < 23; i++)
            {
                Console.WriteLine();
                Thread.Sleep(textSleepTime);
            }
        }

        public static void Init()
        {
            Controller controller = new Controller();
            listener = new ControllerListener();
            SPSender spSender = new SPSender();
            if (SP_TRANSMISSION)
            {
                spSender.SPPort = serialPort;
                spSender.SPSpeed = serialSpeed;
                spSender.Open();
                listener.spSender = spSender;
            }
            if (HEXAPODE)
            {
                Hexapode hexapode = new Hexapode();
                hexapode.Init();
                listener.hexapode = hexapode;
            }

            controller.Connect += listener.OnInit;
            controller.Device += listener.OnConnect;
            controller.FrameReady += listener.OnFrame;
        }
        private static void ProgramExit()
        {
            MyWriteLine("\r\n\r\n                                                                                      \r\n ____  ____   ___   ____ ____     _    __  __    ____ _     ___  ____ ___ _   _  ____ \r\n|  _ \\|  _ \\ / _ \\ / ___|  _ \\   / \\  |  \\/  |  / ___| |   / _ \\/ ___|_ _| \\ | |/ ___|\r\n| |_) | |_) | | | | |  _| |_) | / _ \\ | |\\/| | | |   | |  | | | \\___ \\| ||  \\| | |  _ \r\n|  __/|  _ <| |_| | |_| |  _ < / ___ \\| |  | | | |___| |__| |_| |___) | || |\\  | |_| |\r\n|_|   |_| \\_\\\\___/ \\____|_| \\_/_/   \\_|_|  |_|  \\____|_____\\___/|____|___|_| \\_|\\____|\r\n                                                                                      \r\n\r\n", 250);
        }
        private static void ProgramStartTitle(int textSleepTime)
        {
            MyWriteLine("\r\n\r\n       \r\n ____  \r\n|  _ \\ \r\n| |_) |\r\n|  __/ \r\n|_|    \r\n       \r\n\r\n", textSleepTime);
            MyWriteLine("\r\n\r\n             \r\n ____  ____  \r\n|  _ \\|  _ \\ \r\n| |_) | |_) |\r\n|  __/|  _ < \r\n|_|   |_| \\_\\\r\n             \r\n\r\n", textSleepTime);
            MyWriteLine("\r\n\r\n                   \r\n ____  ____   ___  \r\n|  _ \\|  _ \\ / _ \\ \r\n| |_) | |_) | | | |\r\n|  __/|  _ <| |_| |\r\n|_|   |_| \\_\\\\___/ \r\n                   \r\n\r\n", textSleepTime);
            MyWriteLine("\r\n\r\n                         \r\n ____  ____   ___   ____ \r\n|  _ \\|  _ \\ / _ \\ / ___|\r\n| |_) | |_) | | | | |  _ \r\n|  __/|  _ <| |_| | |_| |\r\n|_|   |_| \\_\\\\___/ \\____|\r\n                         \r\n\r\n", textSleepTime);
            MyWriteLine("\r\n\r\n                               \r\n ____  ____   ___   ____ ____  \r\n|  _ \\|  _ \\ / _ \\ / ___|  _ \\ \r\n| |_) | |_) | | | | |  _| |_) |\r\n|  __/|  _ <| |_| | |_| |  _ < \r\n|_|   |_| \\_\\\\___/ \\____|_| \\_\\\r\n                               \r\n\r\n", textSleepTime);
            MyWriteLine("\r\n\r\n                                       \r\n ____  ____   ___   ____ ____     _    \r\n|  _ \\|  _ \\ / _ \\ / ___|  _ \\   / \\   \r\n| |_) | |_) | | | | |  _| |_) | / _ \\  \r\n|  __/|  _ <| |_| | |_| |  _ < / ___ \\ \r\n|_|   |_| \\_\\\\___/ \\____|_| \\_/_/   \\_\\\r\n                                       \r\n\r\n", textSleepTime);
            MyWriteLine("\r\n\r\n                                              \r\n ____  ____   ___   ____ ____     _    __  __ \r\n|  _ \\|  _ \\ / _ \\ / ___|  _ \\   / \\  |  \\/  |\r\n| |_) | |_) | | | | |  _| |_) | / _ \\ | |\\/| |\r\n|  __/|  _ <| |_| | |_| |  _ < / ___ \\| |  | |\r\n|_|   |_| \\_\\\\___/ \\____|_| \\_/_/   \\_|_|  |_|\r\n                                              \r\n\r\n", textSleepTime);
            MyWriteLine("\r\n\r\n                                                      \r\n ____  ____   ___   ____ ____     _    __  __   ____  \r\n|  _ \\|  _ \\ / _ \\ / ___|  _ \\   / \\  |  \\/  | / ___| \r\n| |_) | |_) | | | | |  _| |_) | / _ \\ | |\\/| | \\___ \\ \r\n|  __/|  _ <| |_| | |_| |  _ < / ___ \\| |  | |  ___) |\r\n|_|   |_| \\_\\\\___/ \\____|_| \\_/_/   \\_|_|  |_| |____/ \r\n                                                      \r\n\r\n", textSleepTime);
            MyWriteLine("\r\n\r\n                                                           \r\n ____  ____   ___   ____ ____     _    __  __   ____ _____ \r\n|  _ \\|  _ \\ / _ \\ / ___|  _ \\   / \\  |  \\/  | / ___|_   _|\r\n| |_) | |_) | | | | |  _| |_) | / _ \\ | |\\/| | \\___ \\ | |  \r\n|  __/|  _ <| |_| | |_| |  _ < / ___ \\| |  | |  ___) || |  \r\n|_|   |_| \\_\\\\___/ \\____|_| \\_/_/   \\_|_|  |_| |____/ |_|  \r\n                                                           \r\n\r\n", textSleepTime);
            MyWriteLine("\r\n\r\n                                                                 \r\n ____  ____   ___   ____ ____     _    __  __   ____ _____  _    \r\n|  _ \\|  _ \\ / _ \\ / ___|  _ \\   / \\  |  \\/  | / ___|_   _|/ \\   \r\n| |_) | |_) | | | | |  _| |_) | / _ \\ | |\\/| | \\___ \\ | | / _ \\  \r\n|  __/|  _ <| |_| | |_| |  _ < / ___ \\| |  | |  ___) || |/ ___ \\ \r\n|_|   |_| \\_\\\\___/ \\____|_| \\_/_/   \\_|_|  |_| |____/ |_/_/   \\_\\\r\n                                                                 \r\n\r\n", textSleepTime);
            MyWriteLine("\r\n\r\n                                                                       \r\n ____  ____   ___   ____ ____     _    __  __   ____ _____  _    ____  \r\n|  _ \\|  _ \\ / _ \\ / ___|  _ \\   / \\  |  \\/  | / ___|_   _|/ \\  |  _ \\ \r\n| |_) | |_) | | | | |  _| |_) | / _ \\ | |\\/| | \\___ \\ | | / _ \\ | |_) |\r\n|  __/|  _ <| |_| | |_| |  _ < / ___ \\| |  | |  ___) || |/ ___ \\|  _ < \r\n|_|   |_| \\_\\\\___/ \\____|_| \\_/_/   \\_|_|  |_| |____/ |_/_/   \\_|_| \\_\\\r\n                                                                       \r\n\r\n", textSleepTime);
            MyWriteLine("\r\n\r\n                                                                            \r\n ____  ____   ___   ____ ____     _    __  __   ____ _____  _    ____ _____ \r\n|  _ \\|  _ \\ / _ \\ / ___|  _ \\   / \\  |  \\/  | / ___|_   _|/ \\  |  _ |_   _|\r\n| |_) | |_) | | | | |  _| |_) | / _ \\ | |\\/| | \\___ \\ | | / _ \\ | |_) || |  \r\n|  __/|  _ <| |_| | |_| |  _ < / ___ \\| |  | |  ___) || |/ ___ \\|  _ < | |  \r\n|_|   |_| \\_\\\\___/ \\____|_| \\_/_/   \\_|_|  |_| |____/ |_/_/   \\_|_| \\_\\|_|  \r\n                                                                            \r\n\r\n", textSleepTime);
            Thread.Sleep(textSleepTime+150);
            MyWriteLine("\r\n\r\n                                                                                        \r\n ____  ____   ___   ____ ____     _    __  __   ____ _____  _    ____ _____ _____ ____  \r\n|  _ \\|  _ \\ / _ \\ / ___|  _ \\   / \\  |  \\/  | / ___|_   _|/ \\  |  _ |_   _| ____|  _ \\ \r\n| |_) | |_) | | | | |  _| |_) | / _ \\ | |\\/| | \\___ \\ | | / _ \\ | |_) || | |  _| | | | |\r\n|  __/|  _ <| |_| | |_| |  _ < / ___ \\| |  | |  ___) || |/ ___ \\|  _ < | | | |___| |_| |\r\n|_|   |_| \\_\\\\___/ \\____|_| \\_/_/   \\_|_|  |_| |____/ |_/_/   \\_|_| \\_\\|_| |_____|____/ \r\n                                                                                        \r\n\r\n", 350);
            Thread.Sleep(textSleepTime+100);
            MyWriteLine("\r\n\r\n  ____  _   _ _   _ _   _ ___ _   _  ____   ____  ____   ___   ____ ____      _    __  __ \r\n |  _ \\| | | | \\ | | \\ | |_ _| \\ | |/ ___| |  _ \\|  _ \\ / _ \\ / ___|  _ \\    / \\  |  \\/  |\r\n | |_) | | | |  \\| |  \\| || ||  \\| | |  _  | |_) | |_) | | | | |  _| |_) |  / _ \\ | |\\/| |\r\n |  _ <| |_| | |\\  | |\\  || || |\\  | |_| | |  __/|  _ <| |_| | |_| |  _ <  / ___ \\| |  | |\r\n |_| \\_\\\\___/|_| \\_|_| \\_|___|_| \\_|\\____| |_|   |_| \\_\\\\___/ \\____|_| \\_\\/_/   \\_\\_|  |_|\r\n                                                                                          \r\n\r\n", 500);
        }
        public static void MyWriteLine(string text, int textSleepTime)
        {
            Console.Clear();
            Thread.Sleep(textSleepTime);
            Console.WriteLine(text);
            for (int i = 0; i < 20; i++)
                Console.WriteLine();
            Thread.Sleep(textSleepTime);
        }        
    }
}

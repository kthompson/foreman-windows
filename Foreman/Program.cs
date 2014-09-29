using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace Foreman
{
    static class Program
    {
        static readonly object ConsoleLock = new object();

        static readonly List<ConsoleColor> Colors = new List<ConsoleColor>
        {
            ConsoleColor.DarkGreen,
            ConsoleColor.DarkCyan,
            ConsoleColor.DarkMagenta,
            ConsoleColor.DarkYellow,
        };

        static readonly Dictionary<string, ConsoleColor> ColorByNumber = new Dictionary<string, ConsoleColor>
        {
            {"0", ConsoleColor.White},
            {"30", ConsoleColor.Black},
            {"34", ConsoleColor.Blue},
            {"32", ConsoleColor.Green},
            {"36", ConsoleColor.Cyan},
            {"31", ConsoleColor.Red},
            {"35", ConsoleColor.Magenta/*Purple*/},
            {"33", ConsoleColor.Green/*brown*/},
            {"37", ConsoleColor.Gray},
        };

        static readonly Dictionary<string, ConsoleColor> ColorByName = new Dictionary<string, ConsoleColor>();

        static int _padding;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            var filename = args.Length == 1 ? args[0] : Path.Combine(Directory.GetCurrentDirectory(), "Procfile");

            using (var procfile = new Procfile(filename))
            {
                InitPadding(procfile);
                InitColorsByname(procfile);

                procfile.ProcessDataReceived += ProcfileOnProcessDataReceived;
                procfile.StatusReceived += ProcfileOnProcessDataReceived;

                Console.CancelKeyPress += ConsoleOnCancelKeyPress;

                procfile.Start();

                lock (ConsoleLock)
                {
                    Monitor.Wait(ConsoleLock);
                }

                procfile.Stop();

                Console.CancelKeyPress -= ConsoleOnCancelKeyPress;

                procfile.StatusReceived -= ProcfileOnProcessDataReceived;
                procfile.ProcessDataReceived -= ProcfileOnProcessDataReceived;
            }
        }

        static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs consoleCancelEventArgs)
        {
            // we dont want the process to terminate before we kill the processes we started
            consoleCancelEventArgs.Cancel = true;

            lock (ConsoleLock)
            {
                Monitor.Pulse(ConsoleLock);
            }
        }

        static void InitColorsByname(Procfile procfile)
        {
            ColorByName.Add("system", ConsoleColor.White);

            for (int i = 0; i < procfile.ProcfileEntries.Count; i++)
            {
                ColorByName.Add(procfile.ProcfileEntries[i].Name, Colors[i%Colors.Count]);
            }
        }

        static void InitPadding(Procfile procfile)
        {
            var intLongestName = procfile.ProcfileEntries.Select(objEntry => objEntry.Name.Length).Max();
            _padding = Math.Max(6, intLongestName);
        }

        static void ProcfileOnProcessDataReceived(object sender, ProcfileEventArgs args)
        {
            lock (typeof(Console))
            {
                Console.ForegroundColor = ColorByName[args.Name];
                Console.Write(@"{0} {1,-" + _padding + "} | ", args.Time.ToString("HH:mm:ss"), args.Name);

                Console.ForegroundColor = ConsoleColor.White;
                CustomWriteLine(args.Text);
            }
        }

        static void CustomWriteLine(string text)
        {
            while (true)
            {
                //Extract color codes for *nix commands
                
                //Black       0;30     Dark Gray     1;30
                //Blue        0;34     Light Blue    1;34
                //Green       0;32     Light Green   1;32
                //Cyan        0;36     Light Cyan    1;36
                //Red         0;31     Light Red     1;31
                //Purple      0;35     Light Purple  1;35
                //Brown       0;33     Yellow        1;33
                //Light Gray  0;37     White         1;37

                var regex = new Regex(@"\[(\d+)m");
                var match = regex.Match(text);

                if (!match.Success)
                {
                    Console.WriteLine(text);
                    return;
                }

                var first = text.Substring(0, match.Index);
                var second = text.Substring(match.Index + match.Length);

                Console.Write(first);
                Console.ForegroundColor = ColorByNumber[match.Groups[1].Value];
                text = second;
            }
        }
    }
}

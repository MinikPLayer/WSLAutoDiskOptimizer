using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WSLAutoDiskOptimizer
{
    [SupportedOSPlatform("windows")]
    class Program
    {
        static void OptimizeDisk(string distroName, string extPath)
        {
            Console.Write($"Optimizing {distroName}... ");
            var p = new Process();
            p.StartInfo.FileName = "powershell.exe";
            p.StartInfo.Arguments = $"Optimize-VHD {extPath}";
            p.StartInfo.UseShellExecute = false;

            p.Start();
            p.WaitForExit();

            if (p.ExitCode != 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Success!");
                Console.ResetColor();
            }
        }

        static bool ShutdownWSL()
        {
            Console.Write("Shutting down WSL... ");
            var p = new Process();
            p.StartInfo.FileName = "wsl.exe";
            p.StartInfo.Arguments = "--shutdown";
            p.StartInfo.UseShellExecute = false;
            p.Start();
            p.WaitForExit();

            var success = p.ExitCode == 0;
            if(success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Success!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed!");
                Console.ResetColor();
            }

            return p.ExitCode == 0;
        }

        static void Main(string[] args)
        {
            var reg = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Lxss");
            if(reg == null)
            {
                Console.WriteLine("No WSL distro found.");
                return;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("This script will shutdown the WSL. Do you want to continue? (y/n) ");
            Console.ResetColor();
            var key = Console.ReadKey();
            Console.WriteLine();
            if (key.Key != ConsoleKey.Y)
            {
                Console.WriteLine("Exiting...");
                return;
            }    

            if(!ShutdownWSL())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to shutdown WSL.");
                Console.ResetColor();
                return;
            }

            Console.WriteLine("\nProcessing distros...");
            var distros = reg.GetSubKeyNames();
            foreach (var distro in distros)
            {
                var distroKey = reg.OpenSubKey(distro);
                var distroPath = distroKey!.GetValue("BasePath");
                if(distroPath == null)
                {
                    Console.WriteLine("No BasePath found for distro {distro}");
                    continue;
                }

                var distroNameKey = distroKey.GetValue("DistributionName");
                if (distroNameKey == null)
                    distroNameKey = "Unknown";
                
                var fullPath = distroPath.ToString()! + "\\ext4.vhdx";
                if (!File.Exists(fullPath))
                {
                    Console.WriteLine($"ext4.vhdx not found for {distro}");
                    continue;
                }
                OptimizeDisk(distroNameKey.ToString()!, fullPath);
            }

            Console.Write("\nProcessing completed!\nPress any key to exit.");
            Console.ReadKey();
        }
    }
}

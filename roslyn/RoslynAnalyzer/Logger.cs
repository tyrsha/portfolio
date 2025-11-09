using System;
using System.IO;
using System.Text;

namespace Roslyn
{
    internal static class Logger
    {
        private static readonly string LogPath = "./roslyn_log.txt";

        public static void Log(string msg)
        {
            try
            {
                File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] {msg}\n", Encoding.UTF8);
            }
            catch { /* ignore */ }
        }
    }
}
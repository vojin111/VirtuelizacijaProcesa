using Server;
using System;

namespace Server
{
    public static class Logger
    {
        public static void HandleTransferStarted(object sender, TransferEventArgs e)
        {
            Write(ConsoleColor.Cyan, "TRANSFER START", e.Message);
        }

        public static void HandleSampleReceived(object sender, SampleEventArgs e)
        {
            // Ispis svakog 10 da konzola ne bude pretrpana.
            if (e.Sample.RowIndex % 10 == 0 || e.Sample.RowIndex == 0)
            {
                Write(ConsoleColor.Gray, "SAMPLE", $"{e.Message} -> {e.Sample}");
            }
        }

        public static void HandleTransferCompleted(object sender, TransferEventArgs e)
        {
            Write(ConsoleColor.Green, "TRANSFER END", e.Message);
        }

        public static void HandleWarning(object sender, WarningEventArgs e)
        {
            Write(ConsoleColor.Yellow,
                  $"WARNING/{e.WarningType}",
                  $"row #{e.RowIndex}, direction={e.Direction}, value={e.Value:F3}, threshold={e.Threshold:F3} | {e.Message}");
        }

        private static readonly object lockObj = new object();

        private static void Write(ConsoleColor color, string tag, string message)
        {
            lock (lockObj)
            {
                ConsoleColor original = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{tag}] {message}");
                Console.ForegroundColor = original;
            }
        }
    }
}

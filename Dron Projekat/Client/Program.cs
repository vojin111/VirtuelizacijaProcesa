using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.ServiceModel;
using System.Threading;
using Common;

namespace Client
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("  DRONE WCF Client");
            Console.WriteLine("===========================================");

            string csvPath = ConfigurationManager.AppSettings["csvPath"]
                                    ?? "..\\..\\..\\database\\3.csv";
            string rejectsLogPath = ConfigurationManager.AppSettings["rejectsLogPath"]
                                    ?? "Logs\\rejected_rows.log";
            int maxRows = int.Parse(ConfigurationManager.AppSettings["maxRows"] ?? "110");
            int delayMs = int.Parse(ConfigurationManager.AppSettings["delayMs"] ?? "50");
            bool simulateBreak = bool.Parse(ConfigurationManager.AppSettings["simulateBreak"] ?? "false");
            int breakAfter = int.Parse(ConfigurationManager.AppSettings["breakAfter"] ?? "30");

            Console.WriteLine($"CSV file       : {csvPath}");
            Console.WriteLine($"Max rows     : {maxRows}");
            Console.WriteLine($"Pause between : {delayMs} ms");
            if (simulateBreak)
                Console.WriteLine($"The simulation stops after {breakAfter} samples!");
            Console.WriteLine();

            List<DroneSample> samples;
            using (Client.CsvReader reader = new Client.CsvReader(csvPath, rejectsLogPath))
            {
                samples = reader.ReadSamples(maxRows);
                Console.WriteLine($"Successfully loaded {samples.Count} valid samples from the CSV.");
                Console.WriteLine($"Log invalid: {Path.GetFullPath(rejectsLogPath)}");
                Console.WriteLine();
            }

            if (samples.Count == 0)
            {
                Console.WriteLine("There are no rows to send.");
                Console.ReadKey();
                return;
            }

            ChannelFactory<IDroneService> factory =
                new ChannelFactory<IDroneService>("DroneServiceEndpoint");
            IDroneService proxy = factory.CreateChannel();
            ICommunicationObject channel = (ICommunicationObject)proxy;

            try
            {
                DroneMeta meta = new DroneMeta(Path.GetFileName(csvPath), samples.Count);
                AckResponse startAck = proxy.StartSession(meta);
                Console.WriteLine($"StartSession -> {startAck}");
                Console.WriteLine();

                int sent = 0;
                foreach (DroneSample sample in samples)
                {
                    if (simulateBreak && sent == breakAfter)
                    {
                        throw new InvalidOperationException(
                            $"Simulated connection interruption after {sent} sent samples.");
                    }

                    try
                    {
                        AckResponse ack = proxy.PushSample(sample);
                        if (ack.Status == ResponseStatus.NACK)
                            Console.WriteLine($" [NACK] #{sample.RowIndex}: {ack.Message}");
                    }
                    catch (FaultException<ValidationFault> vex)
                    {
                        Console.WriteLine($" [VALIDATION] #{sample.RowIndex}: {vex.Detail.Message}");
                    }
                    catch (FaultException<DataFormatFault> dex)
                    {
                        Console.WriteLine($" [FORMAT] #{sample.RowIndex}: {dex.Detail.Message}");
                    }

                    sent++;
                    if (delayMs > 0)
                        Thread.Sleep(delayMs);
                }

                AckResponse endAck = proxy.EndSession();
                Console.WriteLine();
                Console.WriteLine($"EndSession -> {endAck}");

                channel.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"Error during transmission: {ex.Message}");
                Console.WriteLine("Attempting to close the session and channel...");

                try 
                { 
                    proxy.EndSession(); 
                }
                catch { }

                try 
                { 
                    channel.Abort(); 
                }
                catch { }
            }

            Console.WriteLine();
            Console.WriteLine("Press ENTER for exit.");
            Console.ReadLine();
        }
    }
}

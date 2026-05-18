using System;
using System.Globalization;
using System.IO;
using Common;

namespace Server
{
    public class SessionWriter : IDisposable
    {
        private FileStream measurementsFs;
        private StreamWriter measurementsWriter;

        private FileStream rejectsFs;
        private StreamWriter rejectsWriter;

        private bool disposed = false;

        public string SessionFolder { get; }
        public string MeasurementsPath { get; }
        public string RejectsPath { get; }

        public SessionWriter(string baseFolder, string datasetName)
        {
            string sessionName = string.Format("{0}_{1:yyyyMMdd_HHmmss}",
                Path.GetFileNameWithoutExtension(datasetName), DateTime.Now);
            SessionFolder = Path.Combine(baseFolder, sessionName);

            if (!Directory.Exists(SessionFolder))
            {
                Directory.CreateDirectory(SessionFolder);
            }

            MeasurementsPath = Path.Combine(SessionFolder, "measurements_session.csv");
            RejectsPath = Path.Combine(SessionFolder, "rejects.csv");

            measurementsFs = new FileStream(MeasurementsPath, FileMode.Create, FileAccess.Write);
            measurementsWriter = new StreamWriter(measurementsFs);
            measurementsWriter.WriteLine("RowIndex,Time,LinearAccelerationX,LinearAccelerationY," +
                                         "LinearAccelerationZ,WindSpeed,WindAngle");

            rejectsFs = new FileStream(RejectsPath, FileMode.Create, FileAccess.Write);
            rejectsWriter = new StreamWriter(rejectsFs);
            rejectsWriter.WriteLine("RowIndex,Time,LinearAccelerationX,LinearAccelerationY," +
                                    "LinearAccelerationZ,WindSpeed,WindAngle,Reason");
        }

        public void WriteSample(DroneSample s)
        {
            if (disposed)
                throw new ObjectDisposedException("SessionWriter");

            string line = string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5},{6}", s.RowIndex, s.Time,
                s.LinearAccelerationX, s.LinearAccelerationY, s.LinearAccelerationZ,
                s.WindSpeed, s.WindAngle);

            measurementsWriter.WriteLine(line);
            measurementsWriter.Flush();
        }

        public void WriteReject(DroneSample s, string reason)
        {
            if (disposed)
                throw new ObjectDisposedException("SessionWriter");

            string line = string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5},{6},{7}",s.RowIndex, s.Time,
                s.LinearAccelerationX, s.LinearAccelerationY, s.LinearAccelerationZ,
                s.WindSpeed, s.WindAngle, reason);

            rejectsWriter.WriteLine(line);
            rejectsWriter.Flush();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) 
                return;

            if (disposing)
            {
                try 
                { 
                    if (measurementsWriter != null) 
                    { 
                        measurementsWriter.Flush(); 
                        measurementsWriter.Dispose(); 
                    } 
                }
                catch (Exception ex) 
                { 
                    Console.WriteLine("[Dispose] measurementsWriter: " + ex.Message); 
                }

                try 
                { 
                    if (measurementsFs != null) 
                        measurementsFs.Dispose(); 
                }
                catch (Exception ex) 
                { 
                    Console.WriteLine("[Dispose] measurementsFs: " + ex.Message); 
                }

                try 
                { 
                    if (rejectsWriter != null) 
                    { 
                        rejectsWriter.Flush(); 
                        rejectsWriter.Dispose(); 
                    } 
                }
                catch (Exception ex) 
                { 
                    Console.WriteLine("[Dispose] rejectsWriter: " + ex.Message); 
                }

                try 
                {
                    if (rejectsFs != null) 
                        rejectsFs.Dispose(); 
                }
                catch (Exception ex) 
                { 
                    Console.WriteLine("[Dispose] rejectsFs: " + ex.Message); 
                }

                measurementsWriter = null;
                measurementsFs = null;
                rejectsWriter = null;
                rejectsFs = null;
            }

            disposed = true;
        }

        ~SessionWriter()
        {
            Dispose(false);
        }
    }
}

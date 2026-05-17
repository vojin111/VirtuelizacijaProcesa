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
        private bool disposed = false;

        public string SessionFolder { get; }
        public string MeasurementsPath { get; }

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

            measurementsFs = new FileStream(MeasurementsPath, FileMode.Create, FileAccess.Write);
            measurementsWriter = new StreamWriter(measurementsFs);
            measurementsWriter.WriteLine("RowIndex,Time,LinearAccelerationX,LinearAccelerationY," +
                                         "LinearAccelerationZ,WindSpeed,WindAngle");
        }

        public void WriteSample(DroneSample droneSample)
        {
            if (disposed)
                throw new ObjectDisposedException("SessionWriter");

            string line = string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5},{6}",
                droneSample.RowIndex, droneSample.Time,
                droneSample.LinearAccelerationX, droneSample.LinearAccelerationY, droneSample.LinearAccelerationZ,
                droneSample.WindSpeed, droneSample.WindAngle);

            measurementsWriter.WriteLine(line);
            measurementsWriter.Flush();
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
                { Console.WriteLine("[Dispose] measurementsWriter: " + ex.Message); }

                try
                {
                    if (measurementsFs != null)
                        measurementsFs.Dispose();
                }
                catch (Exception ex)
                { Console.WriteLine("[Dispose] measurementsFs: " + ex.Message); }

                measurementsWriter = null;
                measurementsFs = null;
            }

            disposed = true;
        }

        ~SessionWriter()
        {
            Dispose(false);
        }
    }
}

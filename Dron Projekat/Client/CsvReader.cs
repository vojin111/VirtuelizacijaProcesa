using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Common;

namespace Client
{
    public class CsvReader : IDisposable
    {
        private FileStream fs;
        private MemoryStream ms;
        private StreamReader reader;
        private StreamWriter rejectsLog;
        private bool disposed = false;

        private int idxTime, idxWindSpeed, idxWindAngle;
        private int idxAccX, idxAccY, idxAccZ;

        public string FilePath { get; }
        public string RejectsLogPath { get; }

        public CsvReader(string filePath, string rejectsLogPath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"CSV file doesnt exists: {filePath}");

            FilePath = filePath;
            RejectsLogPath = rejectsLogPath;

            fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            ms = new MemoryStream();
            fs.CopyTo(ms);
            ms.Position = 0;

            reader = new StreamReader(ms);

            string logDir = Path.GetDirectoryName(rejectsLogPath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            rejectsLog = new StreamWriter(rejectsLogPath, false);
            rejectsLog.WriteLine($"=== Rejected rows log === ({DateTime.Now})");
            rejectsLog.WriteLine("RowIndex|Reason|RawLine");
        }

        private void ReadHeader()
        {
            string headerLine = reader.ReadLine();
            if (headerLine == null)
                throw new InvalidDataException("CSV is empty.");

            string[] headers = headerLine.Split(',');

            idxTime = FindColumn(headers, "time");
            idxWindSpeed = FindColumn(headers, "wind_speed");
            idxWindAngle = FindColumn(headers, "wind_angle");
            idxAccX = FindColumn(headers, "linear_acceleration_x");
            idxAccY = FindColumn(headers, "linear_acceleration_y");
            idxAccZ = FindColumn(headers, "linear_acceleration_z");
        }

        private static int FindColumn(string[] headers, string name)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                if (headers[i].Trim().Equals(name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            throw new InvalidDataException($"Column '{name}' was not found in the CSV header.");
        }

        public List<DroneSample> ReadSamples(int maxRows)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(CsvReader));

            ReadHeader();

            var samples = new List<DroneSample>(maxRows);
            int physicalRow = 0;
            int sampleIndex = 0;

            string line;
            while (samples.Count < maxRows && (line = reader.ReadLine()) != null)
            {
                physicalRow++;

                if (string.IsNullOrWhiteSpace(line))
                {
                    LogReject(physicalRow, "Empty row", line);
                    continue;
                }

                string[] parts = line.Split(',');

                try
                {
                    DroneSample s = new DroneSample(
                        rowIndex: sampleIndex,
                        time: ParseDouble(parts, idxTime, "time"),
                        linearAccelerationX: ParseDouble(parts, idxAccX, "linear_acceleration_x"),
                        linearAccelerationY: ParseDouble(parts, idxAccY, "linear_acceleration_y"),
                        linearAccelerationZ: ParseDouble(parts, idxAccZ, "linear_acceleration_z"),
                        windSpeed: ParseDouble(parts, idxWindSpeed, "wind_speed"),
                        windAngle: ParseDouble(parts, idxWindAngle, "wind_angle")
                    );

                    samples.Add(s);
                    sampleIndex++;
                }
                catch (Exception ex)
                {
                    LogReject(physicalRow, ex.Message, line);
                }
            }

            rejectsLog.WriteLine($"--- Successfully loaded valid samples: {samples.Count}, " +
                                 $"Rejected: {physicalRow - samples.Count} ---");
            rejectsLog.Flush();

            return samples;
        }

        private static double ParseDouble(string[] parts, int index, string columnName)
        {
            if (index < 0 || index >= parts.Length)
                throw new FormatException($"Column '{columnName}' is missing in the row.");

            string raw = parts[index].Trim();
            if (string.IsNullOrEmpty(raw))
                throw new FormatException($"Empty value in column '{columnName}'.");

            if (!double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowExponent,
                                 CultureInfo.InvariantCulture, out double value))
            {
                throw new FormatException($"Invalid value '{raw}' in column '{columnName}'.");
            }

            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new FormatException($"NaN/Infinity in column '{columnName}'.");

            return value;
        }

        private void LogReject(int physicalRow, string reason, string rawLine)
        {
            try
            {
                rejectsLog.WriteLine($"{physicalRow}|{reason}|{rawLine}");
            }
            catch
            {
                // nikad ne rusimo citanje zbog log-a
            }
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
                    reader?.Dispose(); 
                }
                catch (Exception ex) 
                { Console.WriteLine($"[Dispose] reader: {ex.Message}"); }

                try 
                { 
                    ms?.Dispose(); 
                }
                catch (Exception ex) 
                { Console.WriteLine($"[Dispose] memoryStream: {ex.Message}"); }

                try 
                { 
                    fs?.Dispose(); 
                }
                catch (Exception ex) 
                { Console.WriteLine($"[Dispose] fileStream: {ex.Message}"); }

                try 
                { 
                    rejectsLog?.Flush(); 
                    rejectsLog?.Dispose(); 
                }
                catch (Exception ex) 
                { Console.WriteLine($"[Dispose] rejectsLog: {ex.Message}"); }

                reader = null;
                ms = null;
                fs = null;
                rejectsLog = null;
            }

            disposed = true;
        }

        ~CsvReader()
        {
            Dispose(false);
        }
    }
}

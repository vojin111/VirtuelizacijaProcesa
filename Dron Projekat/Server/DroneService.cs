using Common;
using Server;
using System;
using System.Configuration;
using System.IO;
using System.ServiceModel;

namespace Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Single)]
    public class DroneService : IDroneService
    {
        private SessionWriter writer;
        private DroneMeta meta;
        private bool sessionActive;
        private int receivedCount;

        public AckResponse StartSession(DroneMeta droneMeta)
        {
            if (droneMeta.Columns == null || droneMeta.Columns.Length == 0)
                throw new FaultException<ValidationFault>(
                    new ValidationFault("The meta header does not contain columns."));

            string baseFolder = ConfigurationManager.AppSettings["storagePath"];
            if (string.IsNullOrEmpty(baseFolder))
                baseFolder = "Measurements";

            if (!Directory.Exists(baseFolder))
                Directory.CreateDirectory(baseFolder);

            writer = new SessionWriter(baseFolder, droneMeta.DatasetName ?? "session");
            meta = droneMeta;
            sessionActive = true;
            receivedCount = 0;

            Console.WriteLine("[{0:HH:mm:ss}] Session opened for dataset '{1}', expecting {2} samples.",
                DateTime.Now, droneMeta.DatasetName, droneMeta.TotalRows);
            Console.WriteLine("Folder: " + writer.SessionFolder);

            return new AckResponse(ResponseStatus.IN_PROGRESS,
                "Session is open, waiting for samples...");
        }

        public AckResponse PushSample(DroneSample droneSample)
        {
            if (!sessionActive)
                throw new FaultException<ValidationFault>(
                    new ValidationFault("The session is not open.Try open StartSession first!"));

            string formatError = ValidateFormat(droneSample);
            if (formatError != null)
                throw new FaultException<DataFormatFault>(new DataFormatFault(formatError));

            string validationError = ValidateRanges(droneSample);
            if (validationError != null)
                throw new FaultException<ValidationFault>(new ValidationFault(validationError));

            writer.WriteSample(droneSample);
            receivedCount++;

            return new AckResponse(ResponseStatus.ACK,
                "Sample #" + droneSample.RowIndex + " received.");
        }

        public AckResponse EndSession()
        {
            if (!sessionActive)
                return new AckResponse(ResponseStatus.NACK, "The session was not open");

            if (writer != null) { writer.Dispose(); writer = null; }
            sessionActive = false;

            Console.WriteLine("[{0:HH:mm:ss}] The session is closed. Received {1} samples.",
                DateTime.Now, receivedCount);

            return new AckResponse(ResponseStatus.COMPLETED,
                "The session is successfully closed. Received " + receivedCount + " samples.");
        }

        private string ValidateFormat(DroneSample s)
        {
            if (double.IsNaN(s.Time) || double.IsInfinity(s.Time))
                return "Time is not a valid number.";
            if (double.IsNaN(s.LinearAccelerationX) || double.IsInfinity(s.LinearAccelerationX))
                return "LinearAccelerationX is not a valid number.";
            if (double.IsNaN(s.LinearAccelerationY) || double.IsInfinity(s.LinearAccelerationY))
                return "LinearAccelerationY is not a valid number.";
            if (double.IsNaN(s.LinearAccelerationZ) || double.IsInfinity(s.LinearAccelerationZ))
                return "LinearAccelerationZ is not a valid number.";
            if (double.IsNaN(s.WindSpeed) || double.IsInfinity(s.WindSpeed))
                return "WindSpeed is not a valid number.";
            if (double.IsNaN(s.WindAngle) || double.IsInfinity(s.WindAngle))
                return "WindAngle is not a valid number.";
            return null;
        }

        private string ValidateRanges(DroneSample s)
        {
            if (s.WindSpeed <= 0)
                return "WindSpeed must be > 0.";
            if (s.WindAngle < 0 || s.WindAngle > 360)
                return "WindAngle must be between [0, 360].";
            if (s.Time < 0)
                return "Time must not be negative.";
            return null;
        }

        public void Dispose()
        {
            try
            {
                if (writer != null) { writer.Dispose(); writer = null; }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[DroneService.Dispose] " + ex.Message);
            }
        }
    }
}

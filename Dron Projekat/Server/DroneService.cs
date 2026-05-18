using Common;
using Server;
using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.ServiceModel;

namespace Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Single)]
    public class DroneService : IDroneService
    {
        public delegate void TransferEventHandler(object sender, TransferEventArgs e);
        public delegate void SampleEventHandler(object sender, SampleEventArgs e);
        public delegate void WarningEventHandler(object sender, WarningEventArgs e);

        public event TransferEventHandler OnTransferStarted;
        public event SampleEventHandler OnSampleReceived;
        public event TransferEventHandler OnTransferCompleted;
        public event WarningEventHandler OnWarningRaised;


        private SessionWriter writer;
        private DroneMeta meta;
        private bool sessionActive;
        private int receivedCount;
        private int rejectedCount;

        private bool hasPreviousAz;
        private double previousAz;
        private double azSum;          
        private int azCountForMean;    

        
        private readonly double azThreshold;
        private readonly double wThreshold;
        private readonly double deviationFraction;

        public DroneService()
        {
            azThreshold = ReadDouble("Az_threshold", 0.5);
            wThreshold = ReadDouble("W_threshold", 8.0);
            deviationFraction = ReadDouble("DeviationFraction", 0.25);

           
            OnTransferStarted += Logger.HandleTransferStarted;
            OnSampleReceived += Logger.HandleSampleReceived;
            OnTransferCompleted += Logger.HandleTransferCompleted;
            OnWarningRaised += Logger.HandleWarning;
        }

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
            rejectedCount = 0;
            hasPreviousAz = false;
            azSum = 0.0;
            azCountForMean = 0;

            OnTransferStarted?.Invoke(this,
                new TransferEventArgs($"Session opened for dataset '{droneMeta.DatasetName}', " +
                                      $"expecting {droneMeta.TotalRows} samples. " +
                                      $"Folder: {writer.SessionFolder}"));

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
            {
                rejectedCount++;
                writer.WriteReject(droneSample, "FORMAT: " + formatError);
                throw new FaultException<DataFormatFault>(new DataFormatFault(formatError));
            }

            string validationError = ValidateRanges(droneSample);
            if (validationError != null)
            {
                rejectedCount++;
                writer.WriteReject(droneSample, "VALIDATION: " + validationError);
                throw new FaultException<ValidationFault>(new ValidationFault(validationError));
            }

            writer.WriteSample(droneSample);
            receivedCount++;

            OnSampleReceived?.Invoke(this,
                new SampleEventArgs(droneSample, $"transfer in progress... (sample #{receivedCount})"));

           
            CheckAltitude(droneSample);
            CheckWindEnergy(droneSample);

            return new AckResponse(ResponseStatus.ACK,
                "Sample #" + droneSample.RowIndex + " received.");
        }

        public AckResponse EndSession()
        {
            if (!sessionActive)
                return new AckResponse(ResponseStatus.NACK, "The session was not open");

            if (writer != null) 
            { 
                writer.Dispose(); 
                writer = null; 
            }
            sessionActive = false;

            OnTransferCompleted?.Invoke(this,
                new TransferEventArgs($"transfer completed. Received: {receivedCount}, " +
                                      $"rejected: {rejectedCount}."));

            return new AckResponse(ResponseStatus.COMPLETED,
                $"Session successfully ended. Received {receivedCount}, rejected {rejectedCount}.");
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

        private void CheckAltitude(DroneSample droneSample)
        {
            double az = droneSample.LinearAccelerationZ;

            if (hasPreviousAz)
            {
                double deltaAz = az - previousAz;
                if (Math.Abs(deltaAz) > azThreshold)
                {
                    DeviationDirection dir = deltaAz < 0
                        ? DeviationDirection.BelowExpected
                        : DeviationDirection.AboveExpected;

                    string direction = dir == DeviationDirection.BelowExpected
                        ? "sudden drop" : "sudden rise";

                    OnWarningRaised?.Invoke(this, new WarningEventArgs("AltitudeDropSpike", dir,
                        Math.Abs(deltaAz), azThreshold, droneSample.RowIndex,
                        $"AltitudeDropSpike ({direction}): |dAz|={Math.Abs(deltaAz):F3} > {azThreshold:F3}"));
                }
            }

            azSum += az;
            azCountForMean++;
            double azMean = azSum / azCountForMean;

            if (azCountForMean > 1 && Math.Abs(azMean) > 1e-9)
            {
                double lower = (1.0 - deviationFraction) * azMean;
                double upper = (1.0 + deviationFraction) * azMean;
                double bandLow = Math.Min(lower, upper);
                double bandHigh = Math.Max(lower, upper);

                if (az < bandLow || az > bandHigh)
                {
                    DeviationDirection dir = az > bandHigh
                        ? DeviationDirection.AboveExpected
                        : DeviationDirection.BelowExpected;

                    string direction = dir == DeviationDirection.AboveExpected
                        ? "above expected value" : "below expected value";

                    OnWarningRaised?.Invoke(this, new WarningEventArgs(
                        "OutOfBandWarning", dir, az, azMean, droneSample.RowIndex,
                        $"OutOfBandWarning ({direction}): Az={az:F3}, mean={azMean:F3}, " +
                        $"band=[{bandLow:F3}, {bandHigh:F3}]"));
                }
            }

            previousAz = az;
            hasPreviousAz = true;
        }

        private void CheckWindEnergy(DroneSample droneSample)
        {
            double wKinetic = 0.5 * droneSample.WindSpeed * droneSample.WindSpeed;

            if (wKinetic > wThreshold)
            {
                OnWarningRaised?.Invoke(this, new WarningEventArgs(
                    "WindEnergySpike", DeviationDirection.AboveExpected,
                    wKinetic, wThreshold, droneSample.RowIndex,
                    $"WindEnergySpike (above expected): Wkinetic={wKinetic:F3} > {wThreshold:F3}"));
            }
        }

        private static double ReadDouble(string key, double defaultValue)
        {
            string raw = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(raw))
                return defaultValue;
            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                return v;
            return defaultValue;
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
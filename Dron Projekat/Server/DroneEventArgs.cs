using System;
using Common;

namespace Server
{
    public class TransferEventArgs : EventArgs
    {
        public DateTime Timestamp { get; }
        public string Message { get; }

        public TransferEventArgs(string message)
        {
            Message = message;
            Timestamp = DateTime.Now;
        }
    }

    public class SampleEventArgs : TransferEventArgs
    {
        public DroneSample Sample { get; }

        public SampleEventArgs(DroneSample sample, string message) : base(message)
        {
            Sample = sample;
        }
    }

    public class WarningEventArgs : TransferEventArgs
    {
        public string WarningType { get; }
        public DeviationDirection Direction { get; }
        public double Value { get; }
        public double Threshold { get; }
        public int RowIndex { get; }

        public WarningEventArgs(string warningType,
                                DeviationDirection direction,
                                double value,
                                double threshold,
                                int rowIndex,
                                string message) : base(message)
        {
            WarningType = warningType;
            Direction = direction;
            Value = value;
            Threshold = threshold;
            RowIndex = rowIndex;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class DroneSample
    {
        [DataMember]
        public int RowIndex { get; set; }

        [DataMember]
        public double Time { get; set; }

        [DataMember]
        public double LinearAccelerationX { get; set; }

        [DataMember]
        public double LinearAccelerationY { get; set; }

        [DataMember]
        public double LinearAccelerationZ { get; set; }

        [DataMember]
        public double WindSpeed { get; set; }

        [DataMember]
        public double WindAngle { get; set; }

        public DroneSample() { }

        public DroneSample(int rowIndex, double time, double linearAccelerationX,
                           double linearAccelerationY, double linearAccelerationZ,
                           double windSpeed, double windAngle)
        {
            RowIndex = rowIndex;
            Time = time;
            LinearAccelerationX = linearAccelerationX;
            LinearAccelerationY = linearAccelerationY;
            LinearAccelerationZ = linearAccelerationZ;
            WindSpeed = windSpeed;
            WindAngle = windAngle;
        }

        public override string ToString()
        {
            return $"[#{RowIndex}] t={Time:F2}s | Az={LinearAccelerationZ:F3} | " +
                   $"Wind={WindSpeed:F2} m/s @ {WindAngle:F0}°";
        }
    }
}

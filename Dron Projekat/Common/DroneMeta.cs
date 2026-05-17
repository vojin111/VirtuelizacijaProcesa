using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class DroneMeta
    {
        [DataMember]
        public string DatasetName { get; set; }

        [DataMember]
        public string[] Columns { get; set; }

        [DataMember]
        public int TotalRows { get; set; }

        [DataMember]
        public DateTime StartedAt { get; set; }

        public DroneMeta() { }

        public DroneMeta(string datasetName, int totalRows)
        {
            DatasetName = datasetName;
            TotalRows = totalRows;
            StartedAt = DateTime.Now;
            Columns = new[]
            {
                "LinearAccelerationX",
                "LinearAccelerationY",
                "LinearAccelerationZ",
                "WindSpeed",
                "WindAngle",
                "Time"
            };
        }
    }
}

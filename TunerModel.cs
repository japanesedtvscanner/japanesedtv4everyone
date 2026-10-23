using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinelScanner4JPTV
{
	internal class TunerModel
	{
		public string ModelName { get; set; }

		public int TerrestrialCount { get; set; }

		public int SatelliteCount { get; set; }
		public TunerModel(string modelName, int terrestrialCount, int satelliteCount)
		{
			ModelName = modelName;
			TerrestrialCount = terrestrialCount;
			SatelliteCount = satelliteCount;
		}
	}
}

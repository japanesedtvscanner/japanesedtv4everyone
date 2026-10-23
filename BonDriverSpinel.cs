using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace SpinelScanner4JPTV
{
	internal class BonDriverSpinel
	{
		public BonDriverSpinel(string strIP, int intPort = 48083)
		{
			IP = strIP;
			Port = intPort;
		}

		public string IP { get; set; }

		public int Port { get; set; }

		public int DataStreamPort
		{
			get
			{
				return Port + 1;
			}
		}
	}
}

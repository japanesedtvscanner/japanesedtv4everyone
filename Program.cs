using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SpinelScanner4JPTV
{
	internal class Program
	{
		/// <summary>
		/// Call the Vanyo library to test BonDriver
		/// </summary>
		/// <param name="bonDriverPath">BonDriver path</param>
		/// <returns></returns>
		[DllImport("vanyo.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int LoadBonDriverDLL([MarshalAs(UnmanagedType.LPWStr)] string bonDriverPath);

		public static readonly Assembly assembly = Assembly.GetExecutingAssembly();
		public static readonly AssemblyName assemblyName = assembly.GetName();
		public static readonly Version version = assemblyName.Version;
#if DEBUG
		public static readonly string publishChannel = "DEBUG";
#else
		public static readonly string publishChannel = "PUBLIC";
#endif
		public static readonly string ver =
			version.Major.ToString() + "." +
			version.Minor.ToString() + "." +
			version.Build.ToString() + "." +
			version.Revision.ToString() + " " +
			publishChannel;

		private static List<BonDriverSpinel> lstBonDriverSpinel;// BonDriver list to test
		private static List<TunerModel> lstTunerModel;// Spinel Tuner Model list to test
		private static readonly ILog m_log = LogManager.GetLogger("log");
		private static Dictionary<string, int> dicRegionCount;// the count of each region
		private static DataTable dtResult;
		private static bool DO_NOT_SAVE_BONDRIVER = false;
		private static bool REQUIRE_EXCLUSIVE_CHANNEL_CONTROL = false;
		private static readonly string PROGRAM_PATH = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		private static string OUT_PUT_PATH = Path.Combine(PROGRAM_PATH, "BonDriverValid");
		private static readonly Dictionary<string, string> dicJPRegionEN_CN = new Dictionary<string, string>()
		{
			{ "Japan", "Japan" },
			{ "Hokkaido", "Hokkaido" },
			{ "Aomori", "Aomori" },
			{ "Iwate", "Iwate" },
			{ "Miyagi", "Miyagi" },
			{ "Akita", "Akita" },
			{ "Yamagata", "Yamagata" },
			{ "Fukushima", "Fukushima" },
			{ "Ibaraki", "Ibaraki" },
			{ "Tochigi", "Tochigi" },
			{ "Gunma", "Gunma" },
			{ "Saitama", "Saitama" },
			{ "Chiba", "Chiba" },
			{ "Tokyo", "Tokyo" },
			{ "Kanagawa", "Kanagawa" },
			{ "Niigata", "Niigata" },
			{ "Toyama", "Toyama" },
			{ "Ishikawa", "Ishikawa" },
			{ "Fukui", "Fukui" },
			{ "Yamanashi", "Yamanashi" },
			{ "Nagano", "Nagano" },
			{ "Gifu", "Gifu" },
			{ "Shizuoka", "Shizuoka" },
			{ "Aichi", "Aichi" },
			{ "Mie", "Mie" },
			{ "Shiga", "Shiga" },
			{ "Kyoto", "Kyoto" },
			{ "Osaka", "Osaka" },
			{ "Hyōgo", "Hyogo" },
			{ "Nara", "Nara" },
			{ "Wakayama", "Wakayama" },
			{ "Totori", "Tottori" },
			{ "Shimane", "Shimane" },
			{ "Okayama", "Okayama" },
			{ "Hiroshima", "Hiroshima" },
			{ "Yamaguchi", "Yamaguchi" },
			{ "Tokushima", "Tokushima" },
			{ "Kagawa", "Kagawa" },
			{ "Eihime", "Ehime" },
			{ "Takachi", "Kochi" },
			{ "Fukuoka", "Fukuoka" },
			{ "Saga", "Saga" },
			{ "Nagazaki", "Nagasaki" },
			{ "Kumamoto", "Kumamoto" },
			{ "Ooita", "Oita" },
			{ "Miyazaki", "Miyazaki" },
			{ "Kagoshima", "Kagoshima" },
			{ "Okinawa", "Okinawa"}
		};

		static void Main(string[] args)
		{
			string title = "SpinelScanner4JPTV v" + ver;
			Console.Title = title;

			Console.WriteLine(
				$"┌┬┐┬ ┬┌┐┌┌─┐┬─┐┌┬┐┌─┐┌─┐┌┬┐  ┌─┐ ─┼─ ┌┬┐┬  ┬  ┌─┐┌─┐┌┐┌ ┌─┐\r\n" +
				$" │ │ ││││├─ ├┬┘ │ ├─ └─┐ │   ├─┤ ┌┼┐  │ └┐┌┘  │─┬├─ │││ ┌─┘ ({ver})\r\n" +
				$" ┴ └─┘┘└┘└─┘┴└─ ┴ └─┘└─┘ ┴   └─┘ ┘┼└  ┴  └┘   └─┘└─┘┘└┘■└─┘ (NO VANYO.EXE ANY MORE)\r\n"
			);

			m_log.Info("【■■■■■■SpinelScanner4JPTV BOOM■■■■■■】");
#if DEBUG
			m_log.Debug("【■■■■■■SpinelScanner4JPTV BOOM■■■■■■】");
#endif

			#region Initialize Spinel BonDriver 4 TEST
			if (!Directory.Exists("./BonDriverTest"))
			{
				Directory.CreateDirectory("./BonDriverTest");
			}
			for (int i = 0; i < 2; ++i)
			{
				if (!File.Exists($"./BonDriverTest/BonDriver_Spinel_Test_{i}.dll"))
				{
					File.Copy("./BonDriver_Spinel.dll", $"./BonDriverTest/BonDriver_Spinel_Test_{i}.dll");
				}
				if (File.Exists($"./BonDriverTest/BonDriver_Spinel_Test_{i}.dll.log"))
				{
					File.Delete($"./BonDriverTest/BonDriver_Spinel_Test_{i}.dll.log");
				}
			}
			#endregion

			string line;
			#region Initialize Spinel Tuner Model list
			lstTunerModel = new List<TunerModel>();
			using (StreamReader tunerModel = new StreamReader("./SpinelTunerModel.txt"))
			{
				m_log.Info("The Spinel Tuner Model list is as follows:");
				while ((line = tunerModel.ReadLine()) != null)
				{
					string[] arr = line.Split(',');
					m_log.Info($"Tuner Model: {arr[0]} Terrestrial: {arr[1]} Satellite: {arr[2]}");
					lstTunerModel.Add(new TunerModel(arr[0], int.Parse(arr[1]), int.Parse(arr[2])));
				}
			}
			#endregion

			#region Initialize IP sequence
			lstBonDriverSpinel = new List<BonDriverSpinel>();
			m_log.Info("Checking if each IP is accessible on the corresponding port...");
			using (StreamReader srIPList = new StreamReader("./ip.txt"))
			{
				while ((line = srIPList.ReadLine()) != null)
				{
					List<string> lstIP = new List<string>();
					if (line.Contains("/"))
					{
						lstIP.AddRange(GenerateIpListFromCidr(line, excludeNetworkAndBroadcast: true));
					}
					else
					{
						lstIP.Add(line);
					}
					foreach (string ip in lstIP)
					{
						BonDriverSpinel bon = new BonDriverSpinel(ip);
						m_log.Info($"Testing IP: {bon.IP}");
						// Port connectivity test
						using (TcpClient client = new TcpClient())
						{
							try
							{
								client.Connect(bon.IP, bon.Port);
								client.Close();
							}
							catch (SocketException)
							{
								m_log.Info($"{bon.IP}:{bon.Port} cannot be connected");
								client.Close();
								continue;
							}
						}
						using (TcpClient client = new TcpClient())
						{
							try
							{
								client.Connect(bon.IP, bon.DataStreamPort);
								client.Close();
							}
							catch (SocketException)
							{
								m_log.Info($"{bon.IP}:{bon.DataStreamPort} cannot be connected");
								client.Close();
								continue;
							}
						}

						lstBonDriverSpinel.Add(bon);
					}
				}
			}
			#endregion

			if (args.Length != 0)
			{
				if (args.Contains("--nodll"))
				{
					DO_NOT_SAVE_BONDRIVER = true;
				}
				if (args.Contains("--output-path"))
				{
					int index = Array.IndexOf(args, "--output-path");
					OUT_PUT_PATH = args[index + 1];
				}
				if (args.Contains("--channel-ctrl"))
				{
					REQUIRE_EXCLUSIVE_CHANNEL_CONTROL = true;
				}
			}

			// Initialize region count dictionary
			dicRegionCount = new Dictionary<string, int>();
			// Initialize result DataTable
			dtResult = new DataTable();
			dtResult.Columns.Add("IP", typeof(string));
			dtResult.Columns.Add("Region", typeof(string));
			dtResult.Columns.Add("RegionCN", typeof(string));
			dtResult.Columns.Add("City", typeof(string));
			dtResult.Columns.Add("Model", typeof(string));
			dtResult.Columns.Add("ModelIndex", typeof(int));
			dtResult.Columns.Add("BonDriverNamePrefix", typeof(string));
			dtResult.Columns.Add("ISP", typeof(string));
			dtResult.Columns.Add("Hostname", typeof(string));

			m_log.Info($"{lstBonDriverSpinel.Count} IPs initialized, the existing BonDriver DLL files in the output folder will be cleared, please press Enter to continue");
			Console.ReadLine();

			if (!Directory.Exists(OUT_PUT_PATH))
			{
				try
				{
					Directory.CreateDirectory(OUT_PUT_PATH);
				}
				catch (Exception)
				{
					m_log.Error($"Cannot create DLL output folder at {OUT_PUT_PATH}");
					return;
				}
			}
			string[] files = Directory.GetFiles(OUT_PUT_PATH);
			foreach (string file in files)
			{
				File.Delete(file);
			}

			int count = 0;
			foreach (BonDriverSpinel bonSpinel in lstBonDriverSpinel)
			{
				++count;
				Console.Title = $"[{count}/{lstBonDriverSpinel.Count}]{title}";
				// Run Spinel test
				if (SpinelTest(bonSpinel, 0, "TEST_MODEL", 99, "TEST_SIGNAL", 99, true))
				{
					m_log.Info($"{bonSpinel.IP}:{bonSpinel.Port} find valid Spinel service, start testing tuner models...");
					bool undetectedModel = true;
					foreach (TunerModel tunerModel in lstTunerModel)
					{
						int modelIndex = 0;
						string signalType = "";
						// Test tuner count
						signalType = tunerModel.TerrestrialCount > 0 ? "T" : "S";
						while (SpinelTest(bonSpinel, 1, tunerModel.ModelName, modelIndex, signalType, 0, false))
						{
							undetectedModel = false;
							m_log.Info($"{bonSpinel.IP}:{bonSpinel.Port} found {tunerModel.ModelName}/{modelIndex}");
							SaveValidBonDriverSpinel(bonSpinel, tunerModel, modelIndex);
							++modelIndex;
						}
					}
					if (undetectedModel)
					{
						m_log.Warn($"{bonSpinel.IP}:{bonSpinel.Port} found valid Spinel service, but none of the configured tuner models were detected");
					}
				}
			}

			Console.Title = title;

			string resultExcelFilePath = Path.Combine(PROGRAM_PATH, $"SpinelTestResult_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
			dtResult.TableName = "SpinelTestResult";

			ExcelHelper.SaveDataTable2Excel(dtResult, resultExcelFilePath);

			foreach (var result in dicRegionCount)
			{
				m_log.Info($"【{(dicJPRegionEN_CN.ContainsKey(result.Key) ? dicJPRegionEN_CN[result.Key] : result.Key)}】detected {result.Value} BonDriver instances (multiple tuners on the same IP are counted separately)");
			}

			m_log.Info($"Spinel test completed. Results saved to {resultExcelFilePath}. Press Enter to open the file");
			Console.ReadLine();

			// Open the Excel file
			Process.Start(resultExcelFilePath);
		}

		/// <summary>
		/// Spinel test
		/// </summary>
		/// <param name="bon">BonDriver</param>
		/// <param name="index">Index</param>
		/// <param name="modelName">Tuner model</param>
		/// <param name="modelIndex">Tuner modelIndex</param>
		/// <param name="signalType">Signal type</param>
		/// <param name="signalIndex">Signal index</param>
		/// <param name="allowPathError">Whether path errors are allowed</param>
		/// <returns>Whether it is available</returns>
		static bool SpinelTest(
			BonDriverSpinel bon,
			int index,
			string modelName,
			int modelIndex,
			string signalType,
			int signalIndex,
			bool allowPathError = true
			)
		{
			string bonDLLPath = $"./BonDriverTest/BonDriver_Spinel_Test_{index}.dll";
			string iniFilePath = $"./BonDriverTest/BonDriver_Spinel_Test_{index}.dll.ini";
			string logFilePath = $"./BonDriverTest/BonDriver_Spinel_Test_{index}.dll.log";
			if (File.Exists(logFilePath))
			{
				File.Delete(logFilePath);
			}
#if DEBUG
			m_log.Debug($"Testing {modelName}/{modelIndex}/{signalType}/{signalIndex} on {bon.IP}:{bon.Port}...");
#endif

			// Overwrite BonDLL INI data
			using (StreamWriter sw = new StreamWriter(iniFilePath, false))
			{
				sw.WriteLine(GenerateINIContent4DLL(bon, modelName, modelIndex, signalType, signalIndex));
			}

			int bonStatusCode = -9999;
			int retry = 0;
			while (retry < 3)
			{
				try
				{
					bonStatusCode = LoadBonDriverDLL(bonDLLPath);
					break;
				}
				catch (Exception)
				{
					++retry;
					m_log.Warn($"{bon.IP}:{bon.Port} failed to load {modelName}/{modelIndex}/{signalType}/{signalIndex}; retrying (attempt {retry})");
					continue;
				}
			}

			//Parse the log file
			string line;
			switch (bonStatusCode)
			{
				case 0:
				case 6:
					return true;
				default:
					break;
			}
			if (File.Exists(logFilePath))
			{
				using (StreamReader logreader = new StreamReader(logFilePath))
				{
					while ((line = logreader.ReadLine()) != null)
					{
						if (line.Contains("古すぎます"))
						{
							m_log.Warn($"{bon.IP}:{bon.Port} has an older Spinel server version for {modelName}/{modelIndex}/{signalType}/{signalIndex}");
							return true;
						}
						if (line.Contains("予期しない例外"))
						{
#if DEBUG
							m_log.Debug($"{bon.IP}:{bon.Port} encountered an unexpected connection error for {modelName}/{modelIndex}/{signalType}/{signalIndex}; this may cause a false result");
#endif
							return false;
						}
						if (line.Contains("タイムアウト"))
						{
#if DEBUG
							m_log.Debug($"{bon.IP}:{bon.Port} timed out in Vanyo for {modelName}/{modelIndex}/{signalType}/{signalIndex}");
#endif
							return false;
						}
						if (line.Contains("OpenTuner: DataLinkの生成に失敗"))
						{
							m_log.Warn($"{bon.IP}:{bon.Port} failed to establish the data link for {modelName}/{modelIndex}/{signalType}/{signalIndex}");
							return true;
						}
						if (line.Contains("指定したパスに対応するチューナーがサーバー上に存在しません"))
						{
#if DEBUG
							m_log.Debug($"{bon.IP}:{bon.Port} has no such device for {modelName}/{modelIndex}/{signalType}/{signalIndex}");
#endif
							return allowPathError;
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Save valid Spinel BonDriver
		/// </summary>
		/// <param name="bon">BonDriver IP</param>
		/// <param name="model">Tuner model information</param>
		/// <param name="modelIndex">Tuner model index</param>
		static async void SaveValidBonDriverSpinel(BonDriverSpinel bon, TunerModel model, int modelIndex)
		{
			// Check the prefecture associated with the IP address
			string apiUrl = $"https://ipinfo.io/{bon.IP}/json";
			string region = "Unknown";
			string regionCN = "Unknown";
			string city = "Unknown";
			string isp = "Unknown";
			string hostname = "Unknown";

			// Split the IP address by dots
			string[] octets = bon.IP.Split('.');

			// Store the converted hexadecimal segments
			string[] hexParts = new string[4];

			for (int i = 0; i < octets.Length; i++)
			{
				// Convert each decimal segment to hexadecimal and ensure each segment has two digits (pad with zero if needed)
				hexParts[i] = int.Parse(octets[i]).ToString("X2");
			}

			// Concatenate the four segments to produce the final hexadecimal string
			string hexAddress = string.Join("", hexParts);

			using (HttpClient client = new HttpClient())
			{
				HttpResponseMessage response = await client.GetAsync(apiUrl);
				if (response.IsSuccessStatusCode)
				{
					string jsonResponse = await response.Content.ReadAsStringAsync();
					JObject ipInfo = JObject.Parse(jsonResponse);
					// Get prefecture information
					region = ipInfo["region"]?.ToString();
					if (dicJPRegionEN_CN.ContainsKey(region))
					{
						regionCN = dicJPRegionEN_CN[region];
					}
					city = ipInfo["city"]?.ToString();
					isp = ipInfo["org"]?.ToString();
					hostname = ipInfo["hostname"]?.ToString();
				}
			}

			if (!dicRegionCount.ContainsKey(region))
			{
				dicRegionCount.Add(region, 0);
			}
			dicRegionCount[region]++;
			string bonFilePathModel = Path.Combine(OUT_PUT_PATH, $"BonDriver_Spinel_{region}_{hexAddress}_{model.ModelName}_{modelIndex}_【SIGNAL_TYPE】_【SIGNAL_INDEX】.dll");

			if (DO_NOT_SAVE_BONDRIVER)
			{
				dtResult.Rows.Add(
				bon.IP,
				region,
				regionCN,
				city,
				model.ModelName,
				modelIndex,
				"NOT_SAVED_BY_USER",
				isp,
				hostname
				);
			}
			else
			{
				// Save terrestrial tuner entries
				for (int i = 0; i < model.TerrestrialCount; i++)
				{
					string bonFilePath = bonFilePathModel.Replace("【SIGNAL_TYPE】", "T");
					bonFilePath = bonFilePath.Replace("【SIGNAL_INDEX】", i.ToString());
					File.Copy("./BonDriver_Spinel.dll", bonFilePath, overwrite: true);
					string iniFilePath = $"{bonFilePath}.ini";
					using (StreamWriter sw = new StreamWriter(iniFilePath, false))
					{
						sw.WriteLine(GenerateINIContent4DLL(bon, model.ModelName, modelIndex, "T", i));
					}
				}
				// Save satellite tuner entries
				for (int i = 0; i < model.SatelliteCount; i++)
				{
					string bonFilePath = bonFilePathModel.Replace("【SIGNAL_TYPE】", "S");
					bonFilePath = bonFilePath.Replace("【SIGNAL_INDEX】", i.ToString());
					File.Copy("./BonDriver_Spinel.dll", bonFilePath, overwrite: true);
					string iniFilePath = $"{bonFilePath}.ini";
					using (StreamWriter sw = new StreamWriter(iniFilePath, false))
					{
						sw.WriteLine(GenerateINIContent4DLL(bon, model.ModelName, modelIndex, "S", i));
					}
				}
				dtResult.Rows.Add(
					bon.IP,
					region,
					regionCN,
					city,
					model.ModelName,
					modelIndex,
					$"BonDriver_Spinel_{region}_{hexAddress}_{model.ModelName}_{modelIndex}",
					isp,
					hostname
					);
			}
		}

		/// <summary>
		/// Generate Spinel INI using the specified parameters
		/// </summary>
		/// <param name="bon">BonDriver</param>
		/// <param name="modelName">Tuner model</param>
		/// <param name="modelIndex">Tuner modelIndex</param>
		/// <param name="signalType">Signal type</param>
		/// <param name="signalIndex">Signal index</param>
		/// <param name="timeout">Timeout duration</param>
		/// <returns>Spinel INI content</returns>
		public static string GenerateINIContent4DLL(
			BonDriverSpinel bon,
			string modelName,
			int modelIndex,
			string signalType,
			int signalIndex,
			int timeout = 10
			)
		{
			string result = "";
			result += $"[BonDriver_Spinel]\r\nIniVersion = 1\r\n";
			result += $"Address = \"{bon.IP}:{bon.Port}\"\r\n";
			result += $"TunerPath = \"{modelName}/{modelIndex}/{signalType}/{signalIndex}\"\r\n";
			result += $"RequireExclusiveChannelControl = {(REQUIRE_EXCLUSIVE_CHANNEL_CONTROL ? 1 : 0)}\r\n";
			result += "ForceTCPDataLinkMode = 0\r\n";
			result += "EnableHostProcessAliveCheck = 0\r\n";
			result += $"ConnectTimeoutSeconds = {timeout}\r\n";
			result += "DesiredDescrambleControl = 2";
			return result;
		}

		static List<string> GenerateIpListFromCidr(string cidr, bool excludeNetworkAndBroadcast = false)
		{
			// Split CIDR and subnet mask
			string[] parts = cidr.Split('/');
			if (parts.Length != 2)
				throw new ArgumentException("Invalid CIDR format");

			string baseIp = parts[0];
			int prefixLength = int.Parse(parts[1]);

			// Calculate the subnet mask and address range
			uint mask = ~(uint.MaxValue >> prefixLength); // Subnet mask
			uint ip = IpToUint(baseIp); // Starting IP as an integer
			uint startIp = ip & mask;   // First IP in the subnet (network address)
			uint endIp = startIp | ~mask; // Last IP in the subnet (broadcast address)

			// Generate the IP list
			var ipList = new List<string>();
			for (uint currentIp = startIp; currentIp <= endIp; currentIp++)
			{
				ipList.Add(UintToIp(currentIp));
			}

			// Exclude the network and broadcast addresses
			if (excludeNetworkAndBroadcast && ipList.Count > 2)
			{
				ipList.RemoveAt(0); // Remove the network address
				ipList.RemoveAt(ipList.Count - 1); // Remove the broadcast address
			}

			return ipList;
		}

		static uint IpToUint(string ipAddress)
		{
			byte[] bytes = IPAddress.Parse(ipAddress).GetAddressBytes();
			Array.Reverse(bytes); // Convert byte order to little-endian
			return BitConverter.ToUInt32(bytes, 0);
		}

		static string UintToIp(uint ip)
		{
			byte[] bytes = BitConverter.GetBytes(ip);
			Array.Reverse(bytes); // Convert little-endian back to big-endian
			return new IPAddress(bytes).ToString();
		}
	}
}

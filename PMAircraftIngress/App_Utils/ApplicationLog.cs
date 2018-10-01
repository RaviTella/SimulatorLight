using PMAircraftIngress.Context;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace PMAircraftIngress.App_Utils
{
	internal class ApplicationLog
	{
		private string LogPath
		{
			get;
			set;
		}

		protected ApplicationLog(string logPath)
		{
			this.LogPath = logPath;
		}

		public static ApplicationLog AcquireLog(IngressContext context)
		{
			ApplicationLog returnValue = null;
			try
			{
				string logPath = context.ApplicationLogPath;
				if (!string.IsNullOrEmpty(logPath))
				{
					DateTime now = DateTime.Now;
					string logName = string.Format("{0}.log", now.ToString("MMddyyy_HHmmss"));
					if (logPath.StartsWith("."))
					{
						string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
						logPath = logPath.TrimStart(new char[] { '.', '/', '\\' });
						logPath = Path.Combine(appPath, logPath);
					}
					if (!Directory.Exists(logPath))
					{
						Directory.CreateDirectory(logPath);
					}
					logPath = Path.Combine(logPath, logName);
					returnValue = new ApplicationLog(logPath);
				}
			}
			catch (Exception exception)
			{
			}
			return returnValue;
		}

		public void Report(string reportData)
		{
			this.Report(reportData, null);
		}

		public void Report(string format, params object[] formatObjects)
		{
			string logData = format;
			if (formatObjects != null)
			{
				logData = string.Format(format, formatObjects);
			}
			DateTime now = DateTime.Now;
			logData = string.Format("{0}\t{1}", now.ToString("HH:mm:ss:fff"), logData);
			using (StreamWriter writer = new StreamWriter(this.LogPath, true))
			{
				writer.WriteLine(logData);
			}
		}
	}
}
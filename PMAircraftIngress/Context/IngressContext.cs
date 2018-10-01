using Newtonsoft.Json;
using PMAircraftIngress.App_Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PMAircraftIngress.Context
{
	public class IngressContext
	{
		public int AdfBlobAgeMinutes
		{
			get;
			set;
		}

		public string AdfContainer
		{
			get;
			set;
		}

		public string ApplicationLogPath
		{
			get;
			set;
		}

		[JsonIgnore]
		private ApplicationLog AppLog
		{
			get;
			set;
		}

		[JsonIgnore]
		public string Error
		{
			get;
			set;
		}

		public string EventFile
		{
			get;
			set;
		}

		public string EventHubConnectionString
		{
			get;
			set;
		}

		public string EventHubName
		{
			get;
			set;
		}

		[JsonIgnore]
		public IngressStateFlag IngressState
		{
			get;
			set;
		}

		public string SQLDatabase
		{
			get;
			set;
		}

		public string SQLPassword
		{
			get;
			set;
		}

		public string SQLServer
		{
			get;
			set;
		}

		public List<string> SQLTables
		{
			get;
			set;
		}

		public string SQLUserName
		{
			get;
			set;
		}

		public string StorageConnectionString
		{
			get;
			set;
		}
        public string IoTHubDeviceConnectionString { get; set; }
        public int SendFrequency { get; set; }

        public IngressContext()
		{
			this.SQLTables = new List<string>();
		}

		public static IngressContext GetContext()
		{
			IngressContext returnContext = null;
			try
			{
				string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				using (StreamReader reader = new StreamReader(Path.Combine(appPath, "Setting.json")))
				{
					returnContext = JsonConvert.DeserializeObject<IngressContext>(reader.ReadToEnd());
				}
				returnContext.AppLog = ApplicationLog.AcquireLog(returnContext);
			}
			catch (Exception exception)
			{
				returnContext = new IngressContext()
				{
					Error = exception.Message
				};
			}
			returnContext.IngressState = IngressStateFlag.Idle;
			return returnContext;
		}

		public void ReportStatus(string status)
		{
			if (this.AppLog != null)
			{
				this.AppLog.Report(status);
			}
			if (this.OnUpdateStatus != null)
			{
				this.OnUpdateStatus(status);
			}
		}

		public bool Validate()
		{
			if (!string.IsNullOrEmpty(this.AdfContainer) && !string.IsNullOrEmpty(this.EventFile) && !string.IsNullOrEmpty(this.EventHubConnectionString) && !string.IsNullOrEmpty(this.EventHubName) && !string.IsNullOrEmpty(this.SQLDatabase) && !string.IsNullOrEmpty(this.SQLPassword) && !string.IsNullOrEmpty(this.SQLServer) && !string.IsNullOrEmpty(this.SQLUserName) && !string.IsNullOrEmpty(this.StorageConnectionString) && this.SQLTables.Count > 0)
			{
				return true;
			}
			return false;
		}

		public event OnUpdateStatusHandler OnUpdateStatus;
	}
}
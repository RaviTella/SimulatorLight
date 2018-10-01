using Microsoft.Azure.Devices.Client;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using PMAircraftIngress.Context;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace PMAircraftIngress
{

	internal class IngressUtil
	{
        //static string DeviceConnectionString = "";

        static DeviceClient Client = null;

       
        private System.Threading.CancellationTokenSource CancellationTokenSource
		{
			get;
			set;
		}

		private IngressContext Context
		{
			get;
			set;
		}

		public IngressUtil(IngressContext context)
		{
			this.Context = context;
		}

		public void Cancel()
		{
			if (this.Context.IngressState == IngressStateFlag.InProgress)
			{
				this.CancellationTokenSource.Cancel();
			}
		}

		public Stream GetDataStream()
		{
			Stream returnValue = null;
			try
			{
				string dataFile = this.Context.EventFile;
				if (dataFile.StartsWith("."))
				{
					string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
					dataFile = dataFile.TrimStart(new char[] { '.', '/', '\\' });
					dataFile = Path.Combine(appPath, dataFile);
				}
				this.Context.ReportStatus(string.Format("Data File: {0}", dataFile));
				returnValue = new FileStream(dataFile, FileMode.Open);
			}
			catch (Exception exception)
			{
				Exception ex = exception;
				this.Context.ReportStatus(string.Format("Can not open {0}", this.Context.EventFile));
				this.Context.ReportStatus(string.Format("\t{0}", ex.Message));
				returnValue = null;
			}
			return returnValue;
		}

		private void PerformCleanup()
		{
			try
			{
				(new SQLCleaner()).ConnectAndQuery(this.Context);
			}
			catch (Exception exception)
			{
			}
			try
			{
				(new StorageCleaner(this.Context)).CleanAgedBlobs();
			}
			catch (Exception exception1)
			{
			}
		}

		public void Run()
		{
			if (this.Context.IngressState == IngressStateFlag.Idle)
			{
				this.CancellationTokenSource = new System.Threading.CancellationTokenSource();
				ThreadPool.QueueUserWorkItem(new WaitCallback(this.Upload), this.CancellationTokenSource.Token);
			}
		}


		private void Upload(object cToken)
		{
			CancellationToken cancellationToken = (CancellationToken)cToken;
			this.Context.ReportStatus("Upload Task starting");
            Stream contentStream = this.GetDataStream(); // TLC - read dataFile (EventFile in the Settings.json file
			if (contentStream == null)
			{
				this.Context.ReportStatus("Cannot access the content to stream to the EventHub");
				this.CancellationTokenSource.Cancel();
			}
			using (contentStream)
			{
				try
				{
                    Client = DeviceClient.CreateFromConnectionString(this.Context.IoTHubDeviceConnectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt);
                   // EventHubClient client = EventHubClient.CreateFromConnectionString(this.Context.EventHubConnectionString, this.Context.EventHubName);
                
					using (StreamReader contentReader = new StreamReader(contentStream))
					{
						while (!cancellationToken.IsCancellationRequested)
						{
                            // TLC - Perform Cleanup calls: SQLCleaner.ConnectAndQuery
                            //          StorageCleaner.CleanAgedBlobs - We don't need this...
							//this.PerformCleanup();
							string streamContentString = contentReader.ReadLine();
							if (!string.IsNullOrEmpty(streamContentString))
							{
								char[] chrArray = new char[] { ',' };
								CSVHeaderHelper hdrHelper = new CSVHeaderHelper(streamContentString.Split(chrArray, StringSplitOptions.RemoveEmptyEntries));
                                // TLC - 04/30/18.  Try to make this somewhat flexible???, first column will be the 
                                //                  "id" - we will batch all events by this identifier...  CRow is counter...
                                int counterIdx = 0; //  hdrHelper.GetIndex("Row"); 
                                
								string currentCounter = string.Empty;
								List<string> currentCounterData = new List<string>();
								while (!contentReader.EndOfStream)
								{
									string eventData = contentReader.ReadLine();
									char[] chrArray1 = new char[] { ',' };
									string[] data = eventData.Split(chrArray1, StringSplitOptions.None); // TLC StringSplitOptions.RemoveEmptyEntries);
									//if (string.Compare(currentCounter, data[counterIdx], StringComparison.CurrentCultureIgnoreCase) == 0)
                                    if (string.Compare(currentCounter, data[counterIdx], StringComparison.CurrentCultureIgnoreCase) == 0)
                                    {
										currentCounterData.Add(eventData);
									}
									else
									{
										if (currentCounterData.Count > 0)
										{
											this.Context.ReportStatus(string.Concat("Upload Task event uploaded for counter", currentCounter));
										}
										foreach (string uploadData in currentCounterData)
										{
											object[] str = new object[] { hdrHelper.ToString(), Environment.NewLine, uploadData, null };
											DateTime universalTime = DateTime.Now.ToUniversalTime();
											str[3] = universalTime.ToString("O");
											string payload = string.Format("processed,{0}{1}{3},{2}", str);
                                            Dictionary<string, string> telemetryDictionary = getTelemetryAsDictionary(payload);
                                            string telemetryJson = Newtonsoft.Json.JsonConvert.SerializeObject(telemetryDictionary);
                                            SendDeviceToCloudMessagesAsync(telemetryJson);
                                         //   client.Send(new EventData(Encoding.UTF8.GetBytes(telemetryJson)));
										}
                                        currentCounter = data[counterIdx];
										currentCounterData.Clear();
										currentCounterData.Add(eventData);
										Thread.Sleep(1000);
									}
									if (!cancellationToken.IsCancellationRequested)
									{
										continue;
									}
									this.Context.ReportStatus("Upload Task cancellation request");
									break;
								}
								contentReader.BaseStream.Position = (long)0;
							}
							else
							{
								this.Context.ReportStatus("Content stream does not contain a header......");
								this.CancellationTokenSource.Cancel();
								break;
							}
						}
					}
				}
				catch (Exception exception)
				{
					Exception ex = exception;
					this.Context.ReportStatus(string.Concat("Exception working with Event Hub : ", ex.Message));
					this.CancellationTokenSource.Cancel();
				}
			}
			this.Context.ReportStatus("Upload Task completed");
			if (this.OnJobComplete != null)
			{
				this.OnJobComplete();
			}
		}

        private static Dictionary<String, String> getTelemetryAsDictionary(String csvTelemetry)
        {
            var splitted = csvTelemetry.Split('\n').Select(s => s.Trim()).ToArray();
            var keys = splitted[0].Split(',');
            var values = splitted[1].Split(',');
            var telemetryAsDictionary = keys.Zip(values, (k, v) => new { k, v })
                          .ToDictionary(item => item.k, item => item.v);
            return telemetryAsDictionary;
        }

        public event OnJobCompletionHandler OnJobComplete;



        private async void SendDeviceToCloudMessagesAsync(String jsonMessage)
        {
                var message = new Message(Encoding.ASCII.GetBytes(jsonMessage));
                await Client.SendEventAsync(message);
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, jsonMessage);
               Console.WriteLine("Waiting for a SendFrequency of", this.Context.SendFrequency);
               Task.Delay(this.Context.SendFrequency).Wait();

        }

    }
}
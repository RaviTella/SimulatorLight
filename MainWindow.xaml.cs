using PMAircraftIngress.Context;
using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;


namespace PMAircraftIngress
{
	public partial class MainWindow : Window
	{
		private IngressContext AcceleratorContext
		{
			get;
			set;
		}

		private IngressUtil IngressUtility
		{
			get;
			set;
		}
        //TODO need to chnage this to add config into 
		public MainWindow()
		{
            this.InitializeComponent();
			this.AcceleratorContext = IngressContext.GetContext();
			this.AcceleratorContext.ReportStatus("Application Starting");
			this.AcceleratorContext.OnUpdateStatus += new OnUpdateStatusHandler(this.StatusUpdate);
			if (this.AcceleratorContext.Validate())
			{
				this.IngressUtility = new IngressUtil(this.AcceleratorContext);
				this.IngressUtility.OnJobComplete += new OnJobCompletionHandler(this.IngressJobComplete);
			}
			else
			{
				this.AcceleratorContext.Error = "Settings are invalid.";
			}
			this.btnIngestion.Click += new RoutedEventHandler(this.IngestionButtonClick);
			this.UpdateUploadSection(this.AcceleratorContext);
		}

		private void IngestionButtonClick(object sender, EventArgs e)
		{
			if (this.IngressUtility != null)
			{
				if (this.AcceleratorContext.IngressState != IngressStateFlag.AwaitingCancellation)
				{
					if (this.AcceleratorContext.IngressState != IngressStateFlag.InProgress)
					{
						this.StatusUpdate(null);
						this.StatusUpdate("Starting ingress process...");
						this.IngressUtility.Run();
						this.AcceleratorContext.IngressState = IngressStateFlag.InProgress;
					}
					else
					{
						this.StatusUpdate("Stopping ingress process...");
						this.IngressUtility.Cancel();
						this.AcceleratorContext.IngressState = IngressStateFlag.AwaitingCancellation;
					}
				}
				this.UpdateUploadSection(this.AcceleratorContext);
			}
		}

		private void IngressJobComplete()
		{
			this.AcceleratorContext.IngressState = IngressStateFlag.Idle;
			base.Dispatcher.BeginInvoke(new Action(() => this.UpdateUploadSection(this.AcceleratorContext)), new object[0]);
		}

		public void StatusUpdate(string data)
		{
			if (!string.IsNullOrEmpty(data))
			{
				base.Dispatcher.BeginInvoke(new Action(() => {
					this.lstStatus.Items.Add(data);
					this.ScrollResults.ScrollToEnd();
				}), new object[0]);
				return;
			}
			base.Dispatcher.BeginInvoke(new Action(() => this.lstStatus.Items.Clear()), new object[0]);
		}

		private void UpdateUploadSection(IngressContext context)
		{
			if (context == null || !string.IsNullOrEmpty(context.Error))
			{
				this.btnIngestion.Background = new SolidColorBrush(Colors.Gray);
				this.btnIngestion.Content = "App Error";
				if (!string.IsNullOrEmpty(context.Error))
				{
					this.StatusUpdate(context.Error);
					return;
				}
			}
			else
			{
				if (context.IngressState == IngressStateFlag.Idle)
				{
					this.btnIngestion.Background = new SolidColorBrush(Colors.Green);
					this.btnIngestion.Content = "Start Ingestion";
					return;
				}
				if (context.IngressState == IngressStateFlag.InProgress)
				{
					this.btnIngestion.Background = new SolidColorBrush(Colors.Red);
					this.btnIngestion.Content = "Stop Ingestion";
					return;
				}
				if (context.IngressState == IngressStateFlag.AwaitingCancellation)
				{
					this.btnIngestion.Background = new SolidColorBrush(Colors.Gray);
					this.btnIngestion.Content = "Waiting....";
				}
			}
		}

        private void btnIngestion_Click(object sender, RoutedEventArgs e)
        {

        }
	}
}
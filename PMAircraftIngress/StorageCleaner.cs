using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using PMAircraftIngress.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PMAircraftIngress
{
	internal class StorageCleaner
	{
		private CloudBlobClient BlobClient
		{
			get;
			set;
		}

		private IngressContext Context
		{
			get;
			set;
		}

		public StorageCleaner(IngressContext context)
		{
			this.Context = context;
			try
			{
				this.Context.ReportStatus("Creating Azure Storage object to clean blobs.");
				CloudStorageAccount storageAccount = CloudStorageAccount.Parse(context.StorageConnectionString);
				this.BlobClient = storageAccount.CreateCloudBlobClient();
			}
			catch (Exception exception)
			{
				Exception ex = exception;
				this.Context.ReportStatus(string.Format("Exception with Azure Storage object", new object[0]));
				this.Context.Error = ex.Message;
			}
		}

		public int CleanAgedBlobs()
		{
			int returnValue = 0;
			this.Context.ReportStatus(string.Format("Clearing stale blobs from container {0}", this.Context.AdfContainer));
			try
			{
				if (this.BlobClient != null && !string.IsNullOrEmpty(this.Context.AdfContainer) && this.Context.AdfBlobAgeMinutes != -1)
				{
					returnValue = this.CleanBlobs().Count<string>();
				}
			}
			catch (Exception exception)
			{
				Exception ex = exception;
				this.Context.ReportStatus("Failed to clean up stale blobs:");
				this.Context.ReportStatus(string.Format("\t{0}", ex));
			}
			return returnValue;
		}

		private IEnumerable<string> CleanBlobs()
		{
			List<string> returnValues = new List<string>();
			if (this.BlobClient != null)
			{
				CloudBlobContainer container = this.BlobClient.GetContainerReference(this.Context.AdfContainer);
				container.CreateIfNotExists(null, null);
				this.EnumerateBlobsCleanup(returnValues, container.ListBlobs(null, false, BlobListingDetails.None, null, null));
			}
			return returnValues;
		}

		private void EnumerateBlobsCleanup(List<string> returnValues, IEnumerable<IListBlobItem> items)
		{
			DateTime universalTime = DateTime.Now.ToUniversalTime();
			DateTime dtCutoff = universalTime.AddMinutes((double)(this.Context.AdfBlobAgeMinutes * -1));
			foreach (IListBlobItem blob in items)
			{
				if (!(blob is CloudBlockBlob))
				{
					if (!(blob is CloudBlobDirectory))
					{
						continue;
					}
					CloudBlobDirectory directory = blob as CloudBlobDirectory;
					this.EnumerateBlobsCleanup(returnValues, directory.ListBlobs(false, BlobListingDetails.None, null, null));
				}
				else
				{
					CloudBlockBlob blockBlob = blob as CloudBlockBlob;
					if (blockBlob.Properties.LastModified.GetValueOrDefault().UtcDateTime < dtCutoff)
					{
						blockBlob.Delete(DeleteSnapshotsOption.None, null, null, null);
					}
					returnValues.Add(blockBlob.Name);
				}
			}
		}
	}
}
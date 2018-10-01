using PMAircraftIngress.Context;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;

namespace PMAircraftIngress
{
	internal class SQLCleaner
	{
		private const int ConnectionTimeoutSeconds = 60;

		private const int MaxCountTriesConnectAndQuery = 3;

		private const int SecondsBetweenRetries = 4;

		private SqlConnection _sqlConnection;

		private SqlConnectionStringBuilder _scsBuilder;

		public SQLCleaner()
		{
		}

		private void ClearTables(IngressContext context)
		{
			SqlConnection sqlConnection = new SqlConnection(this._scsBuilder.ToString());
			SqlConnection sqlConnection1 = sqlConnection;
			this._sqlConnection = sqlConnection;
			using (sqlConnection1)
			{
				this._sqlConnection.Open();
				foreach (string table in context.SQLTables)
				{
					try
					{
						context.ReportStatus(string.Format("Clearing SQL table : {0}", table));
						this.IssueDeleteTableContentsCommand(table);
					}
					catch (Exception exception)
					{
						Exception ex = exception;
						context.ReportStatus("Error clearing table : ");
						context.ReportStatus(string.Format("\t{0}", ex.Message));
						break;
					}
				}
			}
		}

		public void ConnectAndQuery(IngressContext context)
		{
			try
			{
				string server = string.Format("tcp:{0}.database.windows.net,1433", context.SQLServer);
				context.ReportStatus(string.Format("Cleaning tables on server {0} for {1}", server, context.SQLUserName));
				this._scsBuilder = new SqlConnectionStringBuilder();
				this._scsBuilder["Server"] = server;
				this._scsBuilder["User ID"] = context.SQLUserName;
				this._scsBuilder["Password"] = context.SQLPassword;
				this._scsBuilder["Database"] = context.SQLDatabase;
				this._scsBuilder["Trusted_Connection"] = false;
				this._scsBuilder["Integrated Security"] = false;
				this._scsBuilder["Encrypt"] = true;
				this._scsBuilder["Connection Timeout"] = 60;
				for (int connectCount = 1; connectCount <= 3; connectCount++)
				{
					try
					{
						this.ClearTables(context);
						break;
					}
					catch (SqlException sqlException)
					{
						SqlException sqlExc = sqlException;
						if (!SqlTransientErrorDetection.IsTransientStatic(sqlExc))
						{
							context.ReportStatus(string.Format("Transient SQL error, retry : {0}", sqlExc.HResult));
							break;
						}
					}
					catch (Exception exception)
					{
						Exception exc = exception;
						context.ReportStatus("Permanent SQL error, quitting attempt to clean up");
						throw exc;
					}
					if (connectCount > 3)
					{
						throw new ApplicationException(string.Format("Transient errors suffered in too many retries ({0}). Will terminate.", connectCount - 1));
					}
					Thread.Sleep(4000);
				}
			}
			catch (Exception exception1)
			{
				context.Error = exception1.Message;
			}
		}

		private void IssueDeleteTableContentsCommand(string tableName)
		{
			IDbCommand dbCommand = null;
			int commandTimeout = 120;
			SqlCommand sqlCommand = this._sqlConnection.CreateCommand();
			dbCommand = sqlCommand;
			using (sqlCommand)
			{
				dbCommand.CommandTimeout = commandTimeout;
				dbCommand.CommandText = string.Format("delete from {0};", tableName);
				using (IDataReader dataReader = dbCommand.ExecuteReader())
				{
				}
			}
		}
	}
}
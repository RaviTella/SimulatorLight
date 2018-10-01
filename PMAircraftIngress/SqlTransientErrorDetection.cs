using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace PMAircraftIngress
{
	public class SqlTransientErrorDetection
	{
		private static List<int> _TransientErrorNumbers;

		static SqlTransientErrorDetection()
		{
			SqlTransientErrorDetection._TransientErrorNumbers = new List<int>(new int[] { 4060, 10928, 10929, 40197, 40501, 40613 });
		}

		public SqlTransientErrorDetection()
		{
		}

		public static bool IsTransientStatic(Exception exc)
		{
			bool returnValue = false;
			if (exc is SqlException)
			{
				returnValue = SqlTransientErrorDetection._TransientErrorNumbers.Contains((exc as SqlException).Number);
			}
			return returnValue;
		}
	}
}
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PMAircraftIngress
{
	internal class CSVHeaderHelper
	{
		private List<string> Header
		{
			get;
			set;
		}

		public CSVHeaderHelper(string[] header)
		{
			this.Header = new List<string>();
			string[] strArrays = header;
			for (int i = 0; i < (int)strArrays.Length; i++)
			{
				string hdr = strArrays[i];
				this.Header.Add(hdr.Replace('\"', ' ').Trim());
			}
		}

		public int GetIndex(string header)
		{
			return this.Header.IndexOf(header);
		}

		public override string ToString()
		{
			return string.Join(",", this.Header);
		}
	}
}
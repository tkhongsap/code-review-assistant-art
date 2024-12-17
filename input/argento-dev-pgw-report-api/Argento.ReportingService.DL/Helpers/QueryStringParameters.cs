using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.DL.Helpers
{
	public class QueryStringParameters
    {
		public int maxPageSize { get; set; } = 100000;
		public int Page { get; set; } = 1;

		private int _pageSize = 20;
		public int PageSize
		{
			get
			{
				return _pageSize;
			}
			set
			{
				_pageSize = value;
			}
		}
	}
}

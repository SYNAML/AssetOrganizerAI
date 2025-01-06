using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVolt.AI.Core.Configurations
{
	public class Asset
	{
		public string FilePath { get; set; }
		public string Description { get; set; }
		public List<string> Keywords { get; set; } = new List<string>();
	}
}

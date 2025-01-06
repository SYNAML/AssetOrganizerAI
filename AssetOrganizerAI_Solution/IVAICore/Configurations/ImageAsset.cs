using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IVolt.AI.Core.Enum;

namespace IVolt.AI.Core.Configurations
{
	public class ImageAsset : Asset
	{
	
		public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
		public List<string> Tags { get; set; } = new List<string>();
		public int Width { get; set; }
		public int Height { get; set; }
		public string Format { get; set; }
		public List<PrimaryColor> PrimaryColors { get; set; } = new List<PrimaryColor>();

	}
}

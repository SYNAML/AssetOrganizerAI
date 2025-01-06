using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVolt.AI.Core.Configurations
{
	public class AgentConfiguration
	{
		public bool UseGPU { get; set; }
		public string AssetsDirectory { get; set; }
		public string ImageModelPath { get; set; }
		public string TextModelPath { get; set; }
	}
}

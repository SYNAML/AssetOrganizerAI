using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVolt.AI.Core.Utils
{

		public static class ConfigurationWizard
		{
			public static AppConfig Run()
			{
				var config = new AppConfig();

				Console.WriteLine("\nConfiguration Wizard");
				Console.WriteLine("--------------------");

				// GPU Usage
				Console.Write("Use GPU for AI processing? (yes/no): ");
				config.UseGPU = Console.ReadLine().Trim().ToLower() == "yes";

				// Asset Directory
				Console.Write("Enter the directory to scan for assets: ");
				config.AssetsDirectory = Console.ReadLine();

				// Model Paths
				Console.Write("Enter path to image model (default: models/image_model.onnx): ");
				var imageModelPath = Console.ReadLine();
				config.ImageModelPath = string.IsNullOrWhiteSpace(imageModelPath) ? "models/image_model.onnx" : imageModelPath;

				Console.Write("Enter path to text model (default: models/text_model.onnx): ");
				var textModelPath = Console.ReadLine();
				config.TextModelPath = string.IsNullOrWhiteSpace(textModelPath) ? "models/text_model.onnx" : textModelPath;

				return config;
			}
		}

		
	

}

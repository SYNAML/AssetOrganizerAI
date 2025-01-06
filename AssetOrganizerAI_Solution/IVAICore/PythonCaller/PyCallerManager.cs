using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVolt.AI.Core.PythonCaller
{
	public  class PyCallerManager
	{
		public void DebugRun(int MaxWorkers = 2,string pythonExe = @"C:\Path\To\python.exe",string scriptPath = @"C:\path\to\analyze_images.py",string imagesDirectory = @"C:\path\to\images")
		{		 			

			var startInfo = new ProcessStartInfo
			{
				FileName = pythonExe,
				Arguments = $"\"{scriptPath}\" \"{imagesDirectory}\" --max-workers " + MaxWorkers.ToString(),
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};

			using var process = new Process { StartInfo = startInfo };
			process.Start();

			// Read the JSON results from stdout
			string jsonOutput = process.StandardOutput.ReadToEnd();
			string errorOutput = process.StandardError.ReadToEnd();

			process.WaitForExit();

			if (process.ExitCode != 0)
			{
				Console.WriteLine("Python script error:");
				Console.WriteLine(errorOutput);
			}
			else
			{
				// Now parse the JSON to any structure you want
				Console.WriteLine("Received JSON:");
				Console.WriteLine(jsonOutput);

				// For example, you could parse with System.Text.Json or Newtonsoft.Json.
				// Then iterate over the results and store them in a database.
			}
		}
	}
}

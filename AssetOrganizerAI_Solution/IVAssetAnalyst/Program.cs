using IVolt.AI.Core.Configurations;
using IVolt.AI.Core.AIControls;
using IVolt.AI.Core.Utils;
using System;


namespace IVolt.AI.Applications.AssetAnalyst
{
    internal class Program
    {
		static void Main(string[] args)
		{
			Console.WriteLine("Welcome to File Organizer!");

			// Run the configuration wizard
			var config = ConfigurationWizard.Run();

			// Initialize AI Manager based on configuration
			var aiManager = new AI.Core.AI.AIManager(config.ImageModelPath, config.TextModelPath, config.UseGPU);

			// Initialize Asset Service
			var assetService = new AssetService(aiManager, config.AssetsDirectory);

			Console.WriteLine("\nStarting asset processing...");
			assetService.ScanAndProcessAssets();

			Console.WriteLine("\nSetup complete. You can now search your assets.");
			Console.WriteLine("Enter a keyword to search or type 'exit' to quit.");

			while (true)
			{
				Console.Write("Search: ");
				var query = Console.ReadLine();
				if (query.Equals("exit", StringComparison.OrdinalIgnoreCase))
					break;

				var results = assetService.SearchAssets(query);
				foreach (var result in results)
				{
					Console.WriteLine($"- {result.FilePath}: {result.Description}");
				}
			}

			Console.WriteLine("Goodbye!");
		}
	}
}

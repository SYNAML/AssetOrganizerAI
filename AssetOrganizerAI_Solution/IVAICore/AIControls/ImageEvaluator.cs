using System;
using System.Collections.Generic;
using Draw = System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using IVolt.AI.Core.Configurations;
using IVolt.AI.Core.Enum;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

using static System.Net.Mime.MediaTypeNames;

namespace IVolt.AI.Core.AIControls
{



	public class ImageEvaluator
	{
		private readonly InferenceSession _session;

		public ImageEvaluator(string modelPath, bool useGPU)
		{
			var options = new SessionOptions();
			if (useGPU)
			{
				options.AppendExecutionProvider_CUDA();
			}

			_session = new InferenceSession(modelPath, options);
		}

		public ImageAsset EvaluateImage(string imagePath)
		{
			if (!File.Exists(imagePath))
				throw new FileNotFoundException($"File not found: {imagePath}");

			Console.WriteLine($"Evaluating image: {imagePath}");

			// Extract metadata
			var metadata = ExtractMetadata(imagePath);

			// Extract basic properties
			var (width, height, format, primaryColors) = ExtractImageProperties(imagePath);

			// Preprocess and infer tags/keywords
			var image = PreprocessImage(imagePath);
			var tags = InferTags(image);

			// Create EvaluatedImage object
			return new ImageAsset
			{
				FilePath = imagePath,
				Metadata = metadata,
				Width = width,
				Height = height,
				Format = format,
				PrimaryColors = primaryColors,
				Tags = tags,
				Keywords = GenerateKeywords(tags, metadata),
				Description = GenerateDescription(tags)
			};
		}

		private Dictionary<string, string> ExtractMetadata(string imagePath)
		{
			var metadata = new Dictionary<string, string>();

			try
			{
				var directories = ImageMetadataReader.ReadMetadata(imagePath);
				foreach (var directory in directories)
				{
					foreach (var tag in directory.Tags)
					{
						metadata[tag.Name] = tag.Description;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to extract metadata: {ex.Message}");
			}

			return metadata;
		}

		private (int width, int height, string format, List<PrimaryColor> primaryColors) ExtractImageProperties(string imagePath)
		{
			using (var image = Draw.Image.FromFile(imagePath))
			{
				int width = image.Width;
				int height = image.Height;
				string format = ImageFormatToString(image.RawFormat);
				var primaryColors = GetDominantColors(image);

				return (width, height, format, primaryColors);
			}
		}

		private string ImageFormatToString(ImageFormat format)
		{
			if (format.Equals(ImageFormat.Jpeg)) return "JPEG";
			if (format.Equals(ImageFormat.Png)) return "PNG";
			if (format.Equals(ImageFormat.Bmp)) return "BMP";
			if (format.Equals(ImageFormat.Gif)) return "GIF";
			if (format.Equals(ImageFormat.Tiff)) return "TIFF";
			return "Unknown";
		}

		private List<PrimaryColor> GetDominantColors(Draw.Image image)
		{
			var colorCount = new Dictionary<PrimaryColor, int>();
			foreach (PrimaryColor color in System.Enum.GetValues(typeof(PrimaryColor)))
			{
				colorCount[color] = 0;
			}

			using (var bitmap = new Draw.Bitmap(image))
			{
				for (int y = 0;y < bitmap.Height;y++)
				{
					for (int x = 0;x < bitmap.Width;x++)
					{
						Draw.Color pixelColor = bitmap.GetPixel(x, y);
						var closestColor = MapToPrimaryColor(pixelColor);
						colorCount[closestColor]++;
					}
				}
			}

			// Determine the most dominant colors
			var dominantColors = new List<PrimaryColor>();
			int totalPixels = image.Width * image.Height;
			foreach (var kvp in colorCount)
			{
				if (kvp.Value > 0.05 * totalPixels) // Colors must represent at least 5% of the image
				{
					dominantColors.Add(kvp.Key);
				}
			}

			return dominantColors;
		}

		private PrimaryColor MapToPrimaryColor(Draw.Color color)
		{
			// Map a color to the closest primary color using Euclidean distance
			var primaryColors = new Dictionary<PrimaryColor, Draw.Color>
				{
					 { PrimaryColor.Red, Draw.Color.FromArgb(255, 0, 0) },
					 { PrimaryColor.Green, Draw.Color.FromArgb(0, 255, 0) },
					 { PrimaryColor.Blue, Draw.Color.FromArgb(0, 0, 255) },
					 { PrimaryColor.Yellow, Draw.Color.FromArgb(255, 255, 0) },
					 { PrimaryColor.Cyan,Draw.Color.FromArgb(0, 255, 255) },
					 { PrimaryColor.Magenta,Draw.Color.FromArgb(255, 0, 255) },
					 { PrimaryColor.Orange,Draw.Color.FromArgb(255, 165, 0) },
					 { PrimaryColor.Purple,Draw.Color.FromArgb(128, 0, 128) },
					 { PrimaryColor.Pink,Draw.Color.FromArgb(255, 192, 203) },
					 { PrimaryColor.Brown,Draw.Color.FromArgb(139, 69, 19) },
					 { PrimaryColor.Gray,Draw.Color.FromArgb(128, 128, 128) },
					 { PrimaryColor.Black,Draw.Color.FromArgb(0, 0, 0) },
					 { PrimaryColor.White,Draw.Color.FromArgb(255, 255, 255) },
					 { PrimaryColor.DarkRed,Draw.Color.FromArgb(139, 0, 0) },
					 { PrimaryColor.DarkGreen,Draw.Color.FromArgb(0, 100, 0) },
					 { PrimaryColor.DarkBlue,Draw.Color.FromArgb(0, 0, 139) },
					 { PrimaryColor.LightRed,Draw.Color.FromArgb(255, 182, 193) },
					 { PrimaryColor.LightGreen,Draw.Color.FromArgb(144, 238, 144) },
					 { PrimaryColor.LightBlue,Draw.Color.FromArgb(173, 216, 230) },
					 { PrimaryColor.LightYellow,Draw.Color.FromArgb(255, 255, 224) },
					 { PrimaryColor.LightCyan,Draw.Color.FromArgb(224, 255, 255) },
					 { PrimaryColor.LightMagenta,Draw.Color.FromArgb(255, 224, 255) },
					 { PrimaryColor.Beige,Draw.Color.FromArgb(245, 245, 220) },
					 { PrimaryColor.Olive,Draw.Color.FromArgb(128, 128, 0) },
					 { PrimaryColor.Teal,Draw.Color.FromArgb(0, 128, 128) }
				};

			PrimaryColor closest = PrimaryColor.Black;
			double minDistance = double.MaxValue;

			foreach (var kvp in primaryColors)
			{
				var dist = Math.Sqrt(
					 Math.Pow(color.R - kvp.Value.R, 2) +
					 Math.Pow(color.G - kvp.Value.G, 2) +
					 Math.Pow(color.B - kvp.Value.B, 2));

				if (dist < minDistance)
				{
					closest = kvp.Key;
					minDistance = dist;
				}
			}

			return closest;
		}

		private Draw.Bitmap PreprocessImage(string imagePath)
		{
			Draw.Bitmap image = new Draw.Bitmap(imagePath);
			return new Draw.Bitmap(image, new Draw.Size(224, 224)); // Resize for model input
		}

		private List<string> InferTags(Draw.Bitmap image)
		{
			var tensor = ImageToTensor(image);
			var inputs = new List<NamedOnnxValue>
				{
					 NamedOnnxValue.CreateFromTensor("input", tensor)
				};

			var tags = new List<string>();
			using (var results = _session.Run(inputs))
			{
				var output = results.First().AsEnumerable<float>();

				foreach (var value in output)
				{
					if (value > 0.5) // Assuming thresholding for valid tags
					{
						tags.Add($"Tag-{value.ToString("F2")}");
					}
				}
			}

			return tags;
		}

		private DenseTensor<float> ImageToTensor(Draw.Bitmap image)
		{
			var tensor = new DenseTensor<float>(new[] { 1, 3, 224, 224 });
			var data = tensor.Buffer.Span;

			int index = 0;
			for (int y = 0;y < image.Height;y++)
			{
				for (int x = 0;x < image.Width;x++)
				{
					Draw.Color pixel = image.GetPixel(x, y);

					// Normalize pixel values
					data[index++] = pixel.R / 255f;
					data[index++] = pixel.G / 255f;
					data[index++] = pixel.B / 255f;
				}
			}

			return tensor;
		}

		private List<string> GenerateKeywords(List<string> tags, Dictionary<string, string> metadata)
		{
			var keywords = new List<string>(tags);

			foreach (var meta in metadata)
			{
				if (!string.IsNullOrWhiteSpace(meta.Value))
				{
					keywords.Add(meta.Value.ToLower().Replace(" ", "_"));
				}
			}

			return keywords;
		}

		private string GenerateDescription(List<string> tags)
		{
			return $"This image likely contains: {string.Join(", ", tags)}.";
		}

		public void Dispose()
		{
			_session.Dispose();
		}
	}
}

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

namespace IVolt.AI.Core.AI
{
	public class AIManager
	{
		private readonly ImageRecognitionModel _imageModel;
		private readonly TextClassificationModel _textModel;

		public AIManager(string imageModelPath, string textModelPath)
		{
			_imageModel = new ImageRecognitionModel(imageModelPath);
			_textModel = new TextClassificationModel(textModelPath);
		}

		public (string Caption, List<string> Tags) ProcessImage(string imagePath)
		{
			return _imageModel.AnalyzeImage(imagePath);
		}

		public string ProcessText(string text)
		{
			return _textModel.ClassifyText(text);
		}
	}
}

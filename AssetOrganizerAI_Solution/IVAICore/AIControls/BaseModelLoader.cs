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
	public abstract class BaseModelLoader
	{
		private readonly InferenceSession _session;

		protected BaseModelLoader(string modelPath)
		{
			_session = new InferenceSession(modelPath);
		}

		protected IReadOnlyList<NamedOnnxValue> RunInference(Dictionary<string, object> inputs)
		{
			return _session.Run(inputs);
		}

		public void Dispose() => _session.Dispose();
	}
}

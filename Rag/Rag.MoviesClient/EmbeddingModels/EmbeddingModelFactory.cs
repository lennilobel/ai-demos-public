using System;

namespace Rag.MoviesClient.EmbeddingModels
{
	public static class EmbeddingModelFactory
	{
		public static EmbeddingModelType EmbeddingModelType { get; set; }

		static EmbeddingModelFactory()
		{
			var args = Environment.GetCommandLineArgs();

			EmbeddingModelType = args.Length > 2
				? (EmbeddingModelType)Enum.Parse(typeof(EmbeddingModelType), args[2], ignoreCase: true)
				: Shared.AppConfig.EmbeddingModel;
		}

		public static string GetDeploymentName()
		{
			switch (EmbeddingModelType)
			{
				case EmbeddingModelType.Default:
					return Shared.AppConfig.OpenAI.EmbeddingDeploymentNames.Default;

				case EmbeddingModelType.TextEmbedding3Large:
					return Shared.AppConfig.OpenAI.EmbeddingDeploymentNames.TextEmbedding3Large;

				case EmbeddingModelType.TextEmbedding3Small:
					return Shared.AppConfig.OpenAI.EmbeddingDeploymentNames.TextEmbedding3Small;

				case EmbeddingModelType.TextEmbeddingAda002:
					return Shared.AppConfig.OpenAI.EmbeddingDeploymentNames.TextEmbeddingAda002;
			}

			throw new NotSupportedException($"No deployment name is implemented for embedding model type {EmbeddingModelType}");
		}

	}
}

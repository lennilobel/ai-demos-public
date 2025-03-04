namespace Rag.AIClient.Engine.Config
{
	public class DemoConfig
	{
		// This class has demo settings that can be changed at runtime

		private static readonly DemoConfig _instance;

		public static DemoConfig Instance => _instance;

		static DemoConfig()
		{
			_instance = new DemoConfig();
		}

		public string Demeanor { get; set; } = "upbeat and friendly";   // Set the language tone of the AI responses
		//public string Demeanor { get; set; } = "upbeat, friendly, and verbose";   // Set the language tone of the AI responses
		public string ResponseLanguage { get; set; } = "English";       // Translate the natural language response to any other language
		public string IncludeDetails { get; set; } = "genre";           // Be specific about what movie info to be included in the response
		public bool NoEmojis { get; set; } = false;                     // Don't include emojies in the response
		public bool NoMarkdown { get; set; } = false;                   // Don't format markdown in the response
		public bool GeneratePosterImage { get; set; } = false;          // Generate a movie poster based on the response (DALL-E)
		public bool ShowInternalOperations { get; set; } = false;       // Display internal operations (completion messages, vector search)
	}
}

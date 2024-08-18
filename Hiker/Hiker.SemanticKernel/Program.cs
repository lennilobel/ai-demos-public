using System.Threading.Tasks;

namespace Hiker.SemanticKernel
{
	public static class Program
	{
		private static async Task Main(string[] args) =>
			await new Demo().PromptAndRun();

	}
}

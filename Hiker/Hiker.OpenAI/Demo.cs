using Hiker.Shared;
using System;
using System.Threading.Tasks;

namespace Hiker.OpenAI
{
	public class Demo : HikerDemoBase
	{
		protected override async Task<bool> RunDemo(string demoId)
		{
			if (demoId == "HB")
			{
				await base.RunDemo(async () => await new HikingBenefitsDemo().Run());
			}
			else if (demoId == "HH")
			{
				await base.RunDemo(async () => await new HikingHistoryDemo().Run());
			}
			else if (demoId == "HR")
			{
				await base.RunDemo(async () => await new HikingRecommendationDemo().Run());
			}
			else if (demoId == "HRT")
			{
				await base.RunDemo(async () => await new HikingRecommendationWithToolDemo().Run());
			}
			else if (demoId == "HI")
			{
				await base.RunDemo(async () => await new HikingImageDemo().Run());
			}
			else
			{
				return false;
			}

			return true;
		}

		protected override void ShowMenu()
		{
			Console.WriteLine(@"Hiker OpenAI Demos

HB  Hiking benefits
HH  Hiking history
HR  Hiking recommendation
HRT Hiking recommendation with tool
HI  Hiking image

Q   Quit
");
		}

	}
}

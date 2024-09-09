using Rag.AIClient.Engine.RagProviders.Base;
using Rag.AIClient.Engine.RagProviders.NoSql.CosmosDb;
using System;
using System.Text;

namespace Rag.AIClient.Engine.OregonPoc
{
	public class OregonPocDpmsAssistant : CosmosDbMoviesAssistant
    {
		public OregonPocDpmsAssistant(IRagProvider ragProvider)
            : base(ragProvider)
        {
        }

        protected override void ShowBanner()
        {
            Console.WriteLine(@"  ____  ____  __  __ ____       _    ___      _            _     _              _   ");
            Console.WriteLine(@" |  _ \|  _ \|  \/  / ___|     / \  |_ _|    / \   ___ ___(_)___| |_ __ _ _ __ | |_ ");
            Console.WriteLine(@" | | | | |_) | |\/| \___ \    / _ \  | |    / _ \ / __/ __| / __| __/ _` | '_ \| __|");
            Console.WriteLine(@" | |_| |  __/| |  | |___) |  / ___ \ | |   / ___ \\__ \__ \ \__ \ || (_| | | | | |_ ");
            Console.WriteLine(@" |____/|_|   |_|  |_|____/  /_/   \_\___| /_/   \_\___/___/_|___/\__\__,_|_| |_|\__|");
            Console.WriteLine();
        }

        protected override string[] Questions => [
            "Help me locate introduction work items submitted by Norman Chow, with a process status of \"Entry\".",
            "Find work items with session title 2021 Regular Session, submitted by Mark Johansen, with a process status of Completed, and a complexity of 0-None.",
            "I'm looking for Introduction work items where the primary attorney is \"McGee, Maureen\", and is assiogned to \"O'Brien, Anne\".",
        ];

        protected override string BuildChatPrompt()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"You are an assistant that helps Oregon Legislative Staff (aka LC) find work items in the LC docket.");
            sb.AppendLine($"Your demeanor is serious and professional; no exclamations.");
            sb.AppendLine($"Provide a user experience based on the following:");
            sb.AppendLine($"1. Each work item in the database has been vectorized using a text embedding model.");
            sb.AppendLine($"2. Users ask natural language questions which are also vectorized using a text embedding model.");
            sb.AppendLine($"3. A vector search is performed on the database, which returns the top 10 most relevant work items, based on similarity score (vector distance).");
            sb.AppendLine($"For each of the work items returned by the database, compose an explanation of the work item as follows:");
            sb.AppendLine($"1. Start each explanation with the work item number as the heading, prefixed by the work item number label (without adding any additional text).");
            sb.AppendLine($"2. Then supply the following details of each work item: Session Title, Work Item Type, Family Number, Primary Subject.");
            sb.AppendLine($"3. Also supply details for the fields that the user asked about in their question.");
            sb.AppendLine($"4. Do not supply any other additional details.");
            sb.AppendLine($"5. Provide the explanation in paragraph form, not as a list of fields and values.");
            sb.AppendLine($"6. Order the results from those that best answer the question to those that don't.");
            sb.AppendLine($"7. For results that don't fit the question, be sure to mention those criteria that didn't match.");
            sb.AppendLine($"When formatting the response, be sure to follow these guidelines:");
            sb.AppendLine($"1. Number each work item result.");
            sb.AppendLine($"2. Don't include emojis, because they won't render in my demo console application.");
            sb.AppendLine($"3. Don't include markdown syntax, because it won't render in my demo console application.");
            return sb.ToString();
        }

        protected override string BuildChatResponse(string question)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"The user asked:  '{question}'.");
            sb.AppendLine($"The database returned the following similarity results from a vector search:");

            return sb.ToString();
        }

        protected override string BuildImageGenerationPrompt()
        {
            var sb = new StringBuilder();

            return sb.ToString();
        }

        // Use the VectorDistance function to calculate a similarity score, and use TOP n with ORDER BY to retrieve the most relevant documents
		//  (by using a subquery, we only need to call VectorDistance once in the inner SELECT clause, and can reuse it in the outer ORDER BY clause)
		protected override string GetVectorSearchSql() =>
            @"
                  SELECT TOP 10
                    vd.id,
                    vd.Attachments,
                    vd.Docket,
                    vd.SessionTitle,
                    vd.SessionKey,
                    vd.WorkItemNumberLabel,
                    vd.WorkItemNumber,
                    vd.OriginalWorkItemNumber,
                    vd.WorkItemNumberSuffix,
                    vd.WorkItemType,
                    vd.SourceWorkItemNumber,
                    vd.SourceFamilyWorkItemNumber,
                    vd.FamilyNumber,
                    vd.PriorityRequestFlag,
                    vd.ActiveIssueCount,
                    vd.Submitter,
                    vd.SubmittedAt,
                    vd.ProcessStatus,
                    vd.StepProgress,
                    vd.AsapFlag,
                    vd.UrgentFlag,
                    vd.InactiveFlag,
                    vd.Complexity,
                    vd.PrimarySubject,
                    vd.SecondarySubjects,
                    vd.Recipients,
                    vd.RequiredChecksTotalCount,
                    vd.RequiredChecksDoneCount,
                    vd.RequiredChecks,
                    vd.IsFromRequestPortal,
                    vd.LastUpdatedAt,
                    vd.LastUpdatedBy,
                    vd.CreatedAt,
                    vd.EngrossingBasedOnWorkItemNumber,
                    vd.EngrossingAmendmentWorkItemNumber,
                    vd.ConfidentialFlag,
                    vd.CorrespondenceFlag,
                    vd.PubsType,
                    vd.DueFromPubsDate,
                    vd.FirmFlag,
                    vd.ExtraWorkFlag,
                    vd.OnlyShowAtTheRequestOf,
                    vd.ToBeScheduledUponReceipt,
                    vd.IntroducedPriorSessionFlag,
                    vd.AmendsCurrentLawFlag,
                    vd.IsPlaceholderDraftFlag,
                    vd.PresessionFlag,
                    vd.IsCorrectedFlag,
                    vd.RequestingChamber,
                    vd.EngrossingAction,
                    vd.EngrossingType,
                    vd.EngrossingLevel,
                    vd.IntentScopeFlag,
                    vd.ConstitutionalIssueFlag,
                    vd.AdoptedFlag,
                    vd.NegativeFlag,
                    vd.SpecialInstructions,
                    vd.ReadyForTransmittalFlag,
                    vd.NewExemptionFlag,
                    vd.MeasureNumber,
                    vd.MeasureNumberPrefix,
                    vd.MeasureNumberValue,
                    vd.RequestedEffectiveDateType,
                    vd.ApprovedDate,
                    vd.FiledDate,
                    vd.EffectiveDate,
                    vd.NinetyFirstDayFlag,
                    vd.ChapterNumber,
                    vd.VetoFlag,
                    vd.SingleItemVetoFlag,
                    vd.EClauseFlag,
                    vd.VetoNotes,
                    vd.OlcNotes,
                    vd.SimilarityScore
                FROM (
                    SELECT
                        c.id,
                        c.Attachments,
                        c.Docket,
                        c.SessionTitle,
                        c.SessionKey,
                        c.WorkItemNumberLabel,
                        c.WorkItemNumber,
                        c.OriginalWorkItemNumber,
                        c.WorkItemNumberSuffix,
                        c.WorkItemType,
                        c.SourceWorkItemNumber,
                        c.SourceFamilyWorkItemNumber,
                        c.FamilyNumber,
                        c.PriorityRequestFlag,
                        c.ActiveIssueCount,
                        c.Submitter,
                        c.SubmittedAt,
                        c.ProcessStatus,
                        c.StepProgress,
                        c.AsapFlag,
                        c.UrgentFlag,
                        c.InactiveFlag,
                        c.Complexity,
                        c.PrimarySubject,
                        c.SecondarySubjects,
                        c.Recipients,
                        c.RequiredChecksTotalCount,
                        c.RequiredChecksDoneCount,
                        c.RequiredChecks,
                        c.IsFromRequestPortal,
                        c.LastUpdatedAt,
                        c.LastUpdatedBy,
                        c.CreatedAt,
                        c.EngrossingBasedOnWorkItemNumber,
                        c.EngrossingAmendmentWorkItemNumber,
                        c.ConfidentialFlag,
                        c.CorrespondenceFlag,
                        c.PubsType,
                        c.DueFromPubsDate,
                        c.FirmFlag,
                        c.ExtraWorkFlag,
                        c.OnlyShowAtTheRequestOf,
                        c.ToBeScheduledUponReceipt,
                        c.IntroducedPriorSessionFlag,
                        c.AmendsCurrentLawFlag,
                        c.IsPlaceholderDraftFlag,
                        c.PresessionFlag,
                        c.IsCorrectedFlag,
                        c.RequestingChamber,
                        c.EngrossingAction,
                        c.EngrossingType,
                        c.EngrossingLevel,
                        c.IntentScopeFlag,
                        c.ConstitutionalIssueFlag,
                        c.AdoptedFlag,
                        c.NegativeFlag,
                        c.SpecialInstructions,
                        c.ReadyForTransmittalFlag,
                        c.NewExemptionFlag,
                        c.MeasureNumber,
                        c.MeasureNumberPrefix,
                        c.MeasureNumberValue,
                        c.RequestedEffectiveDateType,
                        c.ApprovedDate,
                        c.FiledDate,
                        c.EffectiveDate,
                        c.NinetyFirstDayFlag,
                        c.ChapterNumber,
                        c.VetoFlag,
                        c.SingleItemVetoFlag,
                        c.EClauseFlag,
                        c.VetoNotes,
                        c.OlcNotes,
                        VectorDistance(c.vectors, @vectors, false) AS SimilarityScore
                    FROM
                        c
                ) AS vd
                ORDER BY
                    vd.SimilarityScore DESC
			";

    }
}

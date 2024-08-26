CREATE PROCEDURE AskQuestion
	@Question varchar(max),
	@OpenAIEndpoint varchar(max),
	@OpenAIApiKey varchar(max),
	@OpenAIDeploymentName varchar(max)
AS
BEGIN

	DECLARE @Vectors varbinary(8000)

	EXEC VectorizeText
		@Question,
		@OpenAIEndpoint,
		@OpenAIApiKey,
		@OpenAIDeploymentName,
		@Vectors OUTPUT

	EXEC RunVectorSearch @Vectors

END

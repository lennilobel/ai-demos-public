CREATE PROCEDURE AskQuestion
	@Question varchar(max),
	@OpenAIEndpoint varchar(max),
	@OpenAIApiKey varchar(max),
	@OpenAIDeploymentName varchar(max)
AS
BEGIN

	DECLARE @Vectors VectorsUdt

	INSERT INTO @Vectors
		EXEC VectorizeText
			@Question,
			@OpenAIEndpoint,
			@OpenAIApiKey,
			@OpenAIDeploymentName

	EXEC RunVectorSearch @Vectors

END

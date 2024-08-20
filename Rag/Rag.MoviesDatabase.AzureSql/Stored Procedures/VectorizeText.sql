CREATE PROCEDURE VectorizeText
	@Text varchar(max)
AS
BEGIN

	DECLARE @Headers varchar(max) = JSON_OBJECT('api-key': '[API-KEY]')
	DECLARE @Payload varchar(max) = JSON_OBJECT('input': @Text)
	DECLARE @ReturnValue int
	DECLARE @Response nvarchar(max)

	EXEC @ReturnValue = sp_invoke_external_rest_endpoint
		@url = 'https://lenni-openai.openai.azure.com/openai/deployments/lenni-embeddings-3-large/embeddings?api-version=2023-03-15-preview',
		@method = 'POST',
		@headers = @Headers,
		@payload = @Payload,
		@response = @Response OUTPUT

	IF @ReturnValue != 0
		THROW 50000, @Response, 1

	DECLARE @QuestionVectors table (
		VectorValueId int,
		VectorValue float
	)

	SELECT
		VectorValueId = [key],
		VectorValue = value
	FROM
		OPENJSON(@Response, '$.result.data[0].embedding')

END

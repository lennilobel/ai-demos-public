CREATE PROCEDURE VectorizeText
	@Text varchar(max),
	@OpenAIEndpoint varchar(max),
	@OpenAIApiKey varchar(max),
	@OpenAIDeploymentName varchar(max),
	@Vectors varbinary(8000) OUTPUT
AS
BEGIN

	DECLARE @Url varchar(max) = CONCAT(@OpenAIEndpoint, 'openai/deployments/', @OpenAIDeploymentName, '/embeddings?api-version=2023-03-15-preview')
	DECLARE @Headers varchar(max) = JSON_OBJECT('api-key': @OpenAIApiKey)
	DECLARE @Payload varchar(max) = JSON_OBJECT('input': @Text)
	DECLARE @Response nvarchar(max)
	DECLARE @ReturnValue int

	EXEC @ReturnValue = sp_invoke_external_rest_endpoint
		@url = @Url,
		@method = 'POST',
		@headers = @Headers,
		@payload = @Payload,
		@response = @Response OUTPUT

	IF @ReturnValue != 0
		THROW 50000, @Response, 1

	DECLARE @VectorsJson nvarchar(max) = JSON_QUERY(@Response, '$.result.data[0].embedding');

	SET @Vectors = JSON_ARRAY_TO_VECTOR(@VectorsJson);

END

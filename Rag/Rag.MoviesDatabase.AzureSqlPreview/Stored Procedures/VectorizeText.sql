CREATE PROCEDURE VectorizeText
	@Text varchar(max),
	@Vector vector(1536) OUTPUT	-- *Preview*
AS
BEGIN

	DECLARE @OpenAIEndpoint varchar(max)		= (SELECT ConfigValue FROM AppConfig WHERE ConfigKey = 'OpenAIEndpoint')
	DECLARE @OpenAIApiKey varchar(max)			= (SELECT ConfigValue FROM AppConfig WHERE ConfigKey = 'OpenAIApiKey')
	DECLARE @OpenAIDeploymentName varchar(max)	= (SELECT ConfigValue FROM AppConfig WHERE ConfigKey = 'OpenAIDeploymentName')

	DECLARE @Url varchar(max) = CONCAT(@OpenAIEndpoint, 'openai/deployments/', @OpenAIDeploymentName, '/embeddings?api-version=2023-03-15-preview')
	DECLARE @Headers varchar(max) = JSON_OBJECT('api-key': @OpenAIApiKey)
	DECLARE @Payload varchar(max) = JSON_OBJECT('input': @Text, 'dimensions': 1536)
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

	DECLARE @VectorJson nvarchar(max) = JSON_QUERY(@Response, '$.result.data[0].embedding')

	SET @Vector = CONVERT(vector(1536), @VectorJson)

END

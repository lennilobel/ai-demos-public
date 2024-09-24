CREATE PROCEDURE VectorizeText
	@Text varchar(max)
AS
BEGIN

	DECLARE @OpenAIEndpoint varchar(max)		= (SELECT ConfigValue FROM AppConfig WHERE ConfigKey = 'OpenAIEndpoint')
	DECLARE @OpenAIApiKey varchar(max)			= (SELECT ConfigValue FROM AppConfig WHERE ConfigKey = 'OpenAIApiKey')
	DECLARE @OpenAIDeploymentName varchar(max)	= (SELECT ConfigValue FROM AppConfig WHERE ConfigKey = 'OpenAIDeploymentName')

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

	SELECT
		VectorValueId = [key] + 1,
		VectorValue = value
	FROM
		OPENJSON(@Response, '$.result.data[0].embedding')
	ORDER BY
		VectorValueId

END

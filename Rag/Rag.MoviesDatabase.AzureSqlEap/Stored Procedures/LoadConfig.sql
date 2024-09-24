CREATE PROCEDURE LoadConfig
	@OpenAIEndpoint varchar(max),
	@OpenAIApiKey varchar(max),
	@OpenAIDeploymentName varchar(max)
AS
BEGIN

	SET NOCOUNT ON

	RAISERROR('Populating AppConfig table', 0, 1) WITH NOWAIT

	INSERT INTO AppConfig VALUES
	 ('OpenAIEndpoint', @OpenAIEndpoint),
	 ('OpenAIApiKey', @OpenAIApiKey),
	 ('OpenAIDeploymentName', @OpenAIDeploymentName)

END

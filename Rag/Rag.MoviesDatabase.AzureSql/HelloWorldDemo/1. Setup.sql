-- Connect to Azure SQL Database MovieDemo

/*
	*** Setup ***
*/

SET NOCOUNT ON
GO

CREATE SCHEMA HelloWorld
GO

CREATE TABLE HelloWorld.Movie (
	MovieId int IDENTITY,
	Title varchar(50)
)

INSERT INTO HelloWorld.Movie VALUES
	('Return of the Jedi'),
	('The Godfather'),
	('Animal House'),
	('The Two Towers')

CREATE TABLE HelloWorld.MovieVector (
	MovieId int,
	VectorValueId int,
	VectorValue float
)

GO
CREATE PROCEDURE HelloWorld.VectorizeText
	@Text varchar(max)
AS
BEGIN

	DECLARE @OpenAIEndpoint varchar(max) = '[OPENAI-ENDPOINT]'
	DECLARE @OpenAIApiKey varchar(max) = '[OPENAI-API-KEY]'
	DECLARE @OpenAIDeploymentName varchar(max) = '[OPENAI-DEPLOYMENT-NAME]'

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

GO
CREATE PROCEDURE HelloWorld.AskQuestion
	@Question varchar(max)
AS
BEGIN

	DECLARE @QuestionVectors table (
		VectorValueId int,
		VectorValue float
	)

	INSERT INTO @QuestionVectors
		EXEC HelloWorld.VectorizeText @Question

	SELECT TOP 1
		Question = @Question,
		Answer = m.Title,
		CosineDistance =
			SUM(qv.VectorValue * mv.VectorValue) /	-- https://github.com/Azure-Samples/azure-sql-db-openai/blob/classic/vector-embeddings/05-find-similar-articles.sql
		    (
		        SQRT(SUM(qv.VectorValue * qv.VectorValue)) 
		        * 
		        SQRT(SUM(mv.VectorValue * mv.VectorValue))
		    )
	FROM
		@QuestionVectors AS qv
		INNER JOIN HelloWorld.MovieVector AS mv on qv.VectorValueId = mv.VectorValueId
		INNER JOIN HelloWorld.Movie AS m ON m.MovieId = mv.MovieId
	GROUP BY
		mv.MovieId,
		m.Title
	ORDER BY
		CosineDistance DESC

END

-- Connect to Azure SQL Database MovieDemo

/*
	*** Setup ***
*/

CREATE TABLE Movie (
	MovieId int IDENTITY,
	Title varchar(50)
)

INSERT INTO Movie VALUES
	('Return of the Jedi'),
	('The Godfather'),
	('Animal House'),
	('The Two Towers')

CREATE TABLE MovieVector (
	MovieId int,
	VectorValueId int,
	VectorValue float
)

GO
CREATE PROCEDURE VectorizeText
	@Text varchar(max)
AS
BEGIN

	DECLARE @OpenAIEndpoint varchar(max) = '[OPENAI-ENDPOINT]'
	DECLARE @OpenAIApiKey varchar(max) = '[OPENAI-API-KEY]'
	DECLARE @OpenAIDeploymentName varchar(max) = '[OPENAI-DEPLOYMENT-NAME]'

	DECLARE @Url varchar(max) = CONCAT('https://', @OpenAIEndpoint, 'openai/deployments/', @OpenAIDeploymentName, '/embeddings?api-version=2023-03-15-preview')
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
CREATE PROCEDURE AskQuestion
	@Question varchar(max)
AS
BEGIN

	DECLARE @QuestionVectors table (
		VectorValueId int,
		VectorValue float
	)

	INSERT INTO @QuestionVectors
		EXEC VectorizeText @Question

	SELECT TOP 1
		Question = @Question,
		Answer = m.Title,
		CosineDistance = SUM(qv.VectorValue * mv.VectorValue)	-- https://github.com/Azure-Samples/azure-sql-db-openai/blob/classic/vector-embeddings/05-find-similar-articles.sql
	FROM
		@QuestionVectors AS qv
		INNER JOIN MovieVector AS mv on qv.VectorValueId = mv.VectorValueId
		INNER JOIN Movie AS m ON m.MovieId = mv.MovieId
	GROUP BY
		mv.MovieId,
		m.Title
	ORDER BY
		CosineDistance DESC

END
GO

/*
	*** Vectorize Data ***
*/

DECLARE @Vectors table (
	VectorValueId int,
	VectorValue float
)

DECLARE @MovieId int
DECLARE @Title varchar(max)

DECLARE curMovies CURSOR FOR
	SELECT MovieId, Title FROM Movie

OPEN curMovies
FETCH NEXT FROM curMovies INTO @MovieId, @Title

WHILE @@FETCH_STATUS = 0
BEGIN

	DELETE FROM @Vectors
	INSERT INTO @Vectors
		EXEC VectorizeText @Title

	DECLARE @VectorizeCount int = @@ROWCOUNT
	PRINT CONCAT('Vectorized text "', @Title,  '" (', @VectorizeCount, ' values)')

	INSERT INTO MovieVector
		SELECT
			@MovieId,
			mv.VectorValueId,
			mv.VectorValue
		FROM
			@Vectors AS mv

	FETCH NEXT FROM curMovies INTO @MovieId, @Title

END

CLOSE curMovies
DEALLOCATE curMovies
GO

/*
	*** Run Vector Queries ***
*/

EXEC AskQuestion 'May the force be with you'
EXEC AskQuestion 'I''ll make him an offer he can''t refuse'
EXEC AskQuestion 'Don Corleone'
EXEC AskQuestion 'Toga party'
EXEC AskQuestion 'Luke Skywalker'
EXEC AskQuestion 'Darth Vader'
EXEC AskQuestion 'Yoda'
EXEC AskQuestion 'Death Star'
GO

/*
	*** Run Cleanup ***
*/

DROP PROCEDURE IF EXISTS AskQuestion
DROP PROCEDURE IF EXISTS VectorizeText
DROP TABLE IF EXISTS MovieVector
DROP TABLE IF EXISTS Movie

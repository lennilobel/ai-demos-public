-- Connect to MovieDemo database

DROP TABLE IF EXISTS Movie
CREATE TABLE Movie (
	MovieId int IDENTITY,
	Title varchar(50)
)

INSERT INTO Movie VALUES
	('Return of the Jedi'),
	('The Godfather'),
	('Animal House'),
	('The Two Towers')

DROP TABLE IF EXISTS MovieVector
CREATE TABLE MovieVector (
	MovieId int,
	VectorValueId int,
	VectorValue float
)

GO
CREATE OR ALTER PROCEDURE VectorizeText
	@Text varchar(max)
AS
BEGIN

	DECLARE @Headers varchar(max) = JSON_OBJECT('api-key': '[API-KEY]')
	DECLARE @Payload varchar(max) = JSON_OBJECT('input': @Text)
	DECLARE @Response nvarchar(max)
	DECLARE @ReturnValue int

	EXEC @ReturnValue = sp_invoke_external_rest_endpoint
		@url = 'https://lenni-openai.openai.azure.com/openai/deployments/lenni-text-embedding-3-large/embeddings?api-version=2023-03-15-preview',
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
CREATE OR ALTER PROCEDURE AskQuestion
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

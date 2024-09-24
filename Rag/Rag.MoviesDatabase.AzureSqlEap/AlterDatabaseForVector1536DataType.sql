ALTER TABLE Movie
DROP COLUMN Vectors

ALTER TABLE Movie
ADD Vectors vector(1536)
GO

ALTER PROCEDURE VectorizeText
	@Text varchar(max),
	@Vectors vector(1536) OUTPUT	-- *EAP*
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

	DECLARE @VectorsJson nvarchar(max) = JSON_QUERY(@Response, '$.result.data[0].embedding')

	SET @Vectors = CONVERT(vector(1536), @VectorsJson)

END
GO

ALTER PROCEDURE VectorizeMovies
	@MovieIdsCsv varchar(max) = NULL
AS
BEGIN

	SET NOCOUNT ON

    DECLARE @MovieIds table (MovieId int)
    INSERT INTO @MovieIds
        SELECT CONVERT(int, value) AS MovieId
        FROM STRING_SPLIT(@MovieIdsCsv, ',')

	DECLARE @MoviesJson varchar(max) = dbo.GetMoviesJsonUdf(@MovieIdsCsv)

	IF LEFT(@MoviesJson, 1) = '{'
		SET @MoviesJson = CONCAT('[', @MoviesJson, ']')

	DECLARE @MovieJson varchar(max)
	DECLARE @ErrorCount int = 0

	DECLARE curMovies CURSOR FOR
		SELECT value FROM OPENJSON(@MoviesJson)

	OPEN curMovies
	FETCH NEXT FROM curMovies INTO @MovieJson

	WHILE @@FETCH_STATUS = 0
	BEGIN

		DECLARE @MovieId int = JSON_VALUE(@MovieJson, '$.MovieId')
		DECLARE @Title varchar(max) = JSON_VALUE(@MovieJson, '$.Title')

		DECLARE @Message varchar(max) = CONCAT('Vectorizing movie ID ', @MovieId, ' - ', @Title)
		RAISERROR(@Message, 0, 1) WITH NOWAIT

		DECLARE @MovieVectors vector(1536)	-- *EAP*

		BEGIN TRY

			EXEC VectorizeText
				@MovieJson,
				@MovieVectors OUTPUT

		END TRY

		BEGIN CATCH

			RAISERROR('An error occurred attempting to vectorize the movie', 0, 1) WITH NOWAIT

			SET @Message = ERROR_MESSAGE()
			RAISERROR(@Message, 0, 1) WITH NOWAIT

			SET @ErrorCount +=1

		END CATCH

		UPDATE Movie
		SET Vectors = @MovieVectors
		WHERE MovieId = @MovieId

		FETCH NEXT FROM curMovies INTO @MovieJson

	END

	CLOSE curMovies
	DEALLOCATE curMovies

	IF @ErrorCount > 0
		THROW 50000, 'One or more errors occurred vectorizing the movies data', 1

END
GO

ALTER PROCEDURE RunVectorSearch
	@Vectors vector(1536)	-- *EAP*
AS
BEGIN

	DROP TABLE IF EXISTS #SimilarityResults

	SELECT
		MovieId,
		SimilarityScore = VECTOR_DISTANCE('cosine', @Vectors, Vectors)
	INTO
		#SimilarityResults
	FROM
		Movie
	ORDER BY
		SimilarityScore

	SELECT TOP 5
		MovieJson = dbo.GetMoviesJsonUdf(MovieId),
		SimilarityScore
	FROM
		#SimilarityResults
	ORDER BY
		SimilarityScore
	
END
GO

ALTER PROCEDURE AskQuestion
	@Question varchar(max)
AS
BEGIN

	DECLARE @Vectors vector(1536)	-- *EAP*

	EXEC VectorizeText @Question, @Vectors OUTPUT

	EXEC RunVectorSearch @Vectors

END

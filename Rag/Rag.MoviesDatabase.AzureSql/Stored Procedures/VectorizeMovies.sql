CREATE PROCEDURE VectorizeMovies
	@MovieId int = NULL
AS
BEGIN

	SET NOCOUNT ON

	DECLARE @MoviesJson varchar(max) = dbo.GetMoviesJsonUdf(@MovieId)

	IF @MovieId IS NOT NULL
		SET @MoviesJson = CONCAT('[', @MoviesJson, ']')

	DECLARE curMovies CURSOR FOR
		SELECT value FROM OPENJSON(@MoviesJson)

	DECLARE @MovieJson varchar(max)
	DECLARE @ErrorCount int = 0

	OPEN curMovies
	FETCH NEXT FROM curMovies INTO @MovieJson

	WHILE @@FETCH_STATUS = 0
	BEGIN

		SET @MovieId = JSON_VALUE(@MovieJson, '$.MovieId')
		DECLARE @Title varchar(max) = JSON_VALUE(@MovieJson, '$.Title')

		DECLARE @Message varchar(max) = CONCAT('Vectorizing movie ID ', @MovieId, ' - ', @Title)
		RAISERROR(@Message, 0, 1) WITH NOWAIT

		DECLARE @MovieText varchar(max) = @MovieJson
		SET @MovieText = REPLACE(@MovieText, '"', ' ')
		SET @MovieText = REPLACE(@MovieText, '{', ' ')
		SET @MovieText = REPLACE(@MovieText, '}', ' ')

		DECLARE @MovieVectors table (
			VectorValueId int,
			VectorValue float
		)

		BEGIN TRY

			DELETE FROM @MovieVectors

			INSERT INTO @MovieVectors
				EXEC VectorizeText @MovieText

			INSERT INTO MovieVector
			SELECT
				@MovieId,
				mv.VectorValueId,
				mv.VectorValue
			FROM
				@MovieVectors AS mv

		END TRY

		BEGIN CATCH

			PRINT 'An error occurred attempting to vectorize the movie'
			SET @ErrorCount +=1

		END CATCH

		FETCH NEXT FROM curMovies INTO @MovieJson

	END

	CLOSE curMovies
	DEALLOCATE curMovies

	IF @ErrorCount > 0
		THROW 50000, 'One or more errors occurred vectorizing the movies data', 1

END

CREATE PROCEDURE VectorizeMovies
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
		SELECT value FROM OPENJSON(@MoviesJson) ORDER BY JSON_VALUE(value, '$.Title')

	OPEN curMovies
	FETCH NEXT FROM curMovies INTO @MovieJson

	WHILE @@FETCH_STATUS = 0
	BEGIN

		DECLARE @MovieId int = JSON_VALUE(@MovieJson, '$.MovieId')
		DECLARE @Title varchar(max) = JSON_VALUE(@MovieJson, '$.Title')

		DECLARE @Message varchar(max) = CONCAT('Vectorizing movie: ', @Title, ' (', @MovieId, ')')
		RAISERROR(@Message, 0, 1) WITH NOWAIT

		DECLARE @MovieVector vector(1536)	-- *Preview*

		BEGIN TRY

			EXEC VectorizeText
				@MovieJson,
				@MovieVector OUTPUT

		END TRY

		BEGIN CATCH

			RAISERROR('An error occurred attempting to vectorize the movie', 0, 1) WITH NOWAIT

			SET @Message = ERROR_MESSAGE()
			RAISERROR(@Message, 0, 1) WITH NOWAIT

			SET @ErrorCount +=1

		END CATCH

		UPDATE Movie
		SET Vector = @MovieVector
		WHERE MovieId = @MovieId

		FETCH NEXT FROM curMovies INTO @MovieJson

	END

	CLOSE curMovies
	DEALLOCATE curMovies

	IF @ErrorCount > 0
		THROW 50000, 'One or more errors occurred vectorizing the movies data', 1

END

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

	DECLARE @ErrorCount int = 0
    DECLARE @BatchSize int = 100
    DECLARE @CurrentPosition int = 0
    DECLARE @TotalCount int = (SELECT COUNT(*) FROM OPENJSON(@MoviesJson))
	DECLARE @Message varchar(max)

	DECLARE @MovieVectors table (
        MovieId int,
		VectorValueId int,
		VectorValue float
	)

    WHILE @CurrentPosition < @TotalCount BEGIN

		BEGIN TRY

			-- Get the next batch of movies
			DECLARE @MoviesBatchJson varchar(max) = (
				SELECT
					CONCAT('[', STRING_AGG(value, ','), ']')
				FROM (
					SELECT value
					FROM OPENJSON(@MoviesJson)
					ORDER BY JSON_VALUE(value, '$.Title')
					OFFSET @CurrentPosition ROWS FETCH NEXT @BatchSize ROWS ONLY
				) AS Batch
			)

			-- Emit a message for each movie
			DECLARE @MovieJson varchar(max)
			DECLARE curMovies CURSOR FOR SELECT value FROM OPENJSON(@MoviesBatchJson)
			OPEN curMovies
			FETCH NEXT FROM curMovies INTO @MovieJson
			WHILE @@FETCH_STATUS = 0
			BEGIN
				SET @Message = CONCAT('Vectorizing entity - ', JSON_VALUE(@MovieJson, '$.Title'), ' (ID ', JSON_VALUE(@MovieJson, '$.MovieId'), ')')
				RAISERROR(@Message, 0, 1) WITH NOWAIT
				FETCH NEXT FROM curMovies INTO @MovieJson
			END
			CLOSE curMovies
			DEALLOCATE curMovies

			-- Vectorize the batch
			DELETE FROM @MovieVectors

			INSERT INTO @MovieVectors
			EXEC VectorizeMoviesBatch @MoviesBatchJson

			-- Save the vectors
			INSERT INTO MovieVector
			SELECT
				MovieId,
				VectorValueId,
				VectorValue
			FROM
				@MovieVectors

		END TRY

		BEGIN CATCH

			RAISERROR('An error occurred attempting to vectorize the movie batch', 0, 1) WITH NOWAIT

			SET @Message = ERROR_MESSAGE()
			RAISERROR(@Message, 0, 1) WITH NOWAIT

			SET @ErrorCount +=1

		END CATCH

        SET @CurrentPosition = @CurrentPosition + @BatchSize

    END

	IF @ErrorCount > 0
		THROW 50000, 'One or more errors occurred vectorizing the movies data', 1

END

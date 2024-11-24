/*
	*** Vectorize Data ***
*/

DECLARE @MovieId int
DECLARE @Title varchar(max)
DECLARE @Vector vector(1536)

DECLARE curMovies CURSOR FOR
	SELECT MovieId, Title FROM Movie

OPEN curMovies
FETCH NEXT FROM curMovies INTO @MovieId, @Title

WHILE @@FETCH_STATUS = 0 BEGIN

	EXEC VectorizeText @Title, @Vector OUTPUT

	PRINT CONCAT('Vectorized text "', @Title,  '"')

	UPDATE Movie
	SET Vector = @Vector
	WHERE MovieId = @MovieId

	FETCH NEXT FROM curMovies INTO @MovieId, @Title

END

CLOSE curMovies
DEALLOCATE curMovies
GO

-- View the movie titles with vectors
SELECT * FROM Movie

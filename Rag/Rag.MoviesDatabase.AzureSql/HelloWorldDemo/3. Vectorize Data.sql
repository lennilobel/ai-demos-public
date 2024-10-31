/*
	*** Vectorize Data ***
*/

-- Declare a table variable to hold the vector components returned by Azure OpenAI
DECLARE @Vector table (
	VectorValueId int,		-- Sequential ID (preserve ordering of each vector value)
	VectorValue float		-- Vector value
)

DECLARE @MovieId int
DECLARE @Title varchar(max)

-- Iterate each movie title using a cursor
DECLARE curMovies CURSOR FOR
	SELECT MovieId, Title FROM Movie

OPEN curMovies
FETCH NEXT FROM curMovies INTO @MovieId, @Title

WHILE @@FETCH_STATUS = 0
BEGIN

	-- Clear the previous movie title vector
	DELETE FROM @Vector

	-- Call Azure OpenAI to vectorize the movie title into the table variable
	INSERT INTO @Vector
		EXEC VectorizeText @Title

	DECLARE @VectorizeCount int = @@ROWCOUNT
	PRINT CONCAT('Vectorized text "', @Title,  '" (', @VectorizeCount, ' values)')

	-- Store the vector components for the movie title in the MovieVector table
	INSERT INTO MovieVector
		SELECT
			@MovieId,
			mv.VectorValueId,
			mv.VectorValue
		FROM
			@Vector AS mv

	FETCH NEXT FROM curMovies INTO @MovieId, @Title

END

CLOSE curMovies
DEALLOCATE curMovies
GO

-- View the movie titles
SELECT * FROM Movie

-- View the vector components generated for each movie title
SELECT * FROM MovieVector

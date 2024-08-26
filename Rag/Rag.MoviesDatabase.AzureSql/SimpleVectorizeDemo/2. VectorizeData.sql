-- Connect to MovieDemo database

SET NOCOUNT ON

DECLARE @OpenAIEndpoint varchar(max) = 'https://lenni-openai.openai.azure.com/'
DECLARE @OpenAIApiKey varchar(max) = '1e981882b329481ebe4b2bfa261f8dce'
DECLARE @OpenAIDeploymentName varchar(max) = 'lenni-text-embedding-3-large'

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


-- Connect to MovieDemo database

SET NOCOUNT ON

DECLARE @OpenAIEndpoint varchar(max) = '[OPENAI-ENDPOINT]'
DECLARE @OpenAIApiKey varchar(max) = '[OPENAI-API-KEY]'
DECLARE @OpenAIDeploymentName varchar(max) = '[OPENAI-DEPLOYMENT-NAME]'

DECLARE @Vectors table (
	VectorValueId int,
	VectorValue float
)

DECLARE @MovieId int
DECLARE @Title varchar(max)

DECLARE curMovies CURSOR FOR
	SELECT MovieId, Title FROM HelloWorld.Movie

OPEN curMovies
FETCH NEXT FROM curMovies INTO @MovieId, @Title

WHILE @@FETCH_STATUS = 0
BEGIN

	DELETE FROM @Vectors
	INSERT INTO @Vectors
		EXEC HelloWorld.VectorizeText @Title

	DECLARE @VectorizeCount int = @@ROWCOUNT
	PRINT CONCAT('Vectorized text "', @Title,  '" (', @VectorizeCount, ' values)')

	INSERT INTO HelloWorld.MovieVector
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


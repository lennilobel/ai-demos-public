CREATE PROCEDURE VectorizeMoviesBatch
    @MoviesBatchJson varchar(max)
AS
BEGIN

    DECLARE @MoviesPayload nvarchar(max)
    DECLARE @Sql nvarchar(max)

    ;WITH Cte AS (SELECT value FROM OPENJSON(@MoviesBatchJson))
    SELECT @Sql = CONCAT('SET @MoviesPayload = JSON_OBJECT(''input'': JSON_ARRAY(''', (SELECT STRING_AGG(REPLACE(value, '''', ''''''), '"'',''"') FROM Cte))
    SELECT @Sql = LEFT(@Sql, LEN(@Sql) - 1) + '''))'
    EXEC sp_executesql @Sql, N'@MoviesPayload nvarchar(max) OUTPUT', @MoviesPayload OUTPUT

    DECLARE @OpenAIEndpoint varchar(max)        = (SELECT ConfigValue FROM AppConfig WHERE ConfigKey = 'OpenAIEndpoint')
    DECLARE @OpenAIApiKey varchar(max)          = (SELECT ConfigValue FROM AppConfig WHERE ConfigKey = 'OpenAIApiKey')
    DECLARE @OpenAIDeploymentName varchar(max)  = (SELECT ConfigValue FROM AppConfig WHERE ConfigKey = 'OpenAIDeploymentName')

    DECLARE @Url varchar(max) = CONCAT(@OpenAIEndpoint, 'openai/deployments/', @OpenAIDeploymentName, '/embeddings?api-version=2023-03-15-preview')
    DECLARE @Headers varchar(max) = JSON_OBJECT('api-key': @OpenAIApiKey)
    DECLARE @Response nvarchar(max)
    DECLARE @ReturnValue int

    EXEC @ReturnValue = sp_invoke_external_rest_endpoint
        @url = @Url,
        @method = 'POST',
        @headers = @Headers,
        @payload = @MoviesPayload,
        @response = @Response OUTPUT

    IF @ReturnValue != 0
        THROW 50000, @Response, 1

    -- Extract MovieId from @MoviesBatchJson and assign a unique index
    ;WITH MoviesCte AS (
        SELECT 
            MovieId = JSON_VALUE(value, '$.MovieId'), 
            MovieIndex = ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1     -- Will replace this MovieIndex with the MovieId when done
        FROM
            OPENJSON(@MoviesBatchJson)
    ),
    -- Extract the embedding data from the response
    EmbeddingsCte AS (
        SELECT 
            MovieIndex, 
            VectorValueId = ResultDataEmbedding.[key] + 1,
            VectorValue = ResultDataEmbedding.value
        FROM
            OPENJSON(@Response, '$.result.data')                    -- each movie is an element in the result's data array
        WITH (
            MovieIndex int '$.index',
            Embedding nvarchar(max) '$.embedding' AS JSON           -- each movie's vector is retrieved from the embedding array in the data array of each result
        ) AS ResultData
        CROSS APPLY
            OPENJSON(ResultData.Embedding) AS ResultDataEmbedding   -- get the embedding array (vector for a single movie) from each element in the data array (movies)
    )
    -- Compose the result by joining the MoviesCte with EmbeddingsCte based on the MovieIndex
    SELECT 
        m.MovieId,
        e.VectorValueId,
        VectorValue = CONVERT(float, e.VectorValue)
    FROM
        MoviesCte AS m
        INNER JOIN EmbeddingsCte AS e ON m.MovieIndex = e.MovieIndex
    ORDER BY
        MovieId,
        VectorValueId

END

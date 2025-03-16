CREATE PROCEDURE RunVectorSearch
	@Vector vector(1536)	-- *Preview*
AS
BEGIN

	DROP TABLE IF EXISTS #SimilarityResults

	SELECT
		MovieId,
		CosineDistance = VECTOR_DISTANCE('cosine', @Vector, Vector)
	INTO
		#SimilarityResults
	FROM
		Movie
	ORDER BY
		CosineDistance

	SELECT TOP 5
		MovieJson = dbo.GetMoviesJsonUdf(MovieId),
		CosineDistance
	FROM
		#SimilarityResults
	ORDER BY
		CosineDistance
	
END

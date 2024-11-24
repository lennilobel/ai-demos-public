CREATE PROCEDURE RunVectorSearch
	@Vector varbinary(8000)
AS
BEGIN

	DROP TABLE IF EXISTS #SimilarityResults

	SELECT
		MovieId,
		SimilarityScore = VECTOR_DISTANCE('cosine', @Vector, Vector)
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

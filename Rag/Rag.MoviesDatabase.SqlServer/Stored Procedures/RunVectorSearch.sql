CREATE PROCEDURE RunVectorSearch
	@Vectors VectorsUdt READONLY
AS
BEGIN

	DROP TABLE IF EXISTS #SimilarityResults

	SELECT TOP 5
		v2.MovieId, 
		CosineDistance = SUM(v1.VectorValue * v2.VectorValue)	-- https://github.com/Azure-Samples/azure-sql-db-openai/blob/classic/vector-embeddings/05-find-similar-articles.sql
		--CosineDistance = SUM(v1.VectorValue * v2.VectorValue) / 
		--    (
		--        SQRT(SUM(v1.VectorValue * v1.VectorValue)) 
		--        * 
		--        SQRT(SUM(v2.VectorValue * v2.VectorValue))
		--    )
	INTO
		#SimilarityResults
	FROM 
		@Vectors AS v1
		INNER JOIN MovieVector AS v2 on v1.VectorValueId = v2.VectorValueId
	GROUP BY
		v2.MovieId
	ORDER BY
		CosineDistance DESC

	SELECT
		r.CosineDistance,
		MovieJson = dbo.GetMoviesJsonUdf(r.MovieId)
	FROM
		#SimilarityResults AS r
		INNER JOIN Movie AS m ON m.MovieId = r.MovieId
	ORDER BY
		CosineDistance DESC
	
END

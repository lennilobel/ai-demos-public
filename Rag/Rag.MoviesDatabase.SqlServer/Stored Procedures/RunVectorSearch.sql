CREATE PROCEDURE RunVectorSearch
	@Vectors VectorsUdt READONLY
AS
BEGIN

	DROP TABLE IF EXISTS #SimilarityResults

	SELECT TOP 5
		mv.MovieId, 
		CosineDistance =
			SUM(qv.VectorValue * mv.VectorValue) /	-- https://github.com/Azure-Samples/azure-sql-db-openai/blob/classic/vector-embeddings/05-find-similar-articles.sql
		    (
		        SQRT(SUM(qv.VectorValue * qv.VectorValue)) 
		        * 
		        SQRT(SUM(mv.VectorValue * mv.VectorValue))
		    )
	INTO
		#SimilarityResults
	FROM 
		@Vectors AS qv
		INNER JOIN MovieVector AS mv on qv.VectorValueId = mv.VectorValueId
	GROUP BY
		mv.MovieId
	ORDER BY
		CosineDistance DESC

	SELECT
		MovieJson = dbo.GetMoviesJsonUdf(r.MovieId),
		r.CosineDistance
	FROM
		#SimilarityResults AS r
		INNER JOIN Movie AS m ON m.MovieId = r.MovieId
	ORDER BY
		CosineDistance DESC
	
END

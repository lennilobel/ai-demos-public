CREATE PROCEDURE RunVectorSearch
	@Vector VectorUdt READONLY
AS
BEGIN

	DROP TABLE IF EXISTS #SimilarityResults

	SELECT TOP 5
		mv.MovieId, 
		CosineDistance =										-- Calculate the cosine distance as:
			1 -														--	one minus
			(														--	(
				SUM(qv.VectorValue * mv.VectorValue) /				--		dot product (sum of the element-wise multiplication of corresponding vector values from the question and movie vectors) divided by
				(													--		(
					SQRT(SUM(qv.VectorValue * qv.VectorValue)) *	--			magnitude of the question vector (square root of the sum of squares of the question vector values) times
					SQRT(SUM(mv.VectorValue * mv.VectorValue))		--			magnitude of the movie vector (square root of the sum of squares of the movie vector values)
				)													--		)
			)														--	)
	INTO
		#SimilarityResults
	FROM 
		@Vector AS qv															-- compare the query vector values
		INNER JOIN MovieVector AS mv on qv.VectorValueId = mv.VectorValueId		-- with each movie's vector values
	GROUP BY
		mv.MovieId		-- 
	ORDER BY
		CosineDistance	-- Smaller distances (more similar) before larger distances (less similar)

	SELECT
		MovieJson = dbo.GetMoviesJsonUdf(r.MovieId),
		r.CosineDistance
	FROM
		#SimilarityResults AS r
		INNER JOIN Movie AS m ON m.MovieId = r.MovieId
	ORDER BY
		CosineDistance

END

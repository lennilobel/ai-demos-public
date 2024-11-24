CREATE PROCEDURE CreateMovieVectors
	@MovieVectors MovieVectorsUdt READONLY
AS
BEGIN

	INSERT INTO MovieVector (
		MovieId,
		VectorValueId,
		VectorValue)
	SELECT
		MovieId,
		VectorValueId,
		VectorValue
	FROM
		@MovieVectors

END

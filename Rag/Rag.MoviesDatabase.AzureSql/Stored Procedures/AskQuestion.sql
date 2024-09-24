CREATE PROCEDURE AskQuestion
	@Question varchar(max)
AS
BEGIN

	DECLARE @Vectors VectorsUdt

	INSERT INTO @Vectors
		EXEC VectorizeText @Question

	EXEC RunVectorSearch @Vectors

END

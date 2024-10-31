CREATE PROCEDURE AskQuestion
	@Question varchar(max)
AS
BEGIN

	DECLARE @Vector VectorUdt

	INSERT INTO @Vector
		EXEC VectorizeText @Question

	EXEC RunVectorSearch @Vector

END

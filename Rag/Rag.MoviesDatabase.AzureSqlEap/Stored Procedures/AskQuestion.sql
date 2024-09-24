CREATE PROCEDURE AskQuestion
	@Question varchar(max)
AS
BEGIN

	DECLARE @Vectors varbinary(8000)

	EXEC VectorizeText @Question, @Vectors OUTPUT

	EXEC RunVectorSearch @Vectors

END

CREATE PROCEDURE AskQuestion
	@Question varchar(max)
AS
BEGIN

	DECLARE @Vector varbinary(8000)

	EXEC VectorizeText @Question, @Vector OUTPUT

	EXEC RunVectorSearch @Vector

END

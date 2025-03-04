CREATE PROCEDURE AskQuestion
	@Question varchar(max)
AS
BEGIN

	DECLARE @Vector vector(1536)	-- *Preview*

	EXEC VectorizeText @Question, @Vector OUTPUT

	EXEC RunVectorSearch @Vector

END

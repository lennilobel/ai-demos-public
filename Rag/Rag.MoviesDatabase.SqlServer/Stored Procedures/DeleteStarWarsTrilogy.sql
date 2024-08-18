CREATE PROCEDURE DeleteStarWarsTrilogy
AS
BEGIN

	DECLARE @MovieId int

	SELECT @MovieId = MovieId FROM Movie WHERE Title = 'Star Wars'
	EXEC DeleteMovie @MovieId

	SELECT @MovieId = MovieId FROM Movie WHERE Title = 'The Empire Strikes Back'
	EXEC DeleteMovie @MovieId

	SELECT @MovieId = MovieId FROM Movie WHERE Title = 'Return of the Jedi'
	EXEC DeleteMovie @MovieId

END

CREATE PROCEDURE DeleteMovie
	@MovieId int
AS
BEGIN

	SET NOCOUNT ON

	IF @MovieId IS NOT NULL BEGIN

		DECLARE @Message varchar(max) = CONCAT('Deleting movie ID ', @MovieId)
		RAISERROR(@Message, 0, 1) WITH NOWAIT

		DELETE FROM MovieGenre WHERE MovieId = @MovieId
		DELETE FROM MovieProductionCompany WHERE MovieId = @MovieId
		DELETE FROM MovieProductionCountry WHERE MovieId = @MovieId
		DELETE FROM MovieSpokenLanguage WHERE MovieId = @MovieId
		DELETE FROM MovieVector WHERE MovieId = @MovieId
		DELETE FROM Movie WHERE MovieId = @MovieId

	END

END

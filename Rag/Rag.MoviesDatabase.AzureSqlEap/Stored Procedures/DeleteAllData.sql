CREATE PROCEDURE DeleteAllData
AS
BEGIN

	RAISERROR('Deleting all data', 0, 1) WITH NOWAIT

	SET NOCOUNT ON

	TRUNCATE TABLE	MovieSpokenLanguage
	TRUNCATE TABLE	MovieProductionCountry
	TRUNCATE TABLE	MovieProductionCompany
	TRUNCATE TABLE	MovieGenre
	DELETE FROM		SpokenLanguage
	DELETE FROM		ProductionCountry
	DELETE FROM		ProductionCompany
	DELETE FROM		Genre
	DELETE FROM		Movie

END

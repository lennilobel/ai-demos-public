CREATE PROCEDURE DeleteAllData
AS
BEGIN

	RAISERROR('Deleting data in all tables', 0, 1) WITH NOWAIT

	SET NOCOUNT ON

	TRUNCATE TABLE	MovieVector
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

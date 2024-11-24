CREATE PROCEDURE GetMoviesJson
	@MovieIdsCsv varchar(max) = NULL
AS
BEGIN

    SELECT MoviesJson = dbo.GetMoviesJsonUdf(@MovieIdsCsv)

END

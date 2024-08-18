CREATE PROCEDURE GetMoviesJson
    @MovieId int = NULL
AS
BEGIN

    SELECT MoviesJson = dbo.GetMoviesJsonUdf(@MovieId)

END

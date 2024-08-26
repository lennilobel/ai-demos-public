CREATE FUNCTION GetMoviesJsonUdf(@MovieIdsCsv varchar(max))
RETURNS varchar(max)
AS
BEGIN

    DECLARE @MovieIds table (MovieId int)
    INSERT INTO @MovieIds
        SELECT CONVERT(int, value) AS MovieId
        FROM STRING_SPLIT(@MovieIdsCsv, ',')

    DECLARE @MovieCount int = @@ROWCOUNT

    DECLARE @MoviesJson varchar(max) = (
        SELECT
            m.MovieId,
            m.Title,
            m.OriginalTitle,
            m.ReleaseDate,
            m.Popularity,
            m.VoteAverage,
            m.VoteCount,
            m.Runtime,
            m.Overview,
            m.Budget,
            m.Revenue,
            m.HomePage,
            m.Status,
            m.Tagline,
            m.Video,
            Genres = JSON_QUERY((
                SELECT 
                    g.GenreName
                FROM
                    MovieGenre AS mg
                    INNER JOIN Genre AS g ON mg.GenreId = g.GenreId
                WHERE
                    mg.MovieId = m.MovieId
                FOR JSON PATH
            )),
            ProductionCompanies = JSON_QUERY((
                SELECT 
                    pc.ProductionCompanyName
                FROM
                    MovieProductionCompany AS mpc
                    INNER JOIN ProductionCompany AS pc ON mpc.ProductionCompanyId = pc.ProductionCompanyId
                WHERE
                    mpc.MovieId = m.MovieId
                FOR JSON PATH
            )),
            ProductionCountries = JSON_QUERY((
                SELECT 
                    pc.ProductionCountryIsoKey AS IsoKey,
                    pc.ProductionCountryName AS Name
                FROM
                    MovieProductionCountry AS mpc
                    INNER JOIN ProductionCountry AS pc ON mpc.ProductionCountryIsoKey = pc.ProductionCountryIsoKey
                WHERE
                    mpc.MovieId = m.MovieId
                FOR JSON PATH
            )),
            SpokenLanguages = JSON_QUERY((
                SELECT 
                    sl.SpokenLanguageIsoKey AS IsoKey,
                    sl.SpokenLanguageName AS Name
                FROM
                    MovieSpokenLanguage AS msl
                    INNER JOIN SpokenLanguage AS sl ON msl.SpokenLanguageIsoKey = sl.SpokenLanguageIsoKey
                WHERE
                    msl.MovieId = m.MovieId
                FOR JSON PATH
            ))
        FROM 
            Movie AS m
        WHERE
            (@MovieCount = 0) OR
            (@MovieCount > 0 AND EXISTS (SELECT 1 FROM @MovieIds AS ids WHERE ids.MovieId = m.MovieId))
        FOR JSON PATH
    )

    IF @MovieCount = 1
        SET @MoviesJson = SUBSTRING(@MoviesJson, 2, LEN(@MoviesJson) - 2)

    RETURN @MoviesJson

END

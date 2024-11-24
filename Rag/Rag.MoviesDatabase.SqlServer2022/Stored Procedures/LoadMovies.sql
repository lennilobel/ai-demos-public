CREATE PROCEDURE LoadMovies
	@Filename varchar(max)
AS
BEGIN

	SET NOCOUNT ON

	DECLARE @ExternalDataSource varchar(max)

	-- Comment the next line for on-prem SQL Server
	--SET @ExternalDataSource = 'DATA_SOURCE = ''BlobStorageContainer'','

	DECLARE @Sql nvarchar(max) = CONCAT('
		SELECT @RawMoviesJson = BulkColumn
			FROM OPENROWSET(
			BULK ''', @Filename, ''',
			', @ExternalDataSource, '
			SINGLE_CLOB
		) AS MoviesJson
	')

	DECLARE @RawMoviesJson varchar(max)

	EXEC sp_executesql
		@Sql,
		N'@RawMoviesJson varchar(max) OUTPUT',
		@RawMoviesJson = @RawMoviesJson OUTPUT

	SELECT
		MovieId,
		Budget				= TRY_CONVERT(int, Budget),
		HomePage,
		OriginalLanguage,
		OriginalTitle,
		Overview,
		Popularity			= TRY_CONVERT(decimal(9, 6), Popularity),
		ReleaseDate			= TRY_CONVERT(date, ReleaseDate),
		Revenue				= TRY_CONVERT(bigint, TRY_CONVERT(decimal, Revenue)),
		Runtime				= TRY_CONVERT(int, TRY_CONVERT(decimal, Runtime)),
		Status,
		Tagline,
		Title,
		Video				= TRY_CONVERT(bit, Video),
		VoteAverage			= TRY_CONVERT(decimal(3, 1), VoteAverage),
		VoteCount			= TRY_CONVERT(int, TRY_CONVERT(decimal, VoteCount)),
		GenresJson,
		ProductionCompaniesJson,
		ProductionCountriesJson,
		SpokenLanguagesJson,
		DupeNumber			= ROW_NUMBER() OVER (PARTITION BY MovieId ORDER BY MovieId)
	INTO #Movie
	FROM OPENJSON(@RawMoviesJson)
	WITH ( 
		MovieId					int				'$.id',
		Budget					varchar(max)	'$.budget',
		HomePage				varchar(max)	'$.homepage',
		OriginalLanguage		varchar(max)	'$.original_language',
		OriginalTitle			varchar(max)	'$.original_title',
		Overview				varchar(max)	'$.overview',
		Popularity				varchar(max)	'$.popularity',
		ReleaseDate				varchar(max)	'$.release_date',
		Revenue					varchar(max)	'$.revenue',
		Runtime					varchar(max)	'$.runtime',
		Status					varchar(max)	'$.status',
		Tagline					varchar(max)	'$.tagline',
		Title					varchar(max)	'$.title',
		Video					varchar(max)	'$.video',
		VoteAverage				varchar(max)	'$.vote_average',
		VoteCount				varchar(max)	'$.vote_count',
		GenresJson				nvarchar(max)	'$.genres' AS JSON,
		ProductionCompaniesJson	nvarchar(max)	'$.production_companies' AS JSON,
		ProductionCountriesJson	nvarchar(max)	'$.production_countries' AS JSON,
		SpokenLanguagesJson		nvarchar(max)	'$.spoken_languages' AS JSON
	)
	ORDER BY
		MovieId

	-- Populate tables from temp table

	RAISERROR('Populating Movie table', 0, 1) WITH NOWAIT

	INSERT INTO Movie
	SELECT
		MovieId,
		Budget,
		HomePage,
		OriginalLanguage,
		OriginalTitle,
		Overview,
		Popularity,
		ReleaseDate,
		Revenue,
		Runtime,
		Status,
		Tagline,
		Title,
		Video,
		VoteAverage,
		VoteCount
	FROM
		#Movie AS t
	WHERE
		DupeNumber = 1

	RAISERROR('Populating Genre and MovieGenre tables', 0, 1) WITH NOWAIT

	INSERT INTO Genre
	SELECT DISTINCT
		JSON_VALUE(j.value, '$.id') AS GenreId,
		JSON_VALUE(j.value, '$.name') AS GenreName
	FROM #Movie AS m
	CROSS APPLY OPENJSON(GenresJson) AS j
	WHERE NOT EXISTS (
		SELECT 1 
		FROM Genre AS g 
		WHERE g.GenreId = JSON_VALUE(j.value, '$.id')
	)

	INSERT INTO MovieGenre
	SELECT DISTINCT
		m.MovieId,
		JSON_VALUE(j.value, '$.id') AS GenreId
	FROM #Movie AS m
	CROSS APPLY OPENJSON(m.GenresJson) AS j
	WHERE NOT EXISTS (
		SELECT 1 
		FROM MovieGenre AS mg 
		WHERE mg.MovieId = m.MovieId 
		AND mg.GenreId = JSON_VALUE(j.value, '$.id')
	)

	RAISERROR('Populating ProductionCompany and MovieProductionCompany tables', 0, 1) WITH NOWAIT

	INSERT INTO ProductionCompany
	SELECT DISTINCT
		JSON_VALUE(j.value, '$.id') AS ProductionCompanyId,
		JSON_VALUE(j.value, '$.name') AS ProductionCompanyName
	FROM #Movie AS m
	CROSS APPLY OPENJSON(ProductionCompaniesJson) AS j
	WHERE NOT EXISTS (
		SELECT 1 
		FROM ProductionCompany AS pc
		WHERE pc.ProductionCompanyId = JSON_VALUE(j.value, '$.id')
	)

	INSERT INTO MovieProductionCompany
	SELECT DISTINCT
		m.MovieId,
		JSON_VALUE(j.value, '$.id') AS ProductionCompanyId
	FROM #Movie AS m
	CROSS APPLY OPENJSON(m.ProductionCompaniesJson) AS j
	WHERE NOT EXISTS (
		SELECT 1 
		FROM MovieProductionCompany AS mpc 
		WHERE mpc.MovieId = m.MovieId 
		AND mpc.ProductionCompanyId = JSON_VALUE(j.value, '$.id')
	)

	RAISERROR('Populating ProductionCountry and MovieProductionCountry tables', 0, 1) WITH NOWAIT

	INSERT INTO ProductionCountry
	SELECT DISTINCT
		JSON_VALUE(j.value, '$.iso_3166_1') AS ProductionCountryIsoKey,
		JSON_VALUE(j.value, '$.name') AS ProductionCountryName
	FROM #Movie AS m
	CROSS APPLY OPENJSON(ProductionCountriesJson) AS j
	WHERE NOT EXISTS (
		SELECT 1 
		FROM ProductionCountry AS pc 
		WHERE pc.ProductionCountryIsoKey = JSON_VALUE(j.value, '$.iso_3166_1')
	)

	INSERT INTO MovieProductionCountry
	SELECT DISTINCT
		m.MovieId,
		JSON_VALUE(j.value, '$.iso_3166_1') AS ProductionCountryIsoKey
	FROM #Movie AS m
	CROSS APPLY OPENJSON(m.ProductionCountriesJson) AS j
	WHERE NOT EXISTS (
		SELECT 1 
		FROM MovieProductionCountry AS mpc 
		WHERE mpc.MovieId = m.MovieId 
		AND mpc.ProductionCountryIsoKey = JSON_VALUE(j.value, '$.iso_3166_1')
	)

	RAISERROR('Populating SpokenLanguage and MovieSpokenLanguage tables', 0, 1) WITH NOWAIT

	INSERT INTO SpokenLanguage
	SELECT DISTINCT
		JSON_VALUE(j.value, '$.iso_639_1') AS SpokenLanguageIsoKey,
		JSON_VALUE(j.value, '$.name') AS SpokenLanguageName
	FROM #Movie AS m
	CROSS APPLY OPENJSON(SpokenLanguagesJson) AS j
	WHERE NOT EXISTS (
		SELECT 1 
		FROM SpokenLanguage AS sl
		WHERE sl.SpokenLanguageIsoKey = JSON_VALUE(j.value, '$.iso_639_1')
	)

	INSERT INTO MovieSpokenLanguage
	SELECT DISTINCT
		m.MovieId,
		JSON_VALUE(j.value, '$.iso_639_1') AS SpokenLanguageIsoKey
	FROM #Movie AS m
	CROSS APPLY OPENJSON(m.SpokenLanguagesJson) AS j
	WHERE NOT EXISTS (
		SELECT 1 
		FROM MovieSpokenLanguage AS msl 
		WHERE msl.MovieId = m.MovieId 
		AND msl.SpokenLanguageIsoKey = JSON_VALUE(j.value, '$.iso_639_1')
	)

	RAISERROR('Database load complete', 0, 1) WITH NOWAIT

	DROP TABLE IF EXISTS #Movie

END

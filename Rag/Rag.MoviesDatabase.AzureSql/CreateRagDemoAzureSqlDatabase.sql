/****** Object:  Database [rag-demo]    Script Date: 9/16/2024 7:01:35 PM ******/
CREATE DATABASE [rag-demo]  (EDITION = 'GeneralPurpose', SERVICE_OBJECTIVE = 'GP_S_Gen5_1', MAXSIZE = 32 GB) WITH CATALOG_COLLATION = SQL_Latin1_General_CP1_CI_AS, LEDGER = OFF;
GO
ALTER DATABASE [rag-demo] SET COMPATIBILITY_LEVEL = 160
GO
ALTER DATABASE [rag-demo] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [rag-demo] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [rag-demo] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [rag-demo] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [rag-demo] SET ARITHABORT OFF 
GO
ALTER DATABASE [rag-demo] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [rag-demo] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [rag-demo] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [rag-demo] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [rag-demo] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [rag-demo] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [rag-demo] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [rag-demo] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [rag-demo] SET ALLOW_SNAPSHOT_ISOLATION ON 
GO
ALTER DATABASE [rag-demo] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [rag-demo] SET READ_COMMITTED_SNAPSHOT ON 
GO
ALTER DATABASE [rag-demo] SET  MULTI_USER 
GO
ALTER DATABASE [rag-demo] SET ENCRYPTION ON
GO
ALTER DATABASE [rag-demo] SET QUERY_STORE = ON
GO
ALTER DATABASE [rag-demo] SET QUERY_STORE (OPERATION_MODE = READ_WRITE, CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), DATA_FLUSH_INTERVAL_SECONDS = 900, INTERVAL_LENGTH_MINUTES = 60, MAX_STORAGE_SIZE_MB = 100, QUERY_CAPTURE_MODE = AUTO, SIZE_BASED_CLEANUP_MODE = AUTO, MAX_PLANS_PER_QUERY = 200, WAIT_STATS_CAPTURE_MODE = ON)
GO
/*** The scripts of database scoped configurations in Azure should be executed inside the target database connection. ***/
GO
-- ALTER DATABASE SCOPED CONFIGURATION SET MAXDOP = 8;
GO
/****** Object:  DatabaseScopedCredential [BlobStorageCredential]    Script Date: 9/16/2024 7:01:35 PM ******/
CREATE DATABASE SCOPED CREDENTIAL [BlobStorageCredential] WITH IDENTITY = N'SHARED ACCESS SIGNATURE'
GO
/****** Object:  UserDefinedTableType [dbo].[MovieVectorsUdt]    Script Date: 9/16/2024 7:01:35 PM ******/
CREATE TYPE [dbo].[MovieVectorsUdt] AS TABLE(
	[MovieId] [int] NULL,
	[VectorValueId] [int] NULL,
	[VectorValue] [float] NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[VectorsUdt]    Script Date: 9/16/2024 7:01:35 PM ******/
CREATE TYPE [dbo].[VectorsUdt] AS TABLE(
	[VectorValueId] [int] NULL,
	[VectorValue] [float] NULL
)
GO
/****** Object:  UserDefinedFunction [dbo].[GetMoviesJsonUdf]    Script Date: 9/16/2024 7:01:35 PM ******/
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO
CREATE FUNCTION [dbo].[GetMoviesJsonUdf](@MovieIdsCsv varchar(max))
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
GO
/****** Object:  ExternalDataSource [BlobStorageContainer]    Script Date: 9/16/2024 7:01:35 PM ******/
CREATE EXTERNAL DATA SOURCE [BlobStorageContainer] WITH (TYPE = BLOB_STORAGE, LOCATION = N'https://lennidemo.blob.core.windows.net/datasets', CREDENTIAL = [BlobStorageCredential])
GO
/****** Object:  Table [dbo].[AppConfig]    Script Date: 9/16/2024 7:01:35 PM ******/
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppConfig](
	[ConfigKey] [varchar](50) NULL,
	[ConfigValue] [varchar](200) NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Genre]    Script Date: 9/16/2024 7:01:35 PM ******/
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Genre](
	[GenreId] [int] NOT NULL,
	[GenreName] [varchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[GenreId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Movie]    Script Date: 9/16/2024 7:01:35 PM ******/
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Movie](
	[MovieId] [int] NOT NULL,
	[Budget] [int] NULL,
	[HomePage] [varchar](200) NULL,
	[OriginalLanguage] [varchar](10) NULL,
	[OriginalTitle] [varchar](200) NULL,
	[Overview] [varchar](max) NULL,
	[Popularity] [decimal](10, 6) NULL,
	[ReleaseDate] [date] NULL,
	[Revenue] [bigint] NULL,
	[Runtime] [int] NULL,
	[Status] [varchar](50) NULL,
	[Tagline] [varchar](300) NULL,
	[Title] [varchar](200) NULL,
	[Video] [bit] NULL,
	[VoteAverage] [decimal](3, 1) NULL,
	[VoteCount] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[MovieId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MovieGenre]    Script Date: 9/16/2024 7:01:35 PM ******/
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MovieGenre](
	[MovieId] [int] NOT NULL,
	[GenreId] [int] NOT NULL,
 CONSTRAINT [PK_MovieGenre] PRIMARY KEY CLUSTERED 
(
	[MovieId] ASC,
	[GenreId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MovieProductionCompany]    Script Date: 9/16/2024 7:01:35 PM ******/
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MovieProductionCompany](
	[MovieId] [int] NOT NULL,
	[ProductionCompanyId] [int] NOT NULL,
 CONSTRAINT [PK_MovieProductionCompany] PRIMARY KEY CLUSTERED 
(
	[MovieId] ASC,
	[ProductionCompanyId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MovieProductionCountry]    Script Date: 9/16/2024 7:01:35 PM ******/
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MovieProductionCountry](
	[MovieId] [int] NOT NULL,
	[ProductionCountryIsoKey] [varchar](20) NOT NULL,
 CONSTRAINT [PK_MovieProductionCountry] PRIMARY KEY CLUSTERED 
(
	[MovieId] ASC,
	[ProductionCountryIsoKey] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MovieSpokenLanguage]    Script Date: 9/16/2024 7:01:35 PM ******/
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MovieSpokenLanguage](
	[MovieId] [int] NOT NULL,
	[SpokenLanguageIsoKey] [varchar](20) NOT NULL,
 CONSTRAINT [PK_MovieSpokenLanguage] PRIMARY KEY CLUSTERED 
(
	[MovieId] ASC,
	[SpokenLanguageIsoKey] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MovieVector]    Script Date: 9/16/2024 7:01:35 PM ******/
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MovieVector](
	[MovieId] [int] NOT NULL,
	[VectorValueId] [int] NOT NULL,
	[VectorValue] [float] NOT NULL,
 CONSTRAINT [UC_MovieVector_MovieVectorValue] UNIQUE NONCLUSTERED 
(
	[MovieId] ASC,
	[VectorValueId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProductionCompany]    Script Date: 9/16/2024 7:01:36 PM ******/
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProductionCompany](
	[ProductionCompanyId] [int] NOT NULL,
	[ProductionCompanyName] [varchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[ProductionCompanyId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProductionCountry]    Script Date: 9/16/2024 7:01:36 PM ******/
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProductionCountry](
	[ProductionCountryIsoKey] [varchar](20) NOT NULL,
	[ProductionCountryName] [varchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[ProductionCountryIsoKey] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SpokenLanguage]    Script Date: 9/16/2024 7:01:36 PM ******/
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SpokenLanguage](
	[SpokenLanguageIsoKey] [varchar](20) NOT NULL,
	[SpokenLanguageName] [varchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[SpokenLanguageIsoKey] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Index [MovieVectorIndex]    Script Date: 9/16/2024 7:01:36 PM ******/
CREATE CLUSTERED COLUMNSTORE INDEX [MovieVectorIndex] ON [dbo].[MovieVector] WITH (DROP_EXISTING = OFF, COMPRESSION_DELAY = 0, DATA_COMPRESSION = COLUMNSTORE) ON [PRIMARY]
GO
ALTER TABLE [dbo].[MovieGenre]  WITH NOCHECK ADD  CONSTRAINT [FK_MovieGenre_Genre] FOREIGN KEY([GenreId])
REFERENCES [dbo].[Genre] ([GenreId])
GO
ALTER TABLE [dbo].[MovieGenre] CHECK CONSTRAINT [FK_MovieGenre_Genre]
GO
ALTER TABLE [dbo].[MovieGenre]  WITH NOCHECK ADD  CONSTRAINT [FK_MovieGenre_Movie] FOREIGN KEY([MovieId])
REFERENCES [dbo].[Movie] ([MovieId])
GO
ALTER TABLE [dbo].[MovieGenre] CHECK CONSTRAINT [FK_MovieGenre_Movie]
GO
ALTER TABLE [dbo].[MovieProductionCompany]  WITH NOCHECK ADD  CONSTRAINT [FK_MovieProductionCompany_Movie] FOREIGN KEY([MovieId])
REFERENCES [dbo].[Movie] ([MovieId])
GO
ALTER TABLE [dbo].[MovieProductionCompany] CHECK CONSTRAINT [FK_MovieProductionCompany_Movie]
GO
ALTER TABLE [dbo].[MovieProductionCompany]  WITH NOCHECK ADD  CONSTRAINT [FK_MovieProductionCompany_ProductionCompany] FOREIGN KEY([ProductionCompanyId])
REFERENCES [dbo].[ProductionCompany] ([ProductionCompanyId])
GO
ALTER TABLE [dbo].[MovieProductionCompany] CHECK CONSTRAINT [FK_MovieProductionCompany_ProductionCompany]
GO
ALTER TABLE [dbo].[MovieProductionCountry]  WITH NOCHECK ADD  CONSTRAINT [FK_MovieProductionCountry_Movie] FOREIGN KEY([MovieId])
REFERENCES [dbo].[Movie] ([MovieId])
GO
ALTER TABLE [dbo].[MovieProductionCountry] CHECK CONSTRAINT [FK_MovieProductionCountry_Movie]
GO
ALTER TABLE [dbo].[MovieProductionCountry]  WITH NOCHECK ADD  CONSTRAINT [FK_MovieProductionCountry_ProductionCountry] FOREIGN KEY([ProductionCountryIsoKey])
REFERENCES [dbo].[ProductionCountry] ([ProductionCountryIsoKey])
GO
ALTER TABLE [dbo].[MovieProductionCountry] CHECK CONSTRAINT [FK_MovieProductionCountry_ProductionCountry]
GO
ALTER TABLE [dbo].[MovieSpokenLanguage]  WITH NOCHECK ADD  CONSTRAINT [FK_MovieSpokenLanguage_Movie] FOREIGN KEY([MovieId])
REFERENCES [dbo].[Movie] ([MovieId])
GO
ALTER TABLE [dbo].[MovieSpokenLanguage] CHECK CONSTRAINT [FK_MovieSpokenLanguage_Movie]
GO
ALTER TABLE [dbo].[MovieSpokenLanguage]  WITH NOCHECK ADD  CONSTRAINT [FK_MovieSpokenLanguage_SpokenLanguage] FOREIGN KEY([SpokenLanguageIsoKey])
REFERENCES [dbo].[SpokenLanguage] ([SpokenLanguageIsoKey])
GO
ALTER TABLE [dbo].[MovieSpokenLanguage] CHECK CONSTRAINT [FK_MovieSpokenLanguage_SpokenLanguage]
GO
ALTER TABLE [dbo].[MovieVector]  WITH NOCHECK ADD  CONSTRAINT [FK_MovieVector_Movie] FOREIGN KEY([MovieId])
REFERENCES [dbo].[Movie] ([MovieId])
GO
ALTER TABLE [dbo].[MovieVector] CHECK CONSTRAINT [FK_MovieVector_Movie]
GO
/****** Object:  StoredProcedure [dbo].[AskQuestion]    Script Date: 9/16/2024 7:01:36 PM ******/
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO
CREATE PROCEDURE [dbo].[AskQuestion]
	@Question varchar(max)
AS
BEGIN

	DECLARE @Vectors VectorsUdt

	INSERT INTO @Vectors
		EXEC VectorizeText @Question

	EXEC RunVectorSearch @Vectors

END
GO
/****** Object:  StoredProcedure [dbo].[DeleteAllData]    Script Date: 9/16/2024 7:01:36 PM ******/
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO
CREATE PROCEDURE [dbo].[DeleteAllData]
AS
BEGIN

	RAISERROR('Deleting all data', 0, 1) WITH NOWAIT

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
	TRUNCATE TABLE	AppConfig

END
GO
/****** Object:  StoredProcedure [dbo].[DeleteMovie]    Script Date: 9/16/2024 7:01:36 PM ******/
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO
CREATE PROCEDURE [dbo].[DeleteMovie]
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
GO
/****** Object:  StoredProcedure [dbo].[DeleteStarWarsTrilogy]    Script Date: 9/16/2024 7:01:36 PM ******/
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO
CREATE PROCEDURE [dbo].[DeleteStarWarsTrilogy]
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
GO
/****** Object:  StoredProcedure [dbo].[LoadConfig]    Script Date: 9/16/2024 7:01:36 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[LoadConfig]
	@OpenAIEndpoint varchar(max),
	@OpenAIApiKey varchar(max),
	@OpenAIDeploymentName varchar(max)
AS
BEGIN

	SET NOCOUNT ON

	RAISERROR('Populating AppConfig table', 0, 1) WITH NOWAIT

	INSERT INTO AppConfig VALUES
	 ('OpenAIEndpoint', @OpenAIEndpoint),
	 ('OpenAIApiKey', @OpenAIApiKey),
	 ('OpenAIDeploymentName', @OpenAIDeploymentName)

END
GO
/****** Object:  StoredProcedure [dbo].[LoadMovies]    Script Date: 9/16/2024 7:01:36 PM ******/
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO
CREATE PROCEDURE [dbo].[LoadMovies]
	@Filename varchar(max)
AS
BEGIN

	SET NOCOUNT ON

	DECLARE @ExternalDataSource varchar(max)

	-- Comment the next line for on-prem SQL Server
	SET @ExternalDataSource = 'DATA_SOURCE = ''BlobStorageContainer'','

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
GO
/****** Object:  StoredProcedure [dbo].[RunVectorSearch]    Script Date: 9/16/2024 7:01:36 PM ******/
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO
CREATE PROCEDURE [dbo].[RunVectorSearch]
	@Vectors VectorsUdt READONLY
AS
BEGIN

	DROP TABLE IF EXISTS #SimilarityResults

	SELECT TOP 5
		mv.MovieId, 
		CosineDistance = SUM(qv.VectorValue * mv.VectorValue)	-- https://github.com/Azure-Samples/azure-sql-db-openai/blob/classic/vector-embeddings/05-find-similar-articles.sql
		--CosineDistance = SUM(qv.VectorValue * mv.VectorValue) / 
		--    (
		--        SQRT(SUM(qv.VectorValue * qv.VectorValue)) 
		--        * 
		--        SQRT(SUM(mv.VectorValue * mv.VectorValue))
		--    )
	INTO
		#SimilarityResults
	FROM 
		@Vectors AS qv
		INNER JOIN MovieVector AS mv on qv.VectorValueId = mv.VectorValueId
	GROUP BY
		mv.MovieId
	ORDER BY
		CosineDistance DESC

	SELECT
		MovieJson = dbo.GetMoviesJsonUdf(r.MovieId),
		r.CosineDistance
	FROM
		#SimilarityResults AS r
		INNER JOIN Movie AS m ON m.MovieId = r.MovieId
	ORDER BY
		CosineDistance DESC

END
GO
/****** Object:  StoredProcedure [dbo].[VectorizeMovies]    Script Date: 9/16/2024 7:01:36 PM ******/
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO
CREATE PROCEDURE [dbo].[VectorizeMovies]
	@MovieIdsCsv varchar(max) = NULL
AS
BEGIN

	SET NOCOUNT ON

    DECLARE @MovieIds table (MovieId int)
    INSERT INTO @MovieIds
        SELECT CONVERT(int, value) AS MovieId
        FROM STRING_SPLIT(@MovieIdsCsv, ',')

	DECLARE @MoviesJson varchar(max) = dbo.GetMoviesJsonUdf(@MovieIdsCsv)

	IF LEFT(@MoviesJson, 1) = '{'
		SET @MoviesJson = CONCAT('[', @MoviesJson, ']')

	DECLARE @MovieJson varchar(max)
	DECLARE @ErrorCount int = 0

	DECLARE curMovies CURSOR FOR
		SELECT value FROM OPENJSON(@MoviesJson)

	OPEN curMovies
	FETCH NEXT FROM curMovies INTO @MovieJson

	WHILE @@FETCH_STATUS = 0
	BEGIN

		DECLARE @MovieId int = JSON_VALUE(@MovieJson, '$.MovieId')
		DECLARE @Title varchar(max) = JSON_VALUE(@MovieJson, '$.Title')

		DECLARE @Message varchar(max) = CONCAT('Vectorizing movie ID ', @MovieId, ' - ', @Title)
		RAISERROR(@Message, 0, 1) WITH NOWAIT

		DECLARE @MovieVectors table (
			VectorValueId int,
			VectorValue float
		)

		BEGIN TRY

			DELETE FROM @MovieVectors

			INSERT INTO @MovieVectors
			EXEC VectorizeText @MovieJson

			INSERT INTO MovieVector
			SELECT
				@MovieId,
				mv.VectorValueId,
				mv.VectorValue
			FROM
				@MovieVectors AS mv

		END TRY

		BEGIN CATCH

			RAISERROR('An error occurred attempting to vectorize the movie', 0, 1) WITH NOWAIT

			SET @Message = ERROR_MESSAGE()
			RAISERROR(@Message, 0, 1) WITH NOWAIT

			SET @ErrorCount +=1

		END CATCH

		FETCH NEXT FROM curMovies INTO @MovieJson

	END

	CLOSE curMovies
	DEALLOCATE curMovies

	IF @ErrorCount > 0
		THROW 50000, 'One or more errors occurred vectorizing the movies data', 1

END
GO
/****** Object:  StoredProcedure [dbo].[VectorizeText]    Script Date: 9/16/2024 7:01:36 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[VectorizeText]
	@Text varchar(max)
AS
BEGIN

	DECLARE @OpenAIEndpoint varchar(max)		= (SELECT ConfigValue FROM AppConfig WHERE ConfigKey = 'OpenAIEndpoint')
	DECLARE @OpenAIApiKey varchar(max)			= (SELECT ConfigValue FROM AppConfig WHERE ConfigKey = 'OpenAIApiKey')
	DECLARE @OpenAIDeploymentName varchar(max)	= (SELECT ConfigValue FROM AppConfig WHERE ConfigKey = 'OpenAIDeploymentName')

	DECLARE @Url varchar(max) = CONCAT(@OpenAIEndpoint, 'openai/deployments/', @OpenAIDeploymentName, '/embeddings?api-version=2023-03-15-preview')
	DECLARE @Headers varchar(max) = JSON_OBJECT('api-key': @OpenAIApiKey)
	DECLARE @Payload varchar(max) = JSON_OBJECT('input': @Text)
	DECLARE @Response nvarchar(max)
	DECLARE @ReturnValue int

	EXEC @ReturnValue = sp_invoke_external_rest_endpoint
		@url = @Url,
		@method = 'POST',
		@headers = @Headers,
		@payload = @Payload,
		@response = @Response OUTPUT

	IF @ReturnValue != 0
		THROW 50000, @Response, 1

	SELECT
		VectorValueId = [key] + 1,
		VectorValue = value
	FROM
		OPENJSON(@Response, '$.result.data[0].embedding')
	ORDER BY
		VectorValueId

END
GO
ALTER DATABASE [rag-demo] SET  READ_WRITE 
GO

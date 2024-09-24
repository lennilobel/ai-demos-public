USE [master]
GO
/****** Object:  Database [rag-demo]    Script Date: 9/16/2024 9:30:37 AM ******/
CREATE DATABASE [rag-demo]
GO
ALTER DATABASE [rag-demo] SET COMPATIBILITY_LEVEL = 160
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [rag-demo].[dbo].[sp_fulltext_database] @action = 'enable'
end
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
ALTER DATABASE [rag-demo] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [rag-demo] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [rag-demo] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [rag-demo] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [rag-demo] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [rag-demo] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [rag-demo] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [rag-demo] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [rag-demo] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [rag-demo] SET  DISABLE_BROKER 
GO
ALTER DATABASE [rag-demo] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [rag-demo] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [rag-demo] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [rag-demo] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [rag-demo] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [rag-demo] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [rag-demo] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [rag-demo] SET RECOVERY FULL 
GO
ALTER DATABASE [rag-demo] SET  MULTI_USER 
GO
ALTER DATABASE [rag-demo] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [rag-demo] SET DB_CHAINING OFF 
GO
ALTER DATABASE [rag-demo] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [rag-demo] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [rag-demo] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [rag-demo] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
EXEC sys.sp_db_vardecimal_storage_format N'rag-demo', N'ON'
GO
ALTER DATABASE [rag-demo] SET QUERY_STORE = ON
GO
ALTER DATABASE [rag-demo] SET QUERY_STORE (OPERATION_MODE = READ_WRITE, CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), DATA_FLUSH_INTERVAL_SECONDS = 900, INTERVAL_LENGTH_MINUTES = 60, MAX_STORAGE_SIZE_MB = 1000, QUERY_CAPTURE_MODE = AUTO, SIZE_BASED_CLEANUP_MODE = AUTO, MAX_PLANS_PER_QUERY = 200, WAIT_STATS_CAPTURE_MODE = ON)
GO
USE [rag-demo]
GO
/****** Object:  UserDefinedTableType [dbo].[MovieVectorsUdt]    Script Date: 9/16/2024 9:30:37 AM ******/
CREATE TYPE [dbo].[MovieVectorsUdt] AS TABLE(
	[MovieId] [int] NULL,
	[VectorValueId] [int] NULL,
	[VectorValue] [float] NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[VectorsUdt]    Script Date: 9/16/2024 9:30:37 AM ******/
CREATE TYPE [dbo].[VectorsUdt] AS TABLE(
	[VectorValueId] [int] NULL,
	[VectorValue] [float] NULL
)
GO
/****** Object:  UserDefinedFunction [dbo].[GetMoviesJsonUdf]    Script Date: 9/16/2024 9:30:37 AM ******/
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
/****** Object:  Table [dbo].[Genre]    Script Date: 9/16/2024 9:30:37 AM ******/
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
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Movie]    Script Date: 9/16/2024 9:30:37 AM ******/
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
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MovieGenre]    Script Date: 9/16/2024 9:30:37 AM ******/
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
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MovieProductionCompany]    Script Date: 9/16/2024 9:30:37 AM ******/
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
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MovieProductionCountry]    Script Date: 9/16/2024 9:30:37 AM ******/
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
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MovieSpokenLanguage]    Script Date: 9/16/2024 9:30:37 AM ******/
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
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MovieVector]    Script Date: 9/16/2024 9:30:37 AM ******/
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
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProductionCompany]    Script Date: 9/16/2024 9:30:38 AM ******/
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
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProductionCountry]    Script Date: 9/16/2024 9:30:38 AM ******/
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
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SpokenLanguage]    Script Date: 9/16/2024 9:30:38 AM ******/
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
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Index [MovieVectorIndex]    Script Date: 9/16/2024 9:30:38 AM ******/
CREATE CLUSTERED COLUMNSTORE INDEX [MovieVectorIndex] ON [dbo].[MovieVector] WITH (DROP_EXISTING = OFF, COMPRESSION_DELAY = 0, DATA_COMPRESSION = COLUMNSTORE) ON [PRIMARY]
GO
ALTER TABLE [dbo].[MovieGenre]  WITH CHECK ADD  CONSTRAINT [FK_MovieGenre_Genre] FOREIGN KEY([GenreId])
REFERENCES [dbo].[Genre] ([GenreId])
GO
ALTER TABLE [dbo].[MovieGenre] CHECK CONSTRAINT [FK_MovieGenre_Genre]
GO
ALTER TABLE [dbo].[MovieGenre]  WITH CHECK ADD  CONSTRAINT [FK_MovieGenre_Movie] FOREIGN KEY([MovieId])
REFERENCES [dbo].[Movie] ([MovieId])
GO
ALTER TABLE [dbo].[MovieGenre] CHECK CONSTRAINT [FK_MovieGenre_Movie]
GO
ALTER TABLE [dbo].[MovieProductionCompany]  WITH CHECK ADD  CONSTRAINT [FK_MovieProductionCompany_Movie] FOREIGN KEY([MovieId])
REFERENCES [dbo].[Movie] ([MovieId])
GO
ALTER TABLE [dbo].[MovieProductionCompany] CHECK CONSTRAINT [FK_MovieProductionCompany_Movie]
GO
ALTER TABLE [dbo].[MovieProductionCompany]  WITH CHECK ADD  CONSTRAINT [FK_MovieProductionCompany_ProductionCompany] FOREIGN KEY([ProductionCompanyId])
REFERENCES [dbo].[ProductionCompany] ([ProductionCompanyId])
GO
ALTER TABLE [dbo].[MovieProductionCompany] CHECK CONSTRAINT [FK_MovieProductionCompany_ProductionCompany]
GO
ALTER TABLE [dbo].[MovieProductionCountry]  WITH CHECK ADD  CONSTRAINT [FK_MovieProductionCountry_Movie] FOREIGN KEY([MovieId])
REFERENCES [dbo].[Movie] ([MovieId])
GO
ALTER TABLE [dbo].[MovieProductionCountry] CHECK CONSTRAINT [FK_MovieProductionCountry_Movie]
GO
ALTER TABLE [dbo].[MovieProductionCountry]  WITH CHECK ADD  CONSTRAINT [FK_MovieProductionCountry_ProductionCountry] FOREIGN KEY([ProductionCountryIsoKey])
REFERENCES [dbo].[ProductionCountry] ([ProductionCountryIsoKey])
GO
ALTER TABLE [dbo].[MovieProductionCountry] CHECK CONSTRAINT [FK_MovieProductionCountry_ProductionCountry]
GO
ALTER TABLE [dbo].[MovieSpokenLanguage]  WITH CHECK ADD  CONSTRAINT [FK_MovieSpokenLanguage_Movie] FOREIGN KEY([MovieId])
REFERENCES [dbo].[Movie] ([MovieId])
GO
ALTER TABLE [dbo].[MovieSpokenLanguage] CHECK CONSTRAINT [FK_MovieSpokenLanguage_Movie]
GO
ALTER TABLE [dbo].[MovieSpokenLanguage]  WITH CHECK ADD  CONSTRAINT [FK_MovieSpokenLanguage_SpokenLanguage] FOREIGN KEY([SpokenLanguageIsoKey])
REFERENCES [dbo].[SpokenLanguage] ([SpokenLanguageIsoKey])
GO
ALTER TABLE [dbo].[MovieSpokenLanguage] CHECK CONSTRAINT [FK_MovieSpokenLanguage_SpokenLanguage]
GO
ALTER TABLE [dbo].[MovieVector]  WITH CHECK ADD  CONSTRAINT [FK_MovieVector_Movie] FOREIGN KEY([MovieId])
REFERENCES [dbo].[Movie] ([MovieId])
GO
ALTER TABLE [dbo].[MovieVector] CHECK CONSTRAINT [FK_MovieVector_Movie]
GO
/****** Object:  StoredProcedure [dbo].[CreateMovieVectors]    Script Date: 9/16/2024 9:30:38 AM ******/
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO
CREATE PROCEDURE [dbo].[CreateMovieVectors]
	@MovieVectors MovieVectorsUdt READONLY
AS
BEGIN

	INSERT INTO MovieVector (
		MovieId,
		VectorValueId,
		VectorValue)
	SELECT
		MovieId,
		VectorValueId,
		VectorValue
	FROM
		@MovieVectors

END
GO
/****** Object:  StoredProcedure [dbo].[DeleteAllData]    Script Date: 9/16/2024 9:30:38 AM ******/
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO
CREATE PROCEDURE [dbo].[DeleteAllData]
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
GO
/****** Object:  StoredProcedure [dbo].[DeleteMovie]    Script Date: 9/16/2024 9:30:38 AM ******/
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
/****** Object:  StoredProcedure [dbo].[DeleteStarWarsTrilogy]    Script Date: 9/16/2024 9:30:38 AM ******/
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
/****** Object:  StoredProcedure [dbo].[GetMoviesJson]    Script Date: 9/16/2024 9:30:38 AM ******/
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO
CREATE PROCEDURE [dbo].[GetMoviesJson]
	@MovieIdsCsv varchar(max) = NULL
AS
BEGIN

    SELECT MoviesJson = dbo.GetMoviesJsonUdf(@MovieIdsCsv)

END
GO
/****** Object:  StoredProcedure [dbo].[LoadMovies]    Script Date: 9/16/2024 9:30:38 AM ******/
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
GO
/****** Object:  StoredProcedure [dbo].[RunVectorSearch]    Script Date: 9/16/2024 9:30:38 AM ******/
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
USE [master]
GO
ALTER DATABASE [rag-demo] SET  READ_WRITE 
GO

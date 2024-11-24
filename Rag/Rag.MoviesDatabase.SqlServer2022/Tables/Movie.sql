CREATE TABLE Movie
(
	MovieId				int PRIMARY KEY,
	Budget				int,
	HomePage			varchar(200),
	OriginalLanguage	varchar(10),
	OriginalTitle		varchar(200),
	Overview			varchar(max),
	Popularity			decimal(10, 6),
	ReleaseDate			date,
	Revenue				bigint,
	Runtime				int,
	Status				varchar(50),
	Tagline				varchar(300),
	Title				varchar(200),
	Video				bit,
	VoteAverage			decimal(3, 1),
	VoteCount			int,
)

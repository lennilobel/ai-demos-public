CREATE TABLE MovieGenre
(
	MovieId		int,
	GenreId		int,

	CONSTRAINT PK_MovieGenre PRIMARY KEY (MovieId, GenreId),
	CONSTRAINT FK_MovieGenre_Movie FOREIGN KEY (MovieId) REFERENCES Movie (MovieId),
	CONSTRAINT FK_MovieGenre_Genre FOREIGN KEY (GenreId) REFERENCES Genre (GenreId),
)

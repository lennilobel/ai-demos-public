CREATE TABLE MovieSpokenLanguage
(
	MovieId					int,
	SpokenLanguageIsoKey	varchar(20),

	CONSTRAINT PK_MovieSpokenLanguage PRIMARY KEY (MovieId, SpokenLanguageIsoKey),
	CONSTRAINT FK_MovieSpokenLanguage_Movie FOREIGN KEY (MovieId) REFERENCES Movie (MovieId),
	CONSTRAINT FK_MovieSpokenLanguage_SpokenLanguage FOREIGN KEY (SpokenLanguageIsoKey) REFERENCES SpokenLanguage (SpokenLanguageIsoKey),
)

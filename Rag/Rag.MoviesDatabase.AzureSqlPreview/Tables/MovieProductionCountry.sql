CREATE TABLE MovieProductionCountry
(
	MovieId					int,
	ProductionCountryIsoKey	varchar(20),

	CONSTRAINT PK_MovieProductionCountry PRIMARY KEY (MovieId, ProductionCountryIsoKey),
	CONSTRAINT FK_MovieProductionCountry_Movie FOREIGN KEY (MovieId) REFERENCES Movie (MovieId),
	CONSTRAINT FK_MovieProductionCountry_ProductionCountry FOREIGN KEY (ProductionCountryIsoKey) REFERENCES ProductionCountry (ProductionCountryIsoKey),
)

CREATE TABLE MovieProductionCompany
(
	MovieId				int,
	ProductionCompanyId	int,

	CONSTRAINT PK_MovieProductionCompany PRIMARY KEY (MovieId, ProductionCompanyId),
	CONSTRAINT FK_MovieProductionCompany_Movie FOREIGN KEY (MovieId) REFERENCES Movie (MovieId),
	CONSTRAINT FK_MovieProductionCompany_ProductionCompany FOREIGN KEY (ProductionCompanyId) REFERENCES ProductionCompany (ProductionCompanyId),
)

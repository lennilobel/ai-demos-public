CREATE TABLE MovieVector
(
	MovieId int NOT NULL,
	VectorValueId int NOT NULL,
	VectorValue float NOT NULL,

	CONSTRAINT FK_MovieVector_Movie FOREIGN KEY (MovieId) REFERENCES Movie (MovieId),
	CONSTRAINT UC_MovieVector_MovieVectorValue UNIQUE (MovieId, VectorValueId),
)
GO

CREATE CLUSTERED COLUMNSTORE INDEX MovieVectorIndex ON MovieVector
GO

/*
	*** Run Cleanup ***
*/

SET NOCOUNT ON
GO

DROP PROCEDURE IF EXISTS HelloWorld.AskQuestion
DROP PROCEDURE IF EXISTS HelloWorld.VectorizeText
DROP TABLE IF EXISTS HelloWorld.MovieVector
DROP TABLE IF EXISTS HelloWorld.Movie
DROP SCHEMA IF EXISTS HelloWorld

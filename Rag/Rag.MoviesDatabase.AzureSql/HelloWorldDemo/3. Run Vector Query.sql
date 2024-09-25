/*
	*** Run Vector Queries ***
*/

SET NOCOUNT ON
GO

-- Movie phrases
EXEC HelloWorld.AskQuestion 'May the force be with you'
EXEC HelloWorld.AskQuestion 'I''m gonna make him an offer he can''t refuse'
EXEC HelloWorld.AskQuestion 'Toga party'
EXEC HelloWorld.AskQuestion 'One ring to rule them all'

-- Movie characters
EXEC HelloWorld.AskQuestion 'Luke Skywalker'
EXEC HelloWorld.AskQuestion 'Don Corleone'
EXEC HelloWorld.AskQuestion 'James Blutarsky'
EXEC HelloWorld.AskQuestion 'Gandalf'

-- Movie actors
EXEC HelloWorld.AskQuestion 'Mark Hamill'
EXEC HelloWorld.AskQuestion 'Al Pacino'
EXEC HelloWorld.AskQuestion 'John Belushi'
EXEC HelloWorld.AskQuestion 'Elijah Wood'

-- Movie location references
EXEC HelloWorld.AskQuestion 'Tatooine'
EXEC HelloWorld.AskQuestion 'Sicily'
EXEC HelloWorld.AskQuestion 'Faber College'
EXEC HelloWorld.AskQuestion 'Mordor'

-- Movie genres
EXEC HelloWorld.AskQuestion 'Science fiction'
EXEC HelloWorld.AskQuestion 'Crime'
EXEC HelloWorld.AskQuestion 'Comedy'
EXEC HelloWorld.AskQuestion 'Fantasy/Adventure'

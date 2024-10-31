/*
	*** Vector Search ***
*/

-- Movie phrases
EXEC VectorSearch 'May the force be with you'
EXEC VectorSearch 'I''m gonna make him an offer he can''t refuse'
EXEC VectorSearch 'Toga party'
EXEC VectorSearch 'One ring to rule them all'

-- Movie characters
EXEC VectorSearch 'Luke Skywalker'
EXEC VectorSearch 'Don Corleone'
EXEC VectorSearch 'James Blutarsky'
EXEC VectorSearch 'Gandalf'

-- Movie actors
EXEC VectorSearch 'Mark Hamill'
EXEC VectorSearch 'Al Pacino'
EXEC VectorSearch 'John Belushi'
EXEC VectorSearch 'Elijah Wood'

-- Movie location references
EXEC VectorSearch 'Tatooine'
EXEC VectorSearch 'Sicily'
EXEC VectorSearch 'Faber College'
EXEC VectorSearch 'Mordor'

-- Movie genres
EXEC VectorSearch 'Science fiction'
EXEC VectorSearch 'Crime'
EXEC VectorSearch 'Comedy'
EXEC VectorSearch 'Fantasy/Adventure'

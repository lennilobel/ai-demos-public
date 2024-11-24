/*
	*** Drop Database ***
*/

DECLARE @SessionId int

DECLARE curSession CURSOR FOR
 SELECT session_id FROM sys.dm_exec_sessions WHERE database_id = DB_ID('MoviesDB') AND session_id <> @@SPID

OPEN curSession
FETCH NEXT FROM curSession INTO @SessionId

WHILE @@FETCH_STATUS = 0
BEGIN
	PRINT CONCAT('KILL ', @SessionId)
    EXEC ('KILL ' + @SessionId)
    FETCH NEXT FROM curSession INTO @SessionId
END

CLOSE curSession
DEALLOCATE curSession
GO

DROP DATABASE MoviesDB

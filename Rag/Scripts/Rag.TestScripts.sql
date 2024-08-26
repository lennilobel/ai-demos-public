/* SQL Server 2022 */

SELECT COUNT(*) / 3072 FROM [rag-demo-3l].dbo.MovieVector
SELECT COUNT(*) / 1536 FROM [rag-demo-3s].dbo.MovieVector
SELECT COUNT(*) / 1536 FROM [rag-demo-ada].dbo.MovieVector

/* Azure SQL Database */
SELECT COUNT(*) FROM Movie WHERE Vectors IS NOT NULL	-- EAP
SELECT COUNT(*) / 3072 FROM MovieVector					-- 3072 dimensions (3l)
SELECT COUNT(*) / 1536 FROM MovieVector					-- 1536 dimensions (3s, ada)
DECLARE @Endpoint varchar(max) = 'https://lenni-openai.openai.azure.com/'
DECLARE @ApiKey varchar(max) = '1e981882b329481ebe4b2bfa261f8dce'
DECLARE @ModelName varchar(max)
SET @ModelName = 'lenni-text-embedding-3-large'		-- 3l
SET @ModelName = 'lenni-text-embedding-3-small'		-- 3s
SET @ModelName = 'lenni-text-embedding-ada-002'		-- ada

EXEC AskQuestion 'Can you recommend some good sci-fi movies?', @Endpoint, @ApiKey, @ModelName

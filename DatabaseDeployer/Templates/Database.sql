IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = '~DATABASENAME~')
BEGIN
	CREATE DATABASE ~DATABASENAME~
END

USE ~DATABASENAME~

-- RobloxDataAccess database user configuration
IF NOT EXISTS (SELECT [name]
                FROM [sys].[database_principals]
                WHERE [type] = N'S' AND [name] = N'RobloxDataAccess')
BEGIN
	-- Permission configuration
	IF NOT EXISTS (SELECT [name]
					FROM [sys].[database_principals]
					WHERE [type] = N'R' AND [name] = N'db_executor')
	BEGIN
		-- Create a db_executor role
		CREATE ROLE db_executor
		GRANT EXECUTE TO db_executor
	END

	-- Create RobloxDataAccess DB user
	CREATE USER RobloxDataAccess FROM LOGIN RobloxDataAccess
	ALTER ROLE db_datareader ADD MEMBER RobloxDataAccess
	ALTER ROLE db_datawriter ADD MEMBER RobloxDataAccess
	ALTER ROLE db_executor ADD MEMBER RobloxDataAccess
END

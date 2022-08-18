/* Standard Get-Or-Create */
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[~PROCEDURE~]') AND type in (N'P', N'PC'))
BEGIN
	EXEC('CREATE PROCEDURE [dbo].[~PROCEDURE~] AS BEGIN SET NOCOUNT ON; END')
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


ALTER PROCEDURE [dbo].[~PROCEDURE~]
(
	@CreatedNewEntity   [BIT] OUTPUT,
	~SQLINPUTPARAMETERLIST~
)
AS

SET NOCOUNT ON

IF NOT EXISTS
(
	SELECT ID FROM [dbo].[~TABLENAME~]
	WHERE
	(
		~SQLPARAMETERLIST~
	)
)
    BEGIN
		/* No entity found. Create new entity */
		INSERT INTO
		[~TABLENAME~]
		(
			~INSERTCOLUMNLIST~
		)
		VALUES
		(
			~SQLINSERTPARAMETERLIST~
		)
        SET @CreatedNewEntity = 1;
    END
ELSE
    BEGIN
		/* Entity already exists. Don't create new entity */
        SET @CreatedNewEntity = 0;
    END

/* Get new or pre-existing entity */
EXEC [dbo].[~GETPROCEDURE~]
	~EXECPARAMS~;

SET NOCOUNT OFF

RETURN
GO
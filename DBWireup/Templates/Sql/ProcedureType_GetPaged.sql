﻿/* Paged Get */
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
	~SQLINPUTPARAMETERLIST~
)
AS

SET NOCOUNT ON


DECLARE @Offset [int] = (@StartRowIndex-1)*@MaximumRows

IF (@Offset < 1)
BEGIN
    SET @Offset = 0;
END

IF (@MaximumRows < 0)
BEGIN
    SELECT
        [ID]
    FROM
        [dbo].[~TABLENAME~]
    ~if(!(NOPARAMS))~WHERE
    (
        ~SQLPARAMETERLIST~
    )~endif~
    ORDER BY [ID]
    OFFSET @Offset ROWS
END
ELSE
BEGIN
    SELECT
        [ID]
    FROM
        [dbo].[~TABLENAME~]
    ~if(!(NOPARAMS))~WHERE
    (
        ~SQLPARAMETERLIST~
    )~endif~
    ORDER BY [ID]
    OFFSET @Offset ROWS FETCH NEXT @MaximumRows ROWS ONLY
END

SET NOCOUNT OFF

RETURN
GO

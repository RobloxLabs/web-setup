﻿/* Standard Get Count */
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[~PROCEDURE~]') AND type in (N'P', N'PC'))
BEGIN
	EXEC('CREATE PROCEDURE [dbo].[~PROCEDURE~] AS BEGIN SET NOCOUNT ON; END')
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


ALTER PROCEDURE [dbo].[~PROCEDURE~]
~if(!(NOPARAMS))~(
	~SQLINPUTPARAMETERLIST~
)~endif~
AS

SET NOCOUNT ON

SELECT
    ~if(COUNTBIG)~COUNT_BIG(*)~else~COUNT(*)~endif~
FROM
    [dbo].[~TABLENAME~]
~if(!(NOPARAMS))~WHERE
(
    ~SQLPARAMETERLIST~
)~endif~

SET NOCOUNT OFF

RETURN
GO

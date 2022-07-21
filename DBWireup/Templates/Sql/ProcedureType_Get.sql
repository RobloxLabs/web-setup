﻿/* Standard Get */
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

SELECT
~COLUMNLIST~
FROM
	[~TABLENAME~]
WHERE
(
~SQLPARAMETERLIST~
)

SET NOCOUNT OFF

RETURN
GO

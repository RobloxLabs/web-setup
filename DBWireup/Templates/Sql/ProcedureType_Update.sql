﻿/* Standard Update */
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[~PROCEDURE~]') AND type in (N'P', N'PC'))
BEGIN
	EXEC('CREATE PROCEDURE [dbo].[~PROCEDURE~] AS BEGIN SET NOCOUNT ON; END')
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

ALTER PROCEDURE [dbo].[~PROCEDURE~]
(
~SQLINPUTPARAMETERLIST~
)
AS

SET NOCOUNT ON

UPDATE
	[~TABLENAME~]
SET
~SETVALUES~
WHERE
(
~SQLPARAMETERLIST~
)

SET NOCOUNT OFF

RETURN

GO

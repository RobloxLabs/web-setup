using System;

namespace SetupCommon
{
    /// <summary>
    /// Defines the procedure types for the database schema
    /// </summary>
    public enum ProcedureType
    {
        Delete,
        Insert,
        Update,
        Get,
        GetOrCreate,
        GetPaged,
        GetTotal,
        GetCount,
        MultiGet
    }
}

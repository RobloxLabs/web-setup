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

        // Everything here and onwards will utilize method parameters
        // instead of directly accessing the properties
        Get,
        GetOrCreate,
        GetPaged,
        GetCount,
        MultiGet
    }
}

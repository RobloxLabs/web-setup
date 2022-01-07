using System.Collections.Generic;
using System.ComponentModel;

namespace SetupCommon
{
    public class Database
    {
        /// <summary>
        /// Name of the database in MSSQL
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// What to name the repository for .NET Core repository templates.
        /// If null, don't generate a .NET Core repository
        /// </summary>
        public string RepositoryName { get; set; }

        /// <summary>
        /// What namespace to put the .NET Core repository template.
        /// If null, it defaults to Roblox.
        /// </summary>
        [DefaultValue("Roblox")]
        public string RepositoryNamespace { get; set; }

        /// <summary>
        /// List of all entities (tables) in the database
        /// </summary>
        public List<Entity> Entities { get; set; }
    }
}

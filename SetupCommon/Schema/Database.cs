using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;

namespace SetupCommon
{
    [DebuggerDisplay("{Name} Database")]
    [Serializable]
    public class Database
    {
        /// <summary>
        /// Name of the database in MSSQL
        /// </summary>
        [XmlAttribute]
        public string Name { get; set; }

        /// <summary>
        /// What to name the repository for .NET Core repository templates.
        /// If null, don't generate a .NET Core repository
        /// </summary>
        [XmlElement(IsNullable = true)]
        public string RepositoryName { get; set; }

        /// <summary>
        /// What namespace to put the .NET Core repository template.
        /// If null, it defaults to Roblox.
        /// </summary>
        [XmlElement(IsNullable = true)]
        public string RepositoryNamespace { get; set; }

        /// <summary>
        /// List of all entities (tables) in the database
        /// </summary>
        [XmlArray]
        public List<Entity> Entities { get; set; }

        public Database()
        {
            RepositoryNamespace = "Roblox";
        }
    }
}

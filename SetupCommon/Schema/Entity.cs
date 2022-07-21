using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;

namespace SetupCommon
{
    [DebuggerDisplay("{Name}")]
    [Serializable]
    public class Entity
    {
        /// <summary>
        /// Name of the entity.
        /// </summary>
        [XmlAttribute]
        public string Name { get; set; }

        /// <summary>
        /// C# namespace for the entity.
        /// </summary>
        [XmlAttribute("Namespace")]
        public string EntityNamespace { get; set; }

        /// <summary>
        /// MSSQL table name.
        /// </summary>
        [XmlAttribute("Table")]
        public string TableName { get; set; }

        /// <summary>
        /// Whether or not functions and properties will be marked as internal.
        /// </summary>
        [XmlAttribute]
        public bool IsInternal { get; set; }

        /// <summary>
        /// List of all properties (columns for MSSQL table).
        /// </summary>
        [XmlArray]
        public List<Property> Properties { get; set; }

        /// <summary>
        /// List of all MSSQL procedures for the entity.
        /// </summary>
        [XmlArray]
        public List<Procedure> Procedures { get; set; }

        public Entity()
        {
            EntityNamespace = "Roblox";
            IsInternal = false;
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using System.Xml.Schema;

namespace SetupCommon
{
    [Serializable]
    public class Entity : IEntity
    {
        /// <summary>
        /// Name of the entity.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// C# namespace for the entity.
        /// </summary>
        [DefaultValue("Roblox")]
        public string EntityNamespace { get; set; }

        /// <summary>
        /// MSSQL table name.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Type of the ID For C#
        /// </summary>
        public string IdType { get; set; }

        /// <summary>
        /// Type of the ID for MSSQL
        /// </summary>
        public string SqlIdType { get; set; }

        /// <summary>
        /// Whether or not the ID should auto-increment in the table
        /// </summary>
        public bool IdAutoIncrement { get; set; }

        /// <summary>
        /// Whether or not functions and properties will be marked as internal
        /// </summary>
        public bool IsInternal { get; set; }

        /// <summary>
        /// What type of cache to generate for .NET Core repositories
        /// If null, don't generate a .NET Core repository
        /// </summary>
        public CacheType CacheType { get; set; }

        /// <summary>
        /// List of all properties (columns for MSSQL)
        /// </summary>
        public List<Property> Properties { get; set; }

        public Entity ToStandardEntity()
        {
            return new Entity()
            {
                Name = Name,
                EntityNamespace = EntityNamespace,
                TableName = TableName,
                IsInternal = IsInternal,
                //CacheType = CacheType,
                Properties = Properties
            };
        }

        #region IXmlSerializable Members

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteXml(XmlWriter writer)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

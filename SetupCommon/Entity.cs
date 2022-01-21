using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;

namespace SetupCommon
{
    [Serializable]
    public class Entity
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
        [DefaultValue("int")]
        public string IdType { get; set; }

        /// <summary>
        /// Type of the ID for MSSQL
        /// </summary>
        [DefaultValue("INT")]
        public string SqlIdType { get; set; }

        /// <summary>
        /// Whether or not the ID should auto-increment in the table
        /// </summary>
        public bool IdAutoIncrement { get; set; }

        /// <summary>
        /// Whether or not functions and properties will be marked as internal.
        /// </summary>
        [DefaultValue(false)]
        public bool IsInternal { get; set; }

        /// <summary>
        /// List of all properties (columns for MSSQL table).
        /// </summary>
        public List<Property> Properties { get; set; }

        private void WriteIDProperty()
        {
            // TODO: Why do we even do this?
            Properties.Insert(0, new Property()
            {
                Name = "ID",
                Type = IdType,
                SqlType = SqlIdType
            });
        }

        #region XML Serialization Members

        public void ReadXml(XmlElement entity)
        {
            // Read attributes //

            // Name
            Name = entity.GetAttribute("Name");

            // Entity Object Namespace
            EntityNamespace = SchemaHelper.ReadXmlAttributeString(entity, "Namespace") ?? 
                "Roblox";

            // SQL Table Name
            TableName = SchemaHelper.ReadXmlAttributeString(entity, "Table") ?? 
                string.Format(SetupCommon.Properties.Settings.Default.DefaultTableName, Name);

            // Is Internal
            IsInternal = SchemaHelper.ReadXmlAttributeBool(entity, "Internal");

            // ID Type
            IdType = SchemaHelper.ReadXmlAttributeString(entity, "IdType") ??
                "int";

            // SQL ID Type
            SqlIdType = SchemaHelper.ReadXmlAttributeString(entity, "SqlIdType") ??
                "INT";

            // Dumb weird hack
            bool idAutoIncrement = false;
            if (!SchemaHelper.TryReadXmlAttributeBool(entity, "IdAutoIncrement", out idAutoIncrement))
                IdAutoIncrement = false;
            else
                IdAutoIncrement = idAutoIncrement;

            // Read properties
            XmlNode props = entity.FirstChild;
            if (props.HasChildNodes)
            {
                Properties = new List<Property>();
                foreach (XmlNode propNode in props.ChildNodes)
                {
                    if (propNode.NodeType == XmlNodeType.Element)
                    {
                        Property prop = new Property();
                        prop.ReadXml((XmlElement)propNode);
                        Properties.Add(prop);
                    }
                }
            }
            else
            {
                throw new XmlException($"No properties defined for entity: \"{Name}\"");
            }

            WriteIDProperty();
        }

        public void WriteXml(XmlElement parent)
        {
            XmlDocument root = parent.OwnerDocument;

            XmlElement entity = root.CreateElement("Entity");

            // Write attributes //

            // Name
            entity.SetAttribute("Name", Name);

            // Namespace
            if (EntityNamespace != "Roblox")
                entity.SetAttribute("Namespace", EntityNamespace);

            // Table Name
            if (TableName != string.Format(SetupCommon.Properties.Settings.Default.DefaultTableName, Name))
                entity.SetAttribute("Table", TableName);

            // Is Internal
            if (IsInternal)
                entity.SetAttribute("Internal", IsInternal.ToString());

            // ID Type
            if (IdType != "int")
                entity.SetAttribute("IdType", IdType);

            // SQL ID Type
            if (SqlIdType.ToUpper() != "INT")
                entity.SetAttribute("SqlIdType", SqlIdType);

            // ID Auto-Increment
            if (!IdAutoIncrement)
                entity.SetAttribute("IdAutoIncrement", IdAutoIncrement.ToString());


            // Write properties //
            XmlElement props = root.CreateElement("Properties");
            foreach (Property prop in Properties)
            {
                // Serialize all properties other than ID
                if (prop.Name != "ID")
                    prop.WriteXml(props);
            }
            entity.AppendChild(props);
        }

        #endregion
    }
}

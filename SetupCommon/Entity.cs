using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SetupCommon
{
    [Serializable]
    public class Entity : IXmlSerializable
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
            Properties.Add(new Property()
            {
                Name = "ID",
                Type = IdType,
                SqlType = SqlIdType
            });
        }

        #region IXmlSerializable Members

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            Name = reader.GetAttribute("Name");
            EntityNamespace = reader.GetAttribute("Namespace") ?? "Roblox";
            TableName = reader.GetAttribute("Table") ?? string.Format(SetupCommon.Properties.Settings.Default.DefaultTableName, Name);
            IsInternal = SchemaHelper.ReadXmlAttributeBool(reader, "Internal");
            IdType = reader.GetAttribute("IdType") ?? "int";
            SqlIdType = reader.GetAttribute("SqlIdType") ?? "INT";
            // Dumb weird hack
            bool idAutoIncrement = false;
            if (!SchemaHelper.TryReadXmlAttributeBool(reader, "IdAutoIncrement", out idAutoIncrement))
                IdAutoIncrement = false;
            else
                IdAutoIncrement = idAutoIncrement;

            reader.ReadStartElement();
            if (reader.Name == "Properties")
            {
                Properties = new List<Property>();
                reader.ReadStartElement();
                while (reader.Name == "Property")
                {
                    Property prop = new Property();
                    prop.ReadXml(reader);
                    Properties.Add(prop);
                    reader.ReadStartElement();
                }
                reader.ReadEndElement();
            }

            if (Properties == null || Properties.Count == 0)
                throw new XmlException($"No properties defined for entity: \"{Name}\"");

            WriteIDProperty();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("Name", Name);
            if (EntityNamespace != "Roblox")
                writer.WriteAttributeString("Namespace", EntityNamespace);
            if (TableName != string.Format(SetupCommon.Properties.Settings.Default.DefaultTableName, Name))
                writer.WriteAttributeString("Table", TableName);
            if (IsInternal)
                writer.WriteAttributeString("Internal", IsInternal.ToString());
            if (IdType != "int")
                writer.WriteAttributeString("IdType", IdType);
            if (SqlIdType.ToUpper() != "INT")
                writer.WriteAttributeString("SqlIdType", SqlIdType);
            if (!IdAutoIncrement)
                writer.WriteAttributeString("IdAutoIncrement", IdAutoIncrement.ToString());

            writer.WriteStartElement("Properties");
            foreach (Property prop in Properties)
            {
                // Serialize all properties other than ID
                if (prop.Name != "ID")
                {
                    writer.WriteStartElement("Property");
                    prop.WriteXml(writer);
                    writer.WriteEndElement();
                }
            }
            writer.WriteEndElement();
        }

        #endregion
    }
}

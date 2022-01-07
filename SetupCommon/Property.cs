using System;
using System.ComponentModel;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SetupCommon
{
    [Serializable]
    public class Property : IXmlSerializable
    {
        /// <summary>
        /// Name of the property
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// C# type of the property
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// MSSQL type of the property
        /// </summary>
        public string SqlType { get; set; }

        /// <summary>
        /// Whether or not to generate a GetByFK method and SQL procedure for the containing entity.
        /// </summary>
        [DefaultValue(false)]
        public bool IsForeignKey { get; set; }

        /// <summary>
        /// Whether or not the property value is nullable.
        /// </summary>
        [DefaultValue(false)]
        public bool IsNullable { get; set; }

        #region IXmlSerializable Members

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            Name = reader.GetAttribute("Name");
            Type = reader.GetAttribute("Type");
            SqlType = reader.GetAttribute("SqlType");
            IsForeignKey = SchemaHelper.ReadXmlAttributeBool(reader, "IsForeignKey");
            IsNullable = Type.EndsWith("?");
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("Name", Name);
            writer.WriteAttributeString("Type", Type);
            writer.WriteAttributeString("SqlType", SqlType);
            if (IsForeignKey)
                writer.WriteAttributeString("IsForeignKey", IsForeignKey.ToString());
        }

        #endregion
    }
}

using System;
using System.ComponentModel;
using System.Xml;

namespace SetupCommon
{
    [Serializable]
    public class Property
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

        #region XML Serialization Members

        public void ReadXml(XmlElement property)
        {
            // Read attributes
            Name = property.GetAttribute("Name");
            Type = property.GetAttribute("Type");
            SqlType = property.GetAttribute("SqlType");
            IsForeignKey = SchemaHelper.ReadXmlAttributeBool(property, "IsForeignKey");
            IsNullable = Type.EndsWith("?");
        }

        public void WriteXml(XmlElement parent)
        {
            XmlElement prop = parent.OwnerDocument.CreateElement("Property");
            prop.SetAttribute("Name", Name);
            prop.SetAttribute("Type", Type);
            prop.SetAttribute("SqlType", SqlType);
            if (IsForeignKey)
                prop.SetAttribute("IsForeignKey", IsForeignKey.ToString());
        }

        #endregion
    }
}

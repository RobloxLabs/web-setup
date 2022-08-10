using System;
using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;

namespace SetupCommon
{
    [DebuggerDisplay("{Name}")]
    [Serializable]
    public class Property
    {
        /// <summary>
        /// Name of the property
        /// </summary>
        [XmlAttribute]
        public string Name { get; set; }

        /// <summary>
        /// C# type of the property
        /// </summary>
        [XmlAttribute]
        public string Type { get; set; }

        /// <summary>
        /// MSSQL type of the property
        /// </summary>
        [XmlAttribute]
        public string SqlType { get; set; }

        /// <summary>
        /// Whether or not the property is the primary key for the table.
        /// </summary>
        [XmlAttribute]
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// Whether or not the property value is nullable.
        /// </summary>
        [XmlAttribute]
        public bool IsNullable { get; set; }

        /// <summary>
        /// Whether or not the value of the property is read-only.
        /// </summary>
        [XmlAttribute]
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// The default value of the property
        /// </summary>
        [XmlAttribute("Default")]
        public string DefaultValue { get; set; }

        public Property()
        {
            IsPrimaryKey = false;
            IsNullable = false;
            IsReadOnly = false;
        }
    }
}

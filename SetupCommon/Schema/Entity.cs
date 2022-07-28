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
        private Property[] _PropertiesXml;

        /// <summary>
        /// Dictionary of all properties. Indexed by name.
        /// </summary>
        private Dictionary<string, Property> _PropertyDictionary;

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
        public Property[] Properties
        {
            get
            {
                return _PropertiesXml;
            }
            set
            {
                if (_PropertiesXml != value)
                {
                    _PropertiesXml = value;
                    // HACK: Super fucking dumb hack cuz of XML serialization. Properties can't be a List for some reason?
                    _PropertyDictionary = new Dictionary<string, Property>();
                    // Update the property dictionary
                    foreach (Property prop in _PropertiesXml)
                        _PropertyDictionary.Add(prop.Name, prop);
                }
            }
        }

        /// <summary>
        /// List of all MSSQL procedures for the entity.
        /// </summary>
        [XmlArray]
        public Procedure[] Procedures { get; set; }

        public Entity()
        {
            EntityNamespace = "Roblox";
            IsInternal = false;
        }

        public Property GetProperty(string name)
        {
            return _PropertyDictionary[name];
        }

        public Property GetIDProperty()
        {
            return GetProperty("ID");
        }
    }
}

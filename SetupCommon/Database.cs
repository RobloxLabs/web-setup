using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;

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

        #region XML Serialization Members

        public void ReadXml(XmlElement db)
        {
            // Read attributes
            Name = db.GetAttribute("Name");

            // Read config
            RepositoryName = db["RepositoryName"].Value;
            RepositoryNamespace = db["RepositoryNamespace"].Value;
        }

        public void WriteXml(XmlElement parent)
        {
            XmlDocument root = parent.OwnerDocument;
            XmlElement db = root.CreateElement("Database");

            // Set Name
            db.SetAttribute("Name", Name);

            // Write config
            root.CreateElement("RepositoryName").Value = RepositoryName;
            root.CreateElement("RepositoryNamespace").Value = RepositoryNamespace;
        }

        #endregion
    }
}

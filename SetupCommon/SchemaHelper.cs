using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Text.Json;

namespace SetupCommon
{
    public class SchemaHelper
    {
        public static List<Database> ReadSchemaDirectory(string path)
        {
            if (path == null || !Directory.Exists(path))
                throw new FileNotFoundException($"Invalid path for schema: \"{path}\"");

            List<Database> databases = new List<Database>();

            string[] subDirectories = Directory.GetDirectories(path);
            foreach (string subDirectory in subDirectories)
            {
                databases.Add(ReadDatabaseDirectory(subDirectory));
            }

            return databases;
        }

        public static Database ReadDatabaseDirectory(string path)
        {
            if (path == null || !Directory.Exists(path))
                throw new FileNotFoundException($"Invalid path for database: \"{path}\"");

            Database database = new Database();
            {
                string[] pathSplit = path.Split(Path.DirectorySeparatorChar);
                database.Name = pathSplit[pathSplit.Length - 1];
            }

            if (database.Name.Length == 0)
                throw new ApplicationException($"Failed to find DB name for path: \"{path}\"");

            List<Entity> entities = new List<Entity>();

            string[] files = Directory.GetFiles(path);
            foreach (string file in files)
            {
                if (!file.EndsWith(".xml"))
                    continue;

                if (file.EndsWith(".config.xml"))
                    ReadDatabaseConfig(file, ref database);
                else
                    entities.Add(ReadXmlEntity(file));
            }

            database.Entities = entities;

            return database;
        }

        public static Entity ReadXmlEntity(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Failed to open serialized entity at \"{path}\"");

            using (FileStream fs = File.OpenRead(path))
            {
                Entity entity = new Entity();
                XmlDocument root = new XmlDocument();

                root.Load(fs);
                // Technically can support reading multiple entities from the same file
                foreach (XmlNode node in root.ChildNodes)
                {
                    if (node.NodeType == XmlNodeType.Element)
                        entity.ReadXml((XmlElement)node); // Reads from the first node in the document
                }

                return entity;
            }
        }

        public static void WriteXmlEntity(Entity entity, string path)
        {
            using (FileStream fs = File.Create(path))
            {
                XmlDocument root = new XmlDocument();

                root.Load(fs);
                entity.WriteXml(root.DocumentElement); // Writes to the root node of the document
            }
        }

        public static void WriteJsonEntity(Entity entity, string path)
        {
            using (FileStream fs = File.Create(path))
            {
                JsonSerializer.Serialize(fs, entity);
            }
        }

        public static bool TryReadXmlAttributeBool(XmlElement element, string attributeName, out bool attributeBool)
        {
            string attrStr = element.GetAttribute(attributeName);
            attributeBool = false;

            if (string.IsNullOrEmpty(attrStr))
            {
                return false;
            }
            else
            {
                if (bool.TryParse(attrStr, out attributeBool))
                    return true;
                else
                    throw new XmlException($"Invalid boolean for {attributeName} attribute: \"{attrStr}\"");
            }
        }

        public static bool ReadXmlAttributeBool(XmlElement element, string attributeName)
        {
            bool value;
            TryReadXmlAttributeBool(element, attributeName, out value);
            return value;
        }

        public static string ReadXmlAttributeString(XmlElement element, string attributeName)
        {
            if (!string.IsNullOrEmpty(element.GetAttribute(attributeName)))
                return element.GetAttribute(attributeName);
            else
                return null;
        }

        public static void ReadDatabaseConfig(string path, ref Database database)
        {
            XmlDocument document = new XmlDocument();
            document.Load(path);

            XmlNodeList repositoryNameNodes = document.GetElementsByTagName("repositoryName");
            if (repositoryNameNodes.Count != 0)
                database.RepositoryName = repositoryNameNodes[0].InnerText;

            XmlNodeList repositoryNamespaceNodes = document.GetElementsByTagName("repositoryNamespace");
            if (repositoryNamespaceNodes.Count != 0)
                database.RepositoryNamespace = repositoryNamespaceNodes[0].InnerText;
        }
    }
}

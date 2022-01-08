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
                XmlSerializer serializer = new XmlSerializer(typeof(Entity));
                return (Entity)serializer.Deserialize(fs);
            }
        }

        public static void WriteXmlEntity(Entity entity, string path)
        {
            using (FileStream fs = File.Create(path))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Entity));
                serializer.Serialize(fs, entity);
            }
        }

        public static void WriteJsonEntity(Entity entity, string path)
        {
            using (FileStream fs = File.Create(path))
            {
                JsonSerializer.Serialize(fs, entity);
            }
        }

        public static bool ReadXmlAttributeBool(XmlReader reader, string attribute)
        {
            string attrStr = reader.GetAttribute(attribute);
            bool attrBool = false;
            if (!string.IsNullOrEmpty(attrStr) && !bool.TryParse(attrStr, out attrBool))
                throw new XmlException($"Invalid boolean for {attribute} attribute: \"{attrStr}\"");
            return attrBool;
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

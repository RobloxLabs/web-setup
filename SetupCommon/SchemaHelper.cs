﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace SetupCommon
{
    public class SchemaHelper
    {
        public static string GetEntityFilePath(Database database, Entity entity)
        {
            return $"{database.Name}\\{entity.Name}.xml";
        }

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
                {
                    if (SetupCommon.Properties.Settings.Default.UseLegacyEntityRead)
                        entities.Add(ReadLegacyEntity(file));
                    else
                        entities.Add(ReadXmlEntity(file));
                }
            }

            database.Entities = entities;

            return database;
        }

        public static Entity ReadLegacyEntity(string path)
        {
            XmlDocument document = new XmlDocument();
            document.Load(path);
            XmlElement root = document.DocumentElement;

            // ID info
            string idType = root.HasAttribute("idType") ? root.GetAttribute("idType") : "int"; // default to int
            string sqlIdType = root.HasAttribute("sqlIdType") ? root.GetAttribute("sqlIdType") : "Int"; // default to Int
            bool idAutoIncrement = true; // default to True
            // Parse ID auto-increment bool
            if (root.HasAttribute("idAutoIncrement") && !bool.TryParse(root.GetAttribute("idAutoIncrement"), out idAutoIncrement))
                throw new Exception($"Invalid bool for idAutoIncrement: {root.GetAttribute("idAutoIncrement")}");

            // Cache info
            CacheType cacheType = CacheType.Regular;
            // Parse CacheType enum
            if (root.HasAttribute("cacheType") && !Enum.TryParse(root.GetAttribute("cacheType"), out cacheType))
                throw new Exception($"Invalid cache type: {root.GetAttribute("cacheType")}");

            List<Property> properties = new List<Property>();
            properties.Add(new Property() { Name = "ID", Type = idType, SqlType = sqlIdType});

            bool isDated = root.HasAttribute("dated") ? bool.Parse(root.GetAttribute("dated")) : true;
            if (isDated)
            {
                properties.Add(new Property() { Name = "Created", Type = "DateTime", SqlType = "DateTime" });
                properties.Add(new Property() { Name = "Updated", Type = "DateTime", SqlType = "DateTime" });
            }

            XmlNodeList xmlProperties = document.GetElementsByTagName("property");
            foreach (XmlNode property in xmlProperties)
            {
                string propertyName = property.Attributes["name"].Value;
                string propertyType = property.Attributes["type"].Value;
                string propertySqlType = property.Attributes["sqlType"].Value;
                bool isNullable = propertyType.EndsWith("?");

                properties.Add(new Property() { Name = propertyName, Type = propertyType, SqlType = propertySqlType, IsNullable = isNullable });
            }

            return new Entity() {
                Name = root.GetAttribute("name"), 
                EntityNamespace = root.GetAttribute("namespace"), 
                TableName = root.GetAttribute("table"),
                IdType = idType,
                SqlIdType = sqlIdType,
                IdAutoIncrement = idAutoIncrement,
                IsInternal = root.HasAttribute("internal") ? bool.Parse(root.GetAttribute("internal")) : false,
                //CacheType = cacheType,
                Properties = properties
            };
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
            using (TextWriter fs = File.CreateText(path))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(fs, entity);
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

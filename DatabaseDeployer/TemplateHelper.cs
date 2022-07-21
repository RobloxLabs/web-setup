using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Antlr4.StringTemplate;
using SetupCommon;

namespace DatabaseDeployer
{
    internal class TemplateHelper
    {
        private static Dictionary<string, string> Templates = new Dictionary<string, string>();

        /// <summary>
        /// Reads templates
        /// </summary>
        internal static void Setup()
        {
            if (!Directory.Exists("Templates"))
                throw new DirectoryNotFoundException("Template directory is missing");

            foreach (string file in Directory.GetFiles("Templates"))
            {
                string templateName;
                {
                    string[] pathSplit = file.Split(Path.DirectorySeparatorChar);
                    templateName = pathSplit[pathSplit.Length - 1].Split('.')[0];
                }

                Templates.Add(templateName, File.ReadAllText(file));
            }
        }

        /// <summary>
        /// Creates a new Antlr4 Template and fills out basic values
        /// </summary>
        /// <param name="baseTemplate"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        private static Template GetNewTemplate(string baseTemplate, Database database = null)
        {
            Template template = new Template(baseTemplate, '~', '~');
            if (database != null)
                template.Add("DATABASENAME", database.Name);

            return template;
        }

        internal static string FillDatabaseTemplate(Database database)
        {
            Template template = GetNewTemplate(Templates["Database"], database);

            return template.Render();
        }

        internal static string FillTablesTemplate(Database database)
        {
            StringBuilder tables = new StringBuilder();

            foreach (Entity entity in database.Entities)
            {
                tables.AppendLine(FillTableTemplate(entity));
                //tables.AppendLine("GO");
            }

            return tables.ToString();
        }

        internal static string FillTableTemplate(Entity entity)
        {
            StringBuilder tableSql = new StringBuilder();

            // SQL Definitions
            StringBuilder columns = new StringBuilder();
            StringBuilder primaryKeys = new StringBuilder();
            StringBuilder indexes = new StringBuilder();
            StringBuilder foreignKeys = new StringBuilder();
            StringBuilder defaults = new StringBuilder();

            foreach (Property property in entity.Properties)
            {
                string propertySql = $"\r\n\t[{property.Name}] {property.SqlType}";

                if (property.IsPrimaryKey)
                {
                    // If the property is a primary key, 99% it's gonna be the ID
                    // which is always a number of some kind starting at 1
                    propertySql += " IDENTITY(1,1) NOT FOR REPLICATION";

                    Template primaryKeyT = GetNewTemplate(Templates["PrimaryKey"]);
                    primaryKeyT.Add("TABLENAME", entity.TableName);
                    primaryKeyT.Add("PKNAME", property.Name);

                    primaryKeys.Append("\r\n");
                    primaryKeys.Append(primaryKeyT.Render());
                }

                if (property.DefaultValue != null)
                {
                    Template defaultValueT = GetNewTemplate(Templates["DefaultValue"]);
                    defaultValueT.Add("TABLENAME", entity.TableName);
                    defaultValueT.Add("COLUMNNAME", property.Name);
                    defaultValueT.Add("DEFAULTVALUE", property.DefaultValue);

                    defaults.Append("\r\n");
                    defaults.Append(defaultValueT.Render());
                }

                if (!property.IsNullable)
                    propertySql += " NOT";
                propertySql += " NULL";

                columns.Append(propertySql + ",");
            }

            Template tableT = GetNewTemplate(Templates["Table"]);
            tableT.Add("TABLENAME", entity.TableName);
            tableT.Add("PROPERTIES", columns);
            tableT.Add("PKCONSTRAINTS", primaryKeys);

            tableSql.Append(tableT.Render());
            tableSql.Append(defaults);

            return tableSql.ToString();
        }
    }
}

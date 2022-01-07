using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                throw new Exception("Template directory is missing");

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
        private static Template GetNewTemplate(string baseTemplate, Database database)
        {
            Template template = new Template(baseTemplate, '~', '~');
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
            List<string> tables = new List<string>();

            foreach (Entity entity in database.Entities)
            {
                List<string> tableProperties = new List<string>();

                foreach (Property property in entity.Properties)
                {
                    string egg = $"[{property.Name}] {property.SqlType}";
                    if (!property.IsNullable)
                        egg += " NOT";
                    egg += " NULL";
                    if (property.Name == "ID" && entity.IdAutoIncrement)
                        egg += " IDENTITY(1,1)";
                    tableProperties.Add(egg);
                }

                Template table = GetNewTemplate(Templates["Table"], database);
                table.Add("TABLENAME", entity.TableName);
                table.Add("TABLEPROPERTIES", string.Join(",\n", tableProperties.ToArray()));

                tables.Add(table.Render());
            }

            return string.Join(",\n", tables.ToArray());
        }
    }
}

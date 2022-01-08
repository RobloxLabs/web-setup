using System;
using System.Collections.Generic;
using System.IO;
using System.Data.SqlClient;
using SetupCommon;

namespace DatabaseDeployer
{
    /// <summary>
    /// Creates databases, tables and columns based on the given entity schema.
    /// </summary>
    public class Deployer : WebSetupApplication<ApplicationSettings>
    {
        /// <summary>
        /// The list of databases to deploy.
        /// </summary>
        private List<Database> DatabaseList { get; set; }

        /// <summary>
        /// Deploy databases to a designated Roblox database server.
        /// </summary>
        public void Deploy()
        {
            Console.WriteLine("Generating databases...");

            if (!Directory.Exists(SetupCommon.Properties.Settings.Default.SchemaDirectory))
                throw new DirectoryNotFoundException("Schema directory does not exist!");

            TemplateHelper.Setup();
            DatabaseList = SchemaHelper.ReadSchemaDirectory(SetupCommon.Properties.Settings.Default.SchemaDirectory);
            Console.WriteLine("Done!");

            string masterConnectionString;
            if (!Config.ConnectionStrings.TryGetValue("master", out masterConnectionString))
                throw new ApplicationException("Unable to find master database connection string in ApplicationSettings");

            Console.WriteLine("Deploying databases...");
            using (SqlConnection connection = new SqlConnection(masterConnectionString))
            {
                connection.Open();

                foreach (Database database in DatabaseList)
                {
                    string databaseSql = TemplateHelper.FillDatabaseTemplate(database);
                    string tableSql = TemplateHelper.FillTablesTemplate(database);

                    SqlCommand databaseCommand = new SqlCommand(databaseSql, connection);
                    databaseCommand.ExecuteNonQuery();

                    // If no tables exist for a database, tableSql is empty.
                    if (!string.IsNullOrEmpty(tableSql))
                    {
                        SqlCommand tableCommand = new SqlCommand(tableSql, connection);
                        tableCommand.ExecuteNonQuery();
                    }

                    Console.WriteLine($"Deployed {database.Name}");
                }
            }
            Console.WriteLine($"Done! Generated and deployed {DatabaseList.Count} databases.");
        }
    }
}

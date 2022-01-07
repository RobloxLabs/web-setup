using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.SqlClient;
using Newtonsoft.Json;
using SetupCommon;

namespace DatabaseDeployer
{
    public class Deployer
    {
        public static void Run()
        {
            if (!File.Exists("config.json"))
            {
                Console.WriteLine("Config file does not exist");
                return;
            }

            Config config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));

            if (!Directory.Exists(SetupCommon.Properties.Settings.Default.SchemaDirectory))
            {
                Console.WriteLine("Schema directory does not exist");
                return;
            }

            TemplateHelper.Setup();

            List<Database> databases = SchemaHelper.ReadSchemaDirectory(SetupCommon.Properties.Settings.Default.SchemaDirectory);

            using (SqlConnection connection = new SqlConnection(config.ConnectionString))
            {
                connection.Open();

                foreach (Database database in databases)
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

                    Console.WriteLine($"Finished {database.Name}");
                }

                Console.WriteLine($"Done! Generated {databases.Count} databases");
            }
        }
    }
}

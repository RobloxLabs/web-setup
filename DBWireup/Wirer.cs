using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
//using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using SetupCommon;

namespace DBWireup
{
    public class Wirer
    {
        public static void Run()
        {
            if (!File.Exists("config.json"))
            {
                Console.WriteLine("Config file does not exist");
                return;
            }

            Config config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
            Dictionary<string, string> connectionStrings = GetConnectionStrings(config);

            if (!Directory.Exists("Schema"))
            {
                Console.WriteLine("Schema directory does not exist");
                return;
            }

            if (!Directory.Exists("Out"))
                Directory.CreateDirectory("Out");

            TemplateHelper.Setup();

            List<SetupCommon.Database> databases = SchemaHelper.ReadSchemaDirectory(SetupCommon.Properties.Settings.Default.SchemaDirectory);
            foreach (SetupCommon.Database database in databases)
            {
                // TODO: fill repository, repository settings, and a models.
                string databaseOutDirectory = Path.Combine("Out", database.Name);
                if (!Directory.Exists(databaseOutDirectory))
                    Directory.CreateDirectory(databaseOutDirectory);

                using (SqlConnection connection = new SqlConnection())
                {
                    string connectionString = connectionStrings.ContainsKey(database.Name) ? connectionStrings[database.Name] : "";
                    // Don't generate the entity if we aren't going to be able to write it to the database
                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        connection.ConnectionString = connectionStrings[database.Name]/* + $"Initial Catalog={database.Name};"*/;
                        //connection.Open();
                        ServerConnection svrConnection = new ServerConnection(connection);
                        Server server = new Server(svrConnection);


                        foreach (Entity entity in database.Entities)
                        {
                            string entityOutDirectory = Path.Combine(databaseOutDirectory, entity.Name);
                            if (!Directory.Exists(entityOutDirectory))
                                Directory.CreateDirectory(entityOutDirectory);

                            //File.WriteAllText(Path.Combine(entityOutDirectory, $"I{entity.Name}.cs"), TemplateHelper.FillSqlTemplate(entity));

                            // Fill all .NET Framework templates
                            File.WriteAllText(Path.Combine(entityOutDirectory, $"I{entity.Name}.cs"), TemplateHelper.FillInterfaceTemplate(entity));
                            File.WriteAllText(Path.Combine(entityOutDirectory, $"{entity.Name}.cs"), TemplateHelper.FillBizTemplate(entity));
                            File.WriteAllText(Path.Combine(entityOutDirectory, $"{entity.Name}DAL.cs"), TemplateHelper.FillDalTemplate(entity, connectionString));

                            //SqlCommand command = new SqlCommand(TemplateHelper.FillSqlTemplate(entity), connection);
                            //command.ExecuteNonQuery();
                            server.ConnectionContext.ExecuteNonQuery(TemplateHelper.FillSqlTemplate(entity));
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Could not find connection string for {database.Name}, will not be generating MSSQL Procedures.");
                    }
                }

                Console.WriteLine($"Finished {database.Name}");
            }

            Console.WriteLine($"Done! Wired {databases.Count} databases");
        }

        private static Dictionary<string, string> GetConnectionStrings(Config config)
        {
            Dictionary<string, string> connectionStrings = new Dictionary<string, string>();

            using (SqlConnection connection = new SqlConnection(config.ConnectionString))
            {
                //connection.Open();

                //SqlCommand command = new SqlCommand("SELECT Name, Value FROM dbo.ConnectionStrings", connection);

                ServerConnection svrConnection = new ServerConnection(connection);
                Server server = new Server(svrConnection);

                //SqlDataReader dataReader = command.ExecuteReader();
                SqlDataReader dataReader = server.ConnectionContext.ExecuteReader("SELECT Name, Value FROM dbo.ConnectionStrings");
                while (dataReader.Read())
                {
                    connectionStrings.Add(dataReader["Name"].ToString(), dataReader["Value"].ToString());
                }
            }

            return connectionStrings;
        }
    }
}

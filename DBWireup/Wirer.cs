﻿using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using SetupCommon;

namespace DBWireup
{
    public class Wirer : WebSetupApplication<ApplicationSettings>
    {
        /// <summary>
        /// A dictionary containing connection strings for wiring up SQL procedures.
        /// </summary>
        private Dictionary<string, string> ConnectionStrings { get; set; }

        public void Wireup()
        {
            if (!Directory.Exists(SetupCommon.Properties.Settings.Default.SchemaDirectory))
                throw new DirectoryNotFoundException("Schema directory does not exist!");

            // Do this at the beginning of the method to avoid doing unnecessary work in the event it fails.
            // Fill the default connection string template with the required values.
            string connectionStringTemplate = string.Format(Config.ConnectionStrings["Template"],
                Config.DefaultDataSource,
                Config.DefaultUserID,
                Config.DefaultPassword);

            if (!Directory.Exists("Out"))
                Directory.CreateDirectory("Out");

            if (!Config.UseAutoGeneratedConnectionStrings)
            {
                Console.WriteLine("Getting connection strings from database...");
                ConnectionStrings = GetConnectionStrings();
            }

            Console.WriteLine("Setting up templates...");
            TemplateHelper.Setup();

            Console.WriteLine("Reading database schema...");
            List<SetupCommon.Database> databases = SchemaHelper.ReadSchemaDirectory(SetupCommon.Properties.Settings.Default.SchemaDirectory);

            Console.WriteLine("Wiring up databases...");
            int dbCounter = 0;
            foreach (SetupCommon.Database database in databases)
            {
                // Don't generate the entity if we aren't going to be able to write it to the database
                if (!Config.UseAutoGeneratedConnectionStrings && !ConnectionStrings.ContainsKey(database.Name))
                {
                    Console.WriteLine($"Could not find connection string for {database.Name}. Skipping...");
                    continue;
                }

                // TODO: Fill repository, repository settings, and models.
                string databaseOutDirectory = Path.Combine("Out", database.Name);
                if (!Directory.Exists(databaseOutDirectory))
                    Directory.CreateDirectory(databaseOutDirectory);

                // Add the DB name to the default connection string template
                string connectionString = connectionStringTemplate + database.Name;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    Server server = new Server(new ServerConnection(connection));

                    foreach (Entity entity in database.Entities)
                    {
                        // Create the entity out directory if it doesn't exist already
                        string entityOutDirectory = Path.Combine(databaseOutDirectory, entity.Name);
                        if (!Directory.Exists(entityOutDirectory))
                            Directory.CreateDirectory(entityOutDirectory);

                        // Fill all .NET Framework templates
                        File.WriteAllText(Path.Combine(entityOutDirectory, $"I{entity.Name}.cs"),
                            TemplateHelper.FillInterfaceTemplate(entity)
                        );
                        File.WriteAllText(Path.Combine(entityOutDirectory, $"{entity.Name}.cs"),
                            TemplateHelper.FillBizTemplate(entity)
                        );
                        File.WriteAllText(Path.Combine(entityOutDirectory, $"{entity.Name}DAL.cs"),
                            TemplateHelper.FillDalTemplate(entity,
                                Config.UseAutoGeneratedConnectionStrings ?
                                connectionString :
                                ConnectionStrings[database.Name] // Connection string pulled from config DB
                            )
                        );

                        if (Config.DoProcedureWireup)
                        {
                            // Create SQL procedures in DB
                            server.ConnectionContext.ExecuteNonQuery(TemplateHelper.FillSqlTemplate(entity));
                        }
                    }
                }

                Console.WriteLine($"Wired {database.Name}");
                dbCounter++;
            }

            Console.WriteLine($"Done! Wired {dbCounter} databases.");
        }

        private Dictionary<string, string> GetConnectionStrings()
        {
            Dictionary<string, string> connectionStrings = new Dictionary<string, string>();
            string configConnectionString;
            if (!Config.ConnectionStrings.TryGetValue("RobloxConfig", out configConnectionString))
                throw new ApplicationException("Unable to find RobloxConfiguration connection string in ApplicationSettings");

            using (SqlConnection connection = new SqlConnection(configConnectionString))
            {
                Server server = new Server(new ServerConnection(connection));
                SqlDataReader dataReader = server.ConnectionContext.ExecuteReader("SELECT ID, Name, Value FROM dbo.ConnectionStrings");

                while (dataReader.Read())
                {
                    string name = dataReader["Name"].ToString();
                    string value = dataReader["Value"].ToString();

                    if (!(string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value)))
                        connectionStrings.Add(name, value);
                    else
                        Console.WriteLine(string.Format("Warning: ConnectionString with ID {0} has missing Name and or Value!", dataReader["ID"].ToString()));
                }
            }

            return connectionStrings;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using SetupCommon;

namespace SchemaConverter
{
    class Program
    {
        /// <summary>
        /// Converts all pre-defined entities into the new Entity format
        /// and outputs them in XML format to the provided directory.
        /// </summary>
        static void Main(string[] args)
        {
            Console.WriteLine("Converting schema...");

            List<Database> databases = SchemaHelper.ReadSchemaDirectory(SetupCommon.Properties.Settings.Default.SchemaDirectory);

            foreach (Database database in databases)
            {
                // Create directory for the DB if it doesn't exist already
                string path = SetupCommon.Properties.Settings.Default.OutputFolder + "\\" + database.Name;
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                foreach (Entity entity in database.Entities)
                {
                    SchemaHelper.WriteXmlEntity(entity, path + "\\" + entity.Name + ".xml");
                    //SchemaHelper.ReadXmlEntity(path + "\\" + entity.Name + ".xml");
                    //SchemaHelper.WriteJsonEntity(entity, path + "\\" + entity.Name + ".json");
                }
                Console.WriteLine("Finished " + database.Name);
            }

            Console.WriteLine("Done!");
            Console.ReadKey();
        }
    }
}

using Antlr4.StringTemplate;
using SetupCommon;
using System;
using System.Collections.Generic;
using System.IO;

namespace DBWireup
{
    internal class TemplateHelper
    {
        private static Dictionary<string, string> NetCoreTemplates = new Dictionary<string, string>();
        private static Dictionary<string, string> NetFrameworkTemplates = new Dictionary<string, string>();
        private static Dictionary<string, string> SqlTemplates = new Dictionary<string, string>();
        private static Dictionary<string, string> SharedTemplates = new Dictionary<string, string>();

        private static string PARAM_PREFIX = "@";

        /// <summary>
        /// Reads templates from their multiple directories.
        /// </summary>
        internal static void Setup()
        {
            if (!Directory.Exists("Templates"))
                throw new Exception("Template directory is missing");

            LoadTemplateDirectory(Path.Combine("Templates", "NetCore"), ref NetCoreTemplates);
            LoadTemplateDirectory(Path.Combine("Templates", "NetFramework"), ref NetFrameworkTemplates);
            LoadTemplateDirectory(Path.Combine("Templates", "Sql"), ref SqlTemplates);
            LoadTemplateDirectory(Path.Combine("Templates", "Shared"), ref SharedTemplates);
        }

        /// <summary>
        /// Reads all the files in a directory then adds the file name (minus extension) and the contents of the file
        /// into templateDirectory.
        /// </summary>
        /// <param name="directory">Directory to read</param>
        /// <param name="templateDictionary">Dictionary to add the files to</param>
        private static void LoadTemplateDirectory(string directory, ref Dictionary<string, string> templateDictionary)
        {
            if (!Directory.Exists(directory))
                throw new Exception($"Template directory {directory} is missing");

            foreach (string file in Directory.GetFiles(directory))
            {
                string templateName;
                {
                    string[] pathSplit = file.Split(Path.DirectorySeparatorChar);
                    templateName = pathSplit[pathSplit.Length - 1].Split('.')[0];
                }

                templateDictionary.Add(templateName, File.ReadAllText(file));
            }
        }

        /// <summary>
        /// Creates a new Antlr4 Template and fills out basic values.
        /// </summary>
        /// <param name="baseTemplate"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        private static Template GetNewTemplate(string baseTemplate, Entity entity = null)
        {
            Template template = new Template(baseTemplate, '~', '~');

            if (entity != null)
            {
                template.Add("NAMESPACE", entity.EntityNamespace);
                template.Add("ENTITYNAME", entity.Name);
                template.Add("DALCLASSNAME", $"{entity.Name}DAL");
                template.Add("ACCESSIBILITY", entity.IsInternal ? "internal" : "public");
                template.Add("IDTYPE", entity.IdType);
            }

            return template;
        }

        private static string GetGetProcedure(Entity entity)
        {
            return $"{entity.TableName}_Get{entity.Name}";
        }

        private static string GetGetByPropertyProcedure(Entity entity, Property property)
        {
            return $"{GetGetProcedure(entity)}IDsBy{property.Name}";
        }

        private static string GetInsertProcedure(Entity entity)
        {
            return $"{entity.TableName}_Insert{entity.Name}";
        }

        private static string GetUpdateProcedure(Entity entity)
        {
            return $"{entity.TableName}_Update{entity.Name}";
        }

        private static string GetDeleteProcedure(Entity entity)
        {
            return $"{entity.TableName}_Delete{entity.Name}";
        }

        internal static string FillInterfaceTemplate(Entity entity)
        {
            List<string> interfaceParameters = new List<string>();

            foreach (Property property in entity.Properties)
            {
                interfaceParameters.Add($"{property.Type} {property.Name} {{ get; set; }}");
            }

            Template template = GetNewTemplate(SharedTemplates["Interface"], entity);
            template.Add("INTERFACEPARAMETERS", string.Join("\n", interfaceParameters.ToArray()));

            return template.Render();
        }

        /// <summary>
        /// Fills out the NetFramework/Biz.txt template.
        /// </summary>
        /// <param name="entity">Entity to use while filling</param>
        /// <returns>Filled template</returns>
        internal static string FillBizTemplate(Entity entity)
        {
            string bizProperties = "";
            List<string> createNewParams = new List<string>();
            List<string> createNewPropertySetters = new List<string>();
            List<string> paramFunctions = new List<string>();
            List<string> stateTokenCollection = new List<string>();

            foreach (Property property in entity.Properties)
            {
                if (property.Name != "ID")
                {
                    // ID is already there in the template
                    bizProperties += $"public {property.Type} {property.Name}\n" +
                                     $"{{\n" +
                                     $"    get {{ return _EntityDAL.{property.Name};  }}\n" +
                                     $"    set {{ _EntityDAL.{property.Name} = value;  }}\n" +
                                     $"}}\n";

                    // ID doesn't have a setter
                    createNewParams.Add($"{property.Type} {property.Name.ToLower()}");
                    createNewPropertySetters.Add($"entity.{property.Name} = {property.Name.ToLower()};");

                    // This is only for foreign keys!!
                    if (property.IsForeignKey)
                    {
                        Template paramFunction = GetNewTemplate(NetFrameworkTemplates["BizGetByFK"], entity);
                        paramFunction.Add("TABLENAME", entity.TableName);
                        paramFunction.Add("BIZCLASSNAME", entity.Name);
                        paramFunction.Add("FKPROPERTYNAME", char.ToUpper(property.Name[0]) + property.Name.Substring(1));
                        paramFunction.Add("FKIDTYPE", property.Type);
                        stateTokenCollection.Add("\tyield return new StateToken(string.Format(\"" + property.Name + ":{0}\", " + property.Name + "));");

                        paramFunctions.Add(paramFunction.Render());
                    }
                }
            }

            Template template = GetNewTemplate(NetFrameworkTemplates["Biz"], entity);
            template.Add("CLASSNAME", $"{entity.Name}DAL");
            template.Add("BIZCLASSNAME", entity.Name);
            template.Add("BIZREMOTECACHEABLE", ""); // Maybe should do something with this
            template.Add("BIZREMOTECACHEABLEREGION", ""); // This too
            template.Add("BIZPROPERTIES", bizProperties);
            template.Add("CREATENEWPARAMS", string.Join(", ", createNewParams.ToArray()));
            template.Add("CREATENEWPROPERTYSETTERS", string.Join("\n", createNewPropertySetters.ToArray()));
            template.Add("PARAMFUNCTIONS", string.Join("\n", paramFunctions.ToArray()));
            template.Add("STATETOKENCOLLECTION", string.Join("\n", stateTokenCollection.ToArray()));

            return template.Render();
        }

        /// <summary>
        /// Fill template I'll finish this later too lazy - memes 2021
        /// </summary>
        /// <param name="entity"></param>
        internal static string FillDalTemplate(Entity entity, string connectionStringValue)
        {
            List<string> dalFields = new List<string>();
            List<string> dalProperties = new List<string>();
            List<string> queryParameters = new List<string>();
            List<string> readerParameters = new List<string>();
            List<string> paramFunctions = new List<string>();
            string connectionStringPropName = $"dbConnectionString_{entity.Name}DAL"; // Config.ConnectionStringPropertyName

            foreach (Property property in entity.Properties)
            {
                if (property.Name != "ID")
                {
                    string typeDefault = $"default({property.Type})";
                    if (property.Type.ToLower() == "string")
                        typeDefault = "string.Empty";
                    else if (property.Type == "DateTime")
                        typeDefault = "DateTime.MinValue";
                    dalFields.Add($"private {property.Type} _{property.Name} = {typeDefault};");
                    dalProperties.Add($"{(entity.IsInternal ? "internal" : "public")} {property.Type} {property.Name}\n" +
                                      $"{{\n" +
                                      $"    get {{ return _{property.Name}; }}\n" +
                                      $"    set {{ _{property.Name} = value; }}\n" +
                                      $"}}"
                    );

                    queryParameters.Add($"new SqlParameter(\"{PARAM_PREFIX}{property.Name}\", _{property.Name})");
                    readerParameters.Add($"dal.{property.Name} = ({property.Type})reader[\"{property.Name}\"];");

                    if (property.IsForeignKey)
                    {
                        Template paramFunction = GetNewTemplate(NetFrameworkTemplates["DalGetByFK"], entity);
                        paramFunction.Add("CLASSNAME", entity.Name);
                        paramFunction.Add("FKPROPERTYNAME", property.Name);
                        paramFunction.Add("FKIDTYPE", property.Type);
                        paramFunction.Add("GETBYPROPERTYPROCEDURE", GetGetByPropertyProcedure(entity, property));
                        paramFunction.Add("CONNECTIONSTRING", connectionStringPropName);

                        paramFunctions.Add(paramFunction.Render());
                    }
                }
            }

            Template template = GetNewTemplate(NetFrameworkTemplates["Dal"], entity);
            // A hack for the Roblox Server Class Library
            if (entity.EntityNamespace == "Roblox")
            {
                template.Remove("NAMESPACE");
                template.Add("NAMESPACE", "Roblox.DataAccess");
            }
            template.Add("CONNECTIONSTRING", connectionStringPropName);
            template.Add("CONNECTIONSTRINGVALUE", connectionStringValue);
            template.Add("DELETEPROCEDURE", GetDeleteProcedure(entity) + "ByID");
            template.Add("INSERTPROCEDURE", GetInsertProcedure(entity));
            template.Add("UPDATEPROCEDURE", GetUpdateProcedure(entity) + "ByID");
            template.Add("GETPROCEDURE", GetGetProcedure(entity) + "ByID");
            template.Add("IDSQLDBTYPE", entity.SqlIdType);
            template.Add("DALFIELDS", string.Join("\n", dalFields.ToArray()));
            template.Add("DALPROPERTIES", string.Join("\n", dalProperties.ToArray()));
            template.Add("QUERYPARAMETERS", string.Join(",\n", queryParameters.ToArray()));
            template.Add("READERPARAMETERS", string.Join("\n", readerParameters.ToArray()));
            template.Add("PARAMFUNCTIONS", string.Join("\n", paramFunctions.ToArray()));

            return template.Render();
        }

        internal static string FillSqlTemplate(Entity entity)
        {
            List<string> sqlInputParameterList = new List<string>();
            List<string> columnList = new List<string>();
            List<string> setValues = new List<string>();
            List<string> paramFunctions = new List<string>();
            List<string> paramList = new List<string>();

            foreach (Property property in entity.Properties)
            {
                if (property.Name != "ID")
                {
                    string extra = "";
                    if (property.IsNullable)
                        extra += " = NULL";
                    // Hack for binary sql types
                    if (property.Type == "string" && !(property.SqlType.Contains("binary") || property.SqlType.Contains("BINARY")))
                        extra += " = \"\"";
                    sqlInputParameterList.Add($"{PARAM_PREFIX}{property.Name}				{property.SqlType}{extra}");
                    columnList.Add("[" + property.Name + "]");
                    setValues.Add($"[{property.Name}] = {PARAM_PREFIX}{property.Name}");
                    paramList.Add(PARAM_PREFIX + property.Name);

                    if (property.IsForeignKey)
                    {
                        Template paramFunction = GetNewTemplate(SqlTemplates["EntityProceduresFKLookup"], entity);
                        paramFunction.Add("TABLENAME", entity.TableName);
                        paramFunction.Add("FKPROPERTYNAME", property.Name);
                        paramFunction.Add("FKIDSQLTYPE", property.SqlType);
                        paramFunction.Add("GETBYPROPERTYPROCEDURE", GetGetByPropertyProcedure(entity, property));

                        paramFunctions.Add(paramFunction.Render());
                    }
                }
            }

            Template template = GetNewTemplate(SqlTemplates["EntityProcedures"], entity);
            template.Add("TABLENAME", entity.TableName);
            template.Add("SQLIDTYPE", entity.SqlIdType);
            template.Add("DELETEPROCEDURE", GetDeleteProcedure(entity) + "ByID");
            template.Add("INSERTPROCEDURE", GetInsertProcedure(entity));
            template.Add("UPDATEPROCEDURE", GetUpdateProcedure(entity) + "ByID");
            template.Add("GETPROCEDURE", GetGetProcedure(entity) + "ByID");
            template.Add("SQLINPUTPARAMETERLIST", ", " + string.Join(",\n", sqlInputParameterList.ToArray()));
            template.Add("COLUMNLIST", string.Join(",\n", columnList.ToArray()));
            template.Add("SQLPARAMETERLIST", string.Join(",\n", paramList.ToArray())); // Not same output as columnlist
            template.Add("SETVALUES", string.Join(",\n", setValues.ToArray()));
            template.Add("PARAMFUNCTIONS", string.Join("\n", paramFunctions.ToArray()));

            return template.Render();
        }

        internal static string FillModelTemplate(Entity entity, string repositoryNamespace)
        {
            List<string> modelParameters = new List<string>();

            foreach (Property property in entity.Properties)
            {
                modelParameters.Add($"{(entity.IsInternal ? "internal" : "public")} {property.Type} {property.Name} {{ get; set; }}");
            }

            Template template = GetNewTemplate(NetCoreTemplates["Model"], entity);
            template.Add("REPOSITORYNAMESPACE", repositoryNamespace);
            template.Add("MODELPARAMETERS", string.Join("\n", modelParameters.ToArray()));

            return template.Render();
        }

        internal static string FillRepositoryTemplate(Database database)
        {
            List<string> entityFunctions = new List<string>();
            List<string> entityCaches = new List<string>();

            /*foreach (Entity entity in database.Entities)
            {

            }*/

            Template template = GetNewTemplate(NetCoreTemplates["Repository"]);
            template.Add("REPOSITORYNAME", database.RepositoryName);
            template.Add("REPOSITORYNAMESPACE", database.RepositoryNamespace != null ? database.RepositoryNamespace : "Roblox");
            template.Add("ENTITYFUNCTIONS", string.Join("\n", entityFunctions.ToArray()));
            template.Add("ENTITYCACHES", string.Join("\n", entityCaches.ToArray()));

            return template.Render();
        }

        internal static string FillRepositorySettingsTemplate(Database database)
        {
            List<string> entitySettings = new List<string>();

            /*foreach (Entity entity in database.Entities)
            {
                if (entity.CacheType == CacheType.Timed)
                {
                    Template entitySettingsTemplate = GetNewTemplate(NetCoreTemplates["RepositorySettingsEntityParams"], entity);
                    entitySettingsTemplate.Add("TABLENAME", entity.TableName);
                    entitySettings.Add(entitySettingsTemplate.Render());
                }
            }*/

            Template template = GetNewTemplate(NetCoreTemplates["RepositorySettings"]);
            template.Add("REPOSITORYNAME", database.RepositoryName);
            template.Add("REPOSITORYNAMESPACE", database.RepositoryNamespace);
            template.Add("ENTITYSETTINGS", string.Join("\n\n", entitySettings.ToArray()));

            return template.Render();
        }
    }
}

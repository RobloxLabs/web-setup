using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Antlr4.StringTemplate;
using SetupCommon;

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
                template.Add("IDTYPE", entity.GetIDProperty().Type);
                template.Add("SQLIDTYPE", entity.GetIDProperty().SqlType); // Specific to Insert
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

        private static string GetDefaultForType(string type)
        {
            string typeDefault = $"default({type})";
            if (type.ToLower() == "string")
                typeDefault = "string.Empty";
            else if (type == "DateTime")
                typeDefault = "DateTime.MinValue";
            return typeDefault;
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
                // Property definition
                bizProperties += $"public {property.Type} {property.Name}\n{{\n";
                // Append getter
                bizProperties += $"\tget {{ return _EntityDAL.{property.Name};  }}\n";
                if (!property.IsReadonly)
                {
                    // Append setter
                    bizProperties += $"\tset {{ _EntityDAL.{property.Name} = value;  }}\n";
                    // ID doesn't have a setter
                    createNewParams.Add($"{property.Type} {property.Name.ToLower()}");
                    createNewPropertySetters.Add($"entity.{property.Name} = {property.Name.ToLower()};");
                }
                bizProperties += "}\n";

                // This is only for foreign keys!!
                /*if (property.IsForeignKey)
                {
                    Template paramFunction = GetNewTemplate(NetFrameworkTemplates["BizGetByFK"], entity);
                    paramFunction.Add("TABLENAME", entity.TableName);
                    paramFunction.Add("BIZCLASSNAME", entity.Name);
                    paramFunction.Add("FKPROPERTYNAME", char.ToUpper(property.Name[0]) + property.Name.Substring(1));
                    paramFunction.Add("FKIDTYPE", property.Type);
                    stateTokenCollection.Add("\tyield return new StateToken(string.Format(\"" + property.Name + ":{0}\", " + property.Name + "));");

                    paramFunctions.Add(paramFunction.Render());
                }*/
            }

            Template template = GetNewTemplate(NetFrameworkTemplates["BIZ"], entity);
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
            IList<string> dalFields = new List<string>();
            IList<string> dalProperties = new List<string>();
            IList<string> readerParameters = new List<string>();
            IList<string> paramFunctions = new List<string>();
            string connectionStringPropName = $"dbConnectionString_{entity.Name}DAL"; // Config.ConnectionStringPropertyName
            const string LINE_PREFIX_1 = ",\r\n";
            const string LINE_PREFIX_2 = "\r\n";

            // Prep DAL template //
            foreach (Property property in entity.Properties)
            {
                string typeDefault = GetDefaultForType(property.Type);

                dalFields.Add($"private {property.Type} _{property.Name} = {typeDefault};");
                dalProperties.Add($"{(entity.IsInternal ? "internal" : "public")} {property.Type} {property.Name}\r\n" +
                                    $"{{\r\n" +
                                    $"    get {{ return _{property.Name}; }}\r\n" +
                                    $"    set {{ _{property.Name} = value; }}\r\n" +
                                    $"}}"
                );

                readerParameters.Add($"dal.{property.Name} = ({property.Type})reader[\"{property.Name}\"];");
            }

            // DAL Procedure templates //
            foreach (Procedure procedure in entity.Procedures)
            {
                IList<string> parameters = new List<string>();
                IList<string> parameterValidation = new List<string>();
                IList<string> queryParameters = new List<string>();

                foreach (Parameter param in procedure.Parameters)
                {
                    var type = param.Type;
                    if (param.IsPropertyBound)
                        type = entity.GetProperty(param.Name).Type;

                    // Camel case parameter for method
                    string argName;
                    // HACK: ID is showing up as iD instead of id
                    if (param.Name == "ID")
                        argName = "id";
                    else
                        argName = char.ToLowerInvariant(param.Name[0]) + param.Name.Substring(1);
                    parameters.Add($"{type} {argName}");

                    string val;
                    // Determine if we're referencing the property directly,
                    // or if we're using an argument from the current method instead
                    if ((int)procedure.Type >= (int)ProcedureType.Get)
                        val = argName;
                    else
                        val = $"_{ param.Name}";

                    parameterValidation.Add($"if ({val} == {GetDefaultForType(type)}){LINE_PREFIX_2 + "\t"}throw new ApplicationException(\"Required value not specified: {param.Name}.\");");
                    queryParameters.Add($"new SqlParameter(\"{PARAM_PREFIX}{param.Name}\", {val})");
                }

                Template procedureT = GetNewTemplate(NetFrameworkTemplates["DAL_ProcedureType_" + procedure.Type.ToString()], entity);
                procedureT.Add("PARAMETERS", string.Join(", ", parameters));
                procedureT.Add("PARAMETERVALIDATION", string.Join(LINE_PREFIX_2, parameterValidation));
                procedureT.Add("QUERYPARAMETERS", string.Join(LINE_PREFIX_1, queryParameters));
                procedureT.Add("CONNECTIONSTRING", connectionStringPropName);
                procedureT.Add("PROCEDURE", procedure.GetNameSql(entity));
                procedureT.Add("PROCEDUREMETHOD", procedure.GetNameDal(entity));
                procedureT.Add("NOPARAMS", parameters.Count == 0);

                paramFunctions.Add(procedureT.Render());
            }

            // DAL Template //
            Template template = GetNewTemplate(NetFrameworkTemplates["DAL"], entity);
            // A hack for the Roblox Server Class Library
            if (entity.EntityNamespace == "Roblox")
            {
                template.Remove("NAMESPACE");
                template.Add("NAMESPACE", "Roblox.DataAccess");
            }
            template.Add("CONNECTIONSTRING", connectionStringPropName);
            template.Add("CONNECTIONSTRINGVALUE", connectionStringValue);
            template.Add("IDSQLDBTYPE", entity.GetIDProperty().SqlType);
            template.Add("DALFIELDS", string.Join(LINE_PREFIX_2, dalFields));
            template.Add("DALPROPERTIES", string.Join(LINE_PREFIX_2, dalProperties));
            template.Add("READERPARAMETERS", string.Join(LINE_PREFIX_2, readerParameters));
            template.Add("PARAMFUNCTIONS", string.Join(LINE_PREFIX_2, paramFunctions));

            return template.Render();
        }

        /// <summary>
        /// Fills the SQL table script template with data based on
        /// the given entity.
        /// </summary>
        /// <param name="entity">The entity to create the SQL table for</param>
        /// <returns></returns>
        internal static string FillSqlTemplate(Entity entity)
        {
            IList<string> sqlInputParameterList; // Procedure params
            IList<string> paramList; // Section where values in the table are matched up to the parameters

            IList<string> columnList = new List<string>(); // Specific to Insert and Get
            IList<string> setValues = new List<string>(); // Specific to Update
            IList<string> insertParamList = new List<string>(); // Specific to Insert

            StringBuilder result = new StringBuilder();

            const string LINE_PREFIX = ",\r\n";

            foreach (Property property in entity.Properties)
            {
                columnList.Add($"[{property.Name}]");
                if (!property.IsPrimaryKey)
                {
                    setValues.Add($"[{property.Name}] = {PARAM_PREFIX}{property.Name}");
                }
                insertParamList.Add($"{PARAM_PREFIX}{property.Name}");
            }

            foreach (var procedure in entity.Procedures)
            {
                sqlInputParameterList = new List<string>();
                paramList = new List<string>();

                foreach (var param in procedure.Parameters)
                {
                    string type = param.SqlType;
                    // Default value for procedure parameter
                    string extra = "";
                    if (param.IsPropertyBound)
                    {
                        // Find matching property
                        Property property = entity.GetProperty(param.Name);

                        if (property != null)
                        {
                            type = property.SqlType;
                                
                            if (!string.IsNullOrEmpty(property.DefaultValue))
                            {
                                // Hack for binary sql types
                                if (property.Type == "string" && !(property.SqlType.Contains("binary") || property.SqlType.Contains("BINARY")))
                                    extra += $" = \"{property.DefaultValue}\"";
                                else
                                    extra += $" = {property.DefaultValue}";
                            }
                            else if (property.IsNullable)
                                extra += " = NULL";

                            paramList.Add($"[{property.Name}] = {PARAM_PREFIX + param.Name}");
                        }
                    }
                    else
                    {
                        //throw new NotImplementedException("Non-property-bound parameter support has not yet been implemented!");
                    }
                    sqlInputParameterList.Add($"{PARAM_PREFIX}{param.Name}\t\t\t\t{type}{extra}");
                }

                Template template = GetNewTemplate(SqlTemplates["ProcedureType_" + procedure.Type.ToString()], entity);
                template.Add("TABLENAME", entity.TableName);
                template.Add("PROCEDURE", procedure.GetNameSql(entity));
                template.Add("SQLINPUTPARAMETERLIST", string.Join(LINE_PREFIX, sqlInputParameterList));
                var empty = true;
                if (procedure.Type == ProcedureType.Insert)
                {
                    template.Add("SQLPARAMETERLIST", string.Join(LINE_PREFIX, insertParamList));
                    empty = insertParamList.Count == 0;
                }
                else
                {
                    template.Add("SQLPARAMETERLIST", string.Join(LINE_PREFIX, paramList)); // Section where values in the table are set
                    empty = paramList.Count == 0;
                }
                template.Add("NOPARAMS", empty);

                template.Add("COLUMNLIST", string.Join(LINE_PREFIX, columnList)); // Specific to Insert and Get
                template.Add("SETVALUES", string.Join(LINE_PREFIX, setValues)); // Specific to Update

                result.AppendLine(template.Render());
            }

            return result.ToString();
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

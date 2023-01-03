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
                template.Add("BIZCLASSNAME", entity.Name);
                template.Add("ACCESSIBILITY", entity.IsInternal ? "internal" : "public");
                template.Add("IDTYPE", entity.GetIDProperty().Type);
                template.Add("SQLIDTYPE", entity.GetIDProperty().SqlType); // Specific to Insert
                template.Add("CURRENTDATE", DateTime.Now.ToShortDateString());
                template.Add("CURRENTTIME", DateTime.Now.ToLongTimeString());
                string type = entity.GetIDProperty().Type.ToLower();
                template.Add("COUNTTYPE", type == "byte" ? "int" : type);
            }

            return template;
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
            List<string> bizProperties = new List<string>();
            List<string> createNewParams = new List<string>();
            List<string> createNewPropertySetters = new List<string>();
            List<string> paramFunctions = new List<string>();
            List<string> stateTokenCollection = new List<string>();
            const string LINE_PREFIX = "\r\n";

            // Prep BIZ Template //
            foreach (Property property in entity.Properties)
            {
                string bizProp = $"public {property.Type} {property.Name}" + LINE_PREFIX +
                    $"{{\r\n" +
                    $"    get {{ return _EntityDAL.{property.Name}; }}" + LINE_PREFIX;
                if (!property.IsReadOnly)
                {
                    // Append setter
                    bizProp += $"\tset {{ _EntityDAL.{property.Name} = value; }}" + LINE_PREFIX;
                    string argName = CamelCaseString(property.Name);
                    createNewParams.Add($"{property.Type} {argName}");
                    createNewPropertySetters.Add($"entity.{property.Name} = {argName};");
                }
                bizProp += "}" + LINE_PREFIX;
                bizProperties.Add(bizProp);

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

            // BIZ Procedure templates //
            // TODO: Separate out into method (MODULATE!!!)
            foreach (Procedure procedure in entity.Procedures)
            {
                if ((int)procedure.Type > (int)ProcedureType.Update)
                {
                    IList<string> parameters = new List<string>();
                    IList<string> arguments = new List<string>();
                    IList<string> cacheLookup = new List<string>();
                    IList<string> collectionId = new List<string> { procedure.GetNameBiz(entity) };

                    foreach (Parameter param in procedure.Parameters)
                    {
                        var type = param.Type;
                        if (param.IsPropertyBound)
                            type = entity.GetProperty(param.Name).Type;

                        string argName = CamelCaseString(param.Name);
                        parameters.Add($"{type} {argName}");
                        if (!(procedure.Type == ProcedureType.GetPaged && !param.IsPropertyBound))
                        {
                            arguments.Add(argName);
                        }
                        if (param.IsPropertyBound)
                            cacheLookup.Add($"{param.Name}:{{{argName}}}");
                        collectionId.Add($"{param.Name}:{{{argName}}}");
                    }

                    Template procedureT = GetNewTemplate(NetFrameworkTemplates["BIZ_ProcedureType_" + procedure.Type.ToString()], entity);
                    procedureT.Add("PARAMETERS", string.Join(", ", parameters));
                    procedureT.Add("DALPARAMS", string.Join(", ", arguments));
                    procedureT.Add("PROCEDURE", procedure.GetNameSql(entity));
                    procedureT.Add("PROCEDUREMETHOD", procedure.GetNameBiz(entity));
                    procedureT.Add("DALPROCEDUREMETHOD", procedure.GetNameDal(entity));
                    procedureT.Add("NOPARAMS", arguments.Count == 0);
                    procedureT.Add("UNQUALIFIED", cacheLookup.Count == 0);

                    if (procedure.Type == ProcedureType.Get)
                        procedureT.Add("IDONLY", procedure.Parameters.Count == 1 && procedure.Parameters[0].Name == "ID");

                    procedureT.Add("CACHELOOKUP", string.Join("_", cacheLookup));
                    procedureT.Add("COLLECTIONID", string.Join("_", collectionId));

                    paramFunctions.Add(procedureT.Render());
                }
            }

            // BIZ Template //
            Template template = GetNewTemplate(NetFrameworkTemplates["BIZ"], entity);
            //template.Add("BIZREMOTECACHEABLE", $", ICacheableObject<{entity.GetIDProperty().Type}>"); // Maybe should do something with this
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
            IList<string> insertUpdateQueryParameters = new List<string>();
            string connectionStringPropName = $"dbConnectionString_{entity.Name}DAL"; // Config.ConnectionStringPropertyName
            const string LINE_PREFIX_1 = ",\r\n";
            const string LINE_PREFIX_2 = "\r\n";

            // Prep DAL template //
            foreach (Property property in entity.Properties)
            {
                string typeDefault = GetDefaultForType(property.Type);

                dalFields.Add($"private {property.Type} _{property.Name} = {typeDefault};");
                dalProperties.Add($"{(entity.IsInternal ? "internal" : "public")} {property.Type} {property.Name}" + LINE_PREFIX_2 +
                                    $"{{\r\n" +
                                    $"    get {{ return _{property.Name}; }}" + LINE_PREFIX_2 +
                                    $"    set {{ _{property.Name} = value; }}" + LINE_PREFIX_2 +
                                    $"}}"
                );

                if (property.IsNullable)
                    readerParameters.Add($"dal.{property.Name} = (reader[\"{property.Name}\"] != DBNull.Value ? ({property.Type})reader[\"{property.Name}\"] : null);");
                else
                    readerParameters.Add($"dal.{property.Name} = ({property.Type})reader[\"{property.Name}\"];");
                if (!property.IsPrimaryKey)
                    insertUpdateQueryParameters.Add($"new SqlParameter(\"{PARAM_PREFIX}{property.Name}\", _{property.Name})");
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

                    string argName = CamelCaseString(param.Name);
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
                if (procedure.Type == ProcedureType.Insert || procedure.Type == ProcedureType.Update)
                    procedureT.Add("QUERYPARAMETERS", string.Join(LINE_PREFIX_1, insertUpdateQueryParameters));
                else
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
            IList<string> execParams; // Specific to GetOrCreate procedure
            IList<string> procColumnList; // Specific to GetOrCreate procedure
            IList<string> procValues; // Specific to GetOrCreate procedure

            IList<string> columnList = new List<string>(); // Specific to Get
            IList<string> insertColumnList = new List<string>(); // Specific to Insert
            IList<string> setValues = new List<string>(); // Specific to Update
            IList<string> insertSqlInputParameterList = new List<string>(); // Specific to Insert
            IList<string> insertParamList = new List<string>(); // Specific to Insert

            StringBuilder result = new StringBuilder();

            const string LINE_PREFIX_1 = ",\r\n";
            const string LINE_PREFIX_2 = " AND \r\n";

            foreach (Property property in entity.Properties)
            {
                columnList.Add($"[{property.Name}]");
                if (!property.IsPrimaryKey)
                {
                    insertColumnList.Add($"[{property.Name}]");
                    setValues.Add($"[{property.Name}] = {PARAM_PREFIX}{property.Name}");
                    insertSqlInputParameterList.Add($"{PARAM_PREFIX}{property.Name}\t\t\t\t{property.SqlType}{GetSqlInputParameterSuffix(property, true)}");
                    insertParamList.Add($"{PARAM_PREFIX}{property.Name}");
                }
            }

            foreach (var procedure in entity.Procedures)
            {
                sqlInputParameterList = new List<string>();
                paramList = new List<string>();
                execParams = new List<string>();
                procColumnList = new List<string>();
                procValues = new List<string>();

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
                            extra = GetSqlInputParameterSuffix(property);

                            procColumnList.Add($"[{property.Name}]");
                            procValues.Add(PARAM_PREFIX + property.Name);
                            paramList.Add($"[{property.Name}] = {PARAM_PREFIX + param.Name}");
                            execParams.Add($"{PARAM_PREFIX + property.Name} = {PARAM_PREFIX + param.Name}");
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

                if (procedure.Type == ProcedureType.Insert || procedure.Type == ProcedureType.Update)
                    template.Add("SQLINPUTPARAMETERLIST", string.Join(LINE_PREFIX_1, insertSqlInputParameterList));

                var empty = true;
                if (procedure.Type == ProcedureType.Insert)
                {
                    template.Add("SQLPARAMETERLIST", string.Join(LINE_PREFIX_1, insertParamList));
                    empty = insertParamList.Count == 0;
                    template.Add("COLUMNLIST", string.Join(LINE_PREFIX_1, insertColumnList)); // Specific to Insert and Get
                }
                else
                {
                    // HACK: I fucking hate this
                    if (procedure.Type != ProcedureType.Update)
                        template.Add("SQLINPUTPARAMETERLIST", string.Join(LINE_PREFIX_1, sqlInputParameterList));
                    template.Add("SQLPARAMETERLIST", string.Join(LINE_PREFIX_2, paramList)); // Section where values in the table are set
                    empty = paramList.Count == 0;

                    template.Add("COLUMNLIST", string.Join(LINE_PREFIX_1, columnList)); // Specific to Insert and Get
                }
                template.Add("NOPARAMS", empty);
                template.Add("SETVALUES", string.Join(LINE_PREFIX_1, setValues)); // Specific to Update

                // HORRIBLY HACKY and ALL GETORCREATE SPECIFIC.
                // FUCK
                if (procedure.Type == ProcedureType.GetOrCreate)
                {
                    // TODO: Hacky
                    template.Add("EXECPARAMS", string.Join(LINE_PREFIX_1, execParams)); // Specific to GetOrCreate
                    // Insert
                    var insertProc = new Procedure
                    {
                        Type = ProcedureType.Insert
                    };
                    template.Add("INSERTPROCEDURE", insertProc.GetNameSql(entity));

                    // Get
                    var getProc = new Procedure
                    {
                        Type = ProcedureType.Get,
                        Parameters = procedure.Parameters
                    };
                    template.Add("GETPROCEDURE", getProc.GetNameSql(entity));

                    template.Add("SQLINSERTPARAMETERLIST", string.Join(LINE_PREFIX_1, procValues)); // Specific to GetOrCreate
                    template.Add("INSERTCOLUMNLIST", string.Join(LINE_PREFIX_1, procColumnList)); // Specific to GetOrCreate
                }

                template.Add("COUNTBIG", entity.GetIDProperty().SqlType == "BIGINT");

                result.AppendLine(template.Render());
            }

            return result.ToString();
        }

        /// <summary>
        /// Gets the suffix for an SQL input parameter defined in a set of procedure parameters.
        /// </summary>
        /// <param name="property">The property to base the parameter off of</param>
        /// <param name="isInsert">Really really fucking stupid-ass hack</param>
        /// <returns></returns>
        internal static string GetSqlInputParameterSuffix(Property property, bool isInsert = false)
        {
            // HACK: https://stackoverflow.com/questions/470664/sql-function-as-default-parameter-value
            if (isInsert && property.Type.ToLower() == "datetime"/* && property.IsNullable*/)
                return " = NULL";

            string extra = "";

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

            return extra;
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

        /// <summary>
        /// Camel case parameter for method
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        internal static string CamelCaseString(string str)
        {
            // HACK: ID is showing up as iD instead of id
            if (str == "ID")
                return "id";
            else
                return char.ToLowerInvariant(str[0]) + str.Substring(1);
        }
    }
}

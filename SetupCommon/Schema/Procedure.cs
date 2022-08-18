using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace SetupCommon
{
    /// <summary>
    /// The SQL procedure for a table of entities
    /// </summary>
    [Serializable]
    public class Procedure
    {
        private ProcedureType _Type;

        [XmlAttribute]
        public ProcedureType Type
        {
            get
            {
                return _Type;
            }
            set
            {
                if (_Type != value)
                {
                    if (value == ProcedureType.GetPaged)
                    {
                        // Parameters for a Get_Paged procedure
                        Parameters.Add(
                            new Parameter()
                            {
                                Name = "StartRowIndex",
                                Type = "int",
                                SqlType = "INT",
                                IsPropertyBound = false
                            }
                        );
                        Parameters.Add
                        (
                            new Parameter()
                            {
                                Name = "MaximumRows",
                                Type = "int",
                                SqlType = "INT",
                                IsPropertyBound = false
                            }
                        );
                    }
                    _Type = value;
                }
            }
        }
        
        /// <summary>
        /// The parameters for the procedure. Needs to be a List to allow for
        /// proper appending of parameters.
        /// </summary>
        [XmlArray]
        public List<Parameter> Parameters { get; set; }

        /// <summary>
        /// Returns whether or not the current procedure is a required CRUD procedure
        /// </summary>
        /// <returns>Whether the current procedure is a required CRUD procedure</returns>
        public bool IsPrimaryProcedure()
        {
            // CRUD (Create Read Update Delete)
            return Type == ProcedureType.Insert ||
                   Type == ProcedureType.Get ||
                   Type == ProcedureType.Update ||
                   Type == ProcedureType.Delete;
        }

        /// <summary>
        /// Gets the fully qualified name for the SQL procedure
        /// </summary>
        /// <param name="entity">The entity who owns the procedure</param>
        /// <param name="hasPrefix">Whether or not the procedure name has the table name as a prefix</param>
        /// <returns></returns>
        public string GetNameSql(Entity entity, bool hasPrefix = true)
        {
            string result;
            switch (Type)
            {
                case ProcedureType.GetPaged:
                    // Get{AccountRoleSet}IDs{ByAccountID}_Paged
                    result = $"Get{entity.Name}IDs{GetSuffix()}_Paged";
                    break;
                case ProcedureType.GetCount:
                    // GetTotalNumberOf{AssetHashScripts}{ByAssetHashID}
                    result = $"GetTotalNumberOf{entity.TableName}{GetSuffix()}";
                    break;
                case ProcedureType.MultiGet:
                    // Get{RecentItemLists}{ByID}s
                    result = $"Get{entity.TableName}{GetSuffix()}s"; // Only meant for one parameter
                    break;
                default:
                    // Get{AccountRoleSet}{ByID}
                    result = Type.ToString() + $"{entity.Name}{GetSuffix()}";
                    break;
            }

            if (hasPrefix)
                return entity.TableName + "_" + result;
            else
                return result;
        }

        /// <summary>
        /// Gets the DAL method name for the procedure
        /// </summary>
        /// <param name="entity">The entity who owns the procedure</param>
        /// <returns></returns>
        public string GetNameDal(Entity entity)
        {
            if (IsPrimaryProcedure())
            {
                // Procedure qualifies for method name size reduction
                if (Type == ProcedureType.Insert ||
                    (Parameters.Count == 1 &&
                    Parameters[0].Name == "ID"))
                {
                    /*
                     * Rather than doing something like GetByID, UpdateByID, or InsertByIDAndNameAndCreatedAndUpdated,
                     * just do Get, Update, and Insert
                     */
                    return Type.ToString();
                }
            }

            return GetNameSql(entity, false).Replace("_", "");
        }

        /// <summary>
        /// Gets the BIZ method name for the procedure
        /// </summary>
        /// <param name="entity">The entity who owns the procedure</param>
        /// <returns></returns>
        public string GetNameBiz(Entity entity)
        {
            switch (Type)
            {
                case ProcedureType.GetPaged:
                    // Get{AccountRoleSet}s{ByAccountID}Paged
                    return $"Get{entity.Name}s{GetSuffix()}Paged";
                default:
                    return GetNameDal(entity);
            }
        }

        protected string GetSuffix()
        {
            var result = "";
            IList<string> paramNames = new List<string>();

            // Just InsertSale, no InsertSaleByAllColumns
            if ((Parameters != null && Parameters.Count != 0) ||
                Type != ProcedureType.Insert)
            {
                foreach (var param in Parameters)
                {
                    if (param.IsPropertyBound)
                        paramNames.Add(param.Name);
                }

                if (paramNames.Count > 0)
                {
                    result += "By";
                    result += string.Join("And", paramNames);
                }
            }

            return result;
        }
    }
}

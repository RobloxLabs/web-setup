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
        /// <returns></returns>
        public string GetNameSql(Entity entity)
        {
            switch (Type)
            {
                case ProcedureType.GetPaged:
                    // Get{AccountRoleSet}IDs{ByAccountID}_Paged
                    return $"Get{entity.Name}IDs{GetSuffix()}_Paged";
                case ProcedureType.GetCount:
                    // GetTotalNumberOf{AssetHashScripts}{ByAssetHashID}
                    return $"GetTotalNumberOf{entity.TableName}{GetSuffix()}";
                case ProcedureType.MultiGet:
                    // Get{RecentItemLists}{ByID}s
                    return $"Get{entity.TableName}{GetSuffix()}s"; // Only meant for one parameter
                default:
                    // Get{AccountRoleSet}{ByID}
                    return Type.ToString() + $"{entity.Name}{GetSuffix()}";
            }
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

            return GetNameSql(entity).Replace("_", "");
        }

        protected string GetSuffix()
        {
            var result = "";
            IList<string> paramNames = new List<string>();

            // Just InsertSale, no InsertSaleByAllColumns
            if (Parameters.Count != 0 ||
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

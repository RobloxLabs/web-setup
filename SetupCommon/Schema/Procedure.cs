using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;

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
        
        [XmlArray]
        public List<Parameter> Parameters { get; set; }

        /// <summary>
        /// Gets the fully qualified name for the procedure
        /// </summary>
        /// <param name="entityName">The name of the entity</param>
        /// <param name="entityNamePlural">The plural name of the entity</param>
        /// <returns></returns>
        public string GetName(string entityName, string entityNamePlural = null)
        {
            var result = "";
            if (IsPluralType(Type))
                result += string.Format(GetPrefix(), entityNamePlural);
            else
                result += string.Format(GetPrefix(), entityName, GetSuffix());
            return result;
        }

        protected string GetPrefix()
        {
            switch (Type)
            {
                case ProcedureType.GetPaged:
                    // Get{AccountRoleSet}IDs{ByAccountID}_Paged
                    return "Get{0}IDs{1}_Paged";
                case ProcedureType.GetTotal:
                    // GetTotalNumberOf{AccountRoleSets}
                    return "GetTotalNumberOf{0}";
                case ProcedureType.MultiGet:
                    // Get{RecentItemLists}By{ID}s
                    return "Get{0}By{1}s"; // Only meant for one parameter
                default:
                    // Get{AccountRoleSet}{ByID}
                    return Type.ToString() + "{0}{1}";
            }
        }

        protected string GetSuffix()
        {
            var result = "";

            if (Parameters.Count != 0)
            {
                result += "By";
                foreach (var item in Parameters.Select((value, i) => new { i, value }))
                {
                    if (item.i > 0)
                        result += "And";
                    result += item.value.Name;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets whether or not the ProcedureType needs a plural version of the entity name
        /// </summary>
        /// <param name="type">The procedure type</param>
        /// <returns>Whether or not the ProcedureType needs a plural version of the entity name</returns>
        public static bool IsPluralType(ProcedureType type)
        {
            switch (type)
            {
                case ProcedureType.GetTotal:
                    return true;
                case ProcedureType.MultiGet:
                    return true;
                default:
                    return false;
            }
        }
    }
}

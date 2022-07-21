using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;

namespace SetupCommon
{
    /// <summary>
    /// The parameter for an SQL procedure
    /// </summary>
    [DebuggerDisplay("{Name}")]
    [Serializable]
    public class Parameter
    {
        /// <summary>
        /// The name of the property the parameter is referencing
        /// </summary>
        [XmlAttribute]
        public string Name { get; set; }

        /// <summary>
        /// Whether or not the parameter is referencing a pre-existing property
        /// </summary>
        [XmlAttribute]
        public bool IsPropertyBound { get; set; }

        /// <summary>
        /// C# type of the parameter
        /// </summary>
        [XmlAttribute]
        public string Type { get; set; }

        /// <summary>
        /// MSSQL type of the parameter
        /// </summary>
        [XmlAttribute]
        public string SqlType { get; set; }

        public Parameter()
        {
            IsPropertyBound = true;
        }
    }
}

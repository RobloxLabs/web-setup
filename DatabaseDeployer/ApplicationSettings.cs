using System;
using System.Collections.Generic;

namespace DatabaseDeployer
{
    /// <summary>
    /// DatabaseDeployer.ApplicationSettings
    /// </summary>
    public class ApplicationSettings
    {
        /// <summary>
        /// Gets the a list of connection string used by the application.
        /// </summary>
        /// <value>
        /// The list of connection strings to be used throughout execution.
        /// </value>
        public IDictionary<string, string> ConnectionStrings { get; set; }
    }
}

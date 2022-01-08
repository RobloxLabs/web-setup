﻿using System;
using System.Collections.Generic;

namespace DBWireup
{
    /// <summary>
    /// DBWireup.ApplicationSettings
    /// </summary>
    public class ApplicationSettings
    {
        /// <summary>
        /// Gets the default data source used when deploying SQL procedures.
        /// </summary>
        /// <value>
        /// The default connection string data source.
        /// </value>
        public string DefaultDataSource { get; set; }

        /// <summary>
        /// Gets the default user ID used when deploying SQL procedures.
        /// </summary>
        /// <value>
        /// The default connection string user ID.
        /// </value>
        public string DefaultUserID { get; set; }

        /// <summary>
        /// Gets the default password used when deploying SQL procedures.
        /// </summary>
        /// <value>
        /// The default password to be used in a connection string.
        /// </value>
        public string DefaultPassword { get; set; }

        /// <summary>
        /// Gets the a list of connection string used by the application.
        /// </summary>
        /// <value>
        /// The list of connection strings to be used throughout execution.
        /// </value>
        public IDictionary<string, string> ConnectionStrings { get; set; }
    }
}
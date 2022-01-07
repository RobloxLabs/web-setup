using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DatabaseDeployer
{
    public class Config
    {
        [JsonProperty("connectionString")]
        public string ConnectionString { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SetupCommon
{
    public enum CacheType
    {
        /// <summary>
        /// Cache will last until process ends
        /// </summary>
        Regular,
        /// <summary>
        /// Cache will expire within a certain amount of time
        /// </summary>
        Timed
    }
}

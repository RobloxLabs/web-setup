using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SetupCommon
{
    public interface Entity : IXmlSerializable
    {
        string Name { get; }

        [DefaultValue("Roblox")]
        string EntityNamespace { get; }

        string TableName { get; }

        [DefaultValue(false)]
        bool IsInternal { get; }

        /*[DefaultValue(null)]
        CacheType CacheType { get; }*/

        List<Property> Properties { get; }
    }
}

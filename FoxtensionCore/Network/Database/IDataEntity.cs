using System.Collections.Generic;

namespace Foxtension.Network.Database
{
    public interface IDataEntity
    {
        string TableName { get; }
        string[] Columns { get; set; }
        Dictionary<string, object> Properties { get; }
        Dictionary<string, object> Conditions { get; }
        string Trigger { get; set; }
    }
}
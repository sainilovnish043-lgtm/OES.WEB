using System.Data;

namespace OES.DATA.Database;

public interface ISqlConnectionFactory
{
    IDbConnection CreateConnection();
}

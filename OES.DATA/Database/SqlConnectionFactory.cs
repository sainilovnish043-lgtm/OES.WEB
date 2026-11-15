using System.Data;
using Microsoft.Data.SqlClient;

namespace OES.DATA.Database;

public sealed class SqlConnectionFactory(string connectionString) : ISqlConnectionFactory
{
    public IDbConnection CreateConnection() => new SqlConnection(connectionString);
}

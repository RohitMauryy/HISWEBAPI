using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace HISWEBAPI.Repositories
{
    public class Repository
    {
        private readonly IConfiguration _configuration;

        public Repository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Creates and returns a new SQL connection using the provided connection name.
        /// </summary>
        /// <param name="connectionName">Name of the connection string in appsettings.json (Default: "DefaultConnection")</param>
        /// <returns>SqlConnection object</returns>
        protected SqlConnection GetConnection(string connectionName = "DefaultConnection")
        {
            var connectionString = _configuration.GetConnectionString(connectionName);

            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException($"Connection string '{connectionName}' is not found in configuration.");

            return new SqlConnection(connectionString);
        }
    }
}

using System.Data;
using System.Data.SqlClient;

namespace Contratos_Adhesion.Services
{
    public interface IDbConnectionFactory
    {
        IDbConnection Create(int negocio);
        IDbConnection CreateGrupo(); // ← nuevo
    }

    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly IConfiguration _config;
        public DbConnectionFactory(IConfiguration config)
        {
            _config = config;
        }

        public IDbConnection Create(int negocio)
        {
            var key = negocio switch
            {
                1 => "ConexionToque",
                2 => "ConexionKique",
                3 => "ConexionNicui",
                4 => "ConexionReque",
                5 => "ConexionNivil",
                _ => throw new ArgumentException($"Negocio {negocio} no configurado.")
            };
            var connStr = _config.GetConnectionString(key);
            if (string.IsNullOrWhiteSpace(connStr))
                throw new InvalidOperationException($"Connection string '{key}' no encontrada o está vacía.");
            return new SqlConnection(connStr);
        }

        public IDbConnection CreateGrupo() // ← nuevo
        {
            var connStr = _config.GetConnectionString("ConexionGrupo");
            if (string.IsNullOrWhiteSpace(connStr))
                throw new InvalidOperationException("Connection string 'ConexionGrupo' no encontrada o está vacía.");
            return new SqlConnection(connStr);
        }
    }
}
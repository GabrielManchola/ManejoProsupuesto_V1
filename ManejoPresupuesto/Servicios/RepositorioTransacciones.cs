using Dapper;
using ManejoPresupuesto.Models;
using Microsoft.Data.SqlClient;

namespace ManejoPresupuesto.Servicios
{

    public interface IRepositorioTransacciones
    {
        Task CrearTransaccion(Transaccion transaccion);
    }
    public class RepositorioTransacciones: IRepositorioTransacciones
    {

        private readonly string connectionString;

        public RepositorioTransacciones(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task CrearTransaccion(Transaccion transaccion)
        {
            using var connection = new SqlConnection(connectionString);
            var id = await connection.QuerySingleAsync<int>("Transacciones_insertar", new {transaccion.UsuarioId, transaccion.FechaTransaccion,
                transaccion.Monto,
                transaccion.CuentaId,
                transaccion.Nota
            }, commandType: System.Data.CommandType.StoredProcedure);

            transaccion.Id = id;
        }






    }
}

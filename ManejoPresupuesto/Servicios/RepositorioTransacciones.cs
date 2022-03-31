using Dapper;
using ManejoPresupuesto.Models;
using Microsoft.Data.SqlClient;

namespace ManejoPresupuesto.Servicios
{

    public interface IRepositorioTransacciones
    {
        Task Actualizar(Transaccion transaccion, decimal montoAnterior, int cuentaAnterior);
        Task Borrar(int id);
        Task CrearTransaccion(Transaccion transaccion);
        Task<IEnumerable<Transaccion>> ObtenerPorCuentaId(ObtenerTransaccionesPorCuenta modelo);
        Task<Transaccion> ObtenerPorId(int id, int usuarioId);
        Task<IEnumerable<ResultadoObtenerPorSemana>> ObtenerPorSemana(ParametroObtenerTransaccionesPorusuario modelo);
        Task<IEnumerable<Transaccion>> ObtenerPorUsuarioId(ParametroObtenerTransaccionesPorusuario modelo);
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
                transaccion.CategoriaId,
                transaccion.CuentaId,
                transaccion.Nota
            }, commandType: System.Data.CommandType.StoredProcedure);

            transaccion.Id = id;
        }

        public async Task Actualizar(Transaccion transaccion, decimal montoAnterior, int cuentaAnteriorId)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.ExecuteAsync("Transacciones_actualizar", new
            {
                transaccion.Id,
                transaccion.FechaTransaccion,
                transaccion.Monto,
                montoAnterior,
                transaccion.CuentaId,
                cuentaAnteriorId,
                transaccion.CategoriaId,
                transaccion.Nota
            }, commandType: System.Data.CommandType.StoredProcedure);
        }

        public async Task<Transaccion> ObtenerPorId(int id, int usuarioId)
        {
            using var connection = new SqlConnection(connectionString);
            return await connection.QueryFirstOrDefaultAsync<Transaccion>(@"SELECT Transacciones.*, cat.TipoOperacionId FROM Transacciones 
                                                                          INNER JOIN Categorias cat on cat.Id = Transacciones.CategoriaId
                                                                          Where Transacciones.Id = @Id and Transacciones.UsuarioId = @UsuarioId", new { id, usuarioId });


        }

        public async Task Borrar(int id)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.ExecuteAsync("Transacciones_borrar", new { id }, commandType: System.Data.CommandType.StoredProcedure);

        }


        public async Task<IEnumerable<Transaccion>> ObtenerPorCuentaId(ObtenerTransaccionesPorCuenta modelo)
        {
            using var connection = new SqlConnection(connectionString);
            return await connection.QueryAsync<Transaccion>(@"select t.Id, t.Monto, t.FechaTransaccion, c.Nombre as Categoria, cu.Nombre as Cuenta, c.TipoOperacionId from Transacciones t
                                                              inner join Categorias c on c.Id = t.CategoriaId inner join Cuentas cu on cu.Id = t.CuentaId
                                                              where t.CuentaId = @CuentaId and t.UsuarioId = @UsuarioId
                                                              and FechaTransaccion between @FechaInicio and @FechaFin", modelo);


        }

        public async Task<IEnumerable<Transaccion>> ObtenerPorUsuarioId(ParametroObtenerTransaccionesPorusuario modelo)
        {
            using var connection = new SqlConnection(connectionString);
            return await connection.QueryAsync<Transaccion>(@"select t.Id, t.Monto, t.FechaTransaccion, cu.Nombre as Categoria, c.Nombre as Cuenta, c.TipoOperacionId 
                                                              from Transacciones t
                                                              inner join Categorias c on c.Id = t.CategoriaId inner join Cuentas cu on cu.Id = t.CuentaId
                                                              where t.UsuarioId = @UsuarioId
                                                              and FechaTransaccion between @FechaInicio and @FechaFin
                                                              ORDER BY t.FechaTransaccion DESC", modelo);


        }

        public async Task<IEnumerable<ResultadoObtenerPorSemana>> ObtenerPorSemana(ParametroObtenerTransaccionesPorusuario modelo)
        {
            using var connection = new SqlConnection(connectionString);
            return await connection.QueryAsync<ResultadoObtenerPorSemana>(@"Select DATEDIFF(d, @fechaInicio, FechaTransaccion) / 7 + 1 as Semana, sum(Monto) as Monto, C.TipoOperacionId as Operacion
                                                                              from Transacciones T
                                                                              Inner join Categorias C
                                                                              ON C.Id = T.CategoriaId
                                                                              Where T.UsuarioId = @usuarioId and FechaTransaccion between @fechaInicio and @fechaFin
                                                                              group by DATEDIFF(d, @fechaInicio, FechaTransaccion) / 7 + 1, C.TipoOperacionId", modelo);
        }



    }
}

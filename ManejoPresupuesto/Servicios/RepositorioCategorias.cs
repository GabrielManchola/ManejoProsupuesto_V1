using Dapper;
using ManejoPresupuesto.Models;
using Microsoft.Data.SqlClient;

namespace ManejoPresupuesto.Servicios
{

    public interface IRepositorioCategorias
    {
        Task ActualizarCategoria(Categoria categoria);
        Task Crear(Categoria categoria);
        Task<IEnumerable<Categoria>> Obtener(int usuarioId);
        Task<Categoria> ObtenerPorId(int id, int usuarioId);
    }

    public class RepositorioCategorias : IRepositorioCategorias
    {
        private readonly string connectionString;
        public RepositorioCategorias(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task Crear(Categoria categoria)
        {
            using var connection = new SqlConnection(connectionString);
            var id = await connection.QuerySingleAsync<int>(@"INSERT INTO Categorias (Nombre, TipoOperacionId, UsuarioId)
                                                              VALUES(@Nombre, @TipoOperacionId, @UsuarioId)

                                                              Select SCOPE_IDENTITY()", categoria);

            categoria.Id = id;
        }

        public async Task<IEnumerable<Categoria>> Obtener(int usuarioId)
        {
            using var connection = new SqlConnection(connectionString);
            return await connection.QueryAsync<Categoria>("Select * from categorias where UsuarioId = @UsuarioId", new {usuarioId});

        }

        public async Task<Categoria> ObtenerPorId(int id, int usuarioId)
        {
            using var connection = new SqlConnection(connectionString);
            return await connection.QueryFirstOrDefaultAsync<Categoria>(@"Select * from Categorias Where id = @id and UsuarioId = @UsuarioId", new {id, usuarioId});


        }

        public async Task ActualizarCategoria(Categoria categoria)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.ExecuteAsync(@"update Categorias set Nombre = @Nombre, TipoOperacionId = @TipoOperacionId where Id = @Id", categoria);

            
        }


    }
}

using Dapper;
using ManejoPresupuesto.Models;
using Microsoft.Data.SqlClient;

namespace ManejoPresupuesto.Servicios
{
    public interface IRepositorioUsuarios
    {
        Task<Usuario> BuscarPorEmail(string emailNomrmalizado);
        Task<int> CrearUsuario(Usuario usuario);
    }
    public class RepositorioUsuarios : IRepositorioUsuarios
    {

        private readonly string connectionString;
        public RepositorioUsuarios(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
        }


        public async Task<int> CrearUsuario(Usuario usuario)
        {
            using var connection = new SqlConnection(connectionString);
            var usuarioId = await connection.QuerySingleAsync<int>(@"insert into Usuarios(Email, EmailNormalizado, PasswordHash)
                                                              values(@Email, @EmailNormalizado, @PasswordHash);
                                                              Select SCOPE_IDENTITY();", usuario);

            await connection.ExecuteAsync("CrearDatosUsuarioNuevo", new { usuarioId }, commandType: System.Data.CommandType.StoredProcedure);

            return usuarioId;  
        }


        public async Task<Usuario> BuscarPorEmail(string emailNomrmalizado)
        {
            var connection = new SqlConnection(connectionString);
            return await connection.QuerySingleOrDefaultAsync<Usuario>("Select * from Usuarios Where EmailNormalizado = @emailNomrmalizado", new { emailNomrmalizado });

        }

    }
}

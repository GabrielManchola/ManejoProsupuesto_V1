using System.Security.Claims;

namespace ManejoPresupuesto.Servicios
{

    public interface IServicioUsuarios
    {
        int ObtenerusuarioId();
    }
    public class ServicioUsuarios: IServicioUsuarios
    {
        private readonly HttpContext httpContext;

        public ServicioUsuarios(IHttpContextAccessor httpContextAccessor)
        {
            httpContext = httpContextAccessor.HttpContext;
        }

        public int ObtenerusuarioId()
        {
            if (httpContext.User.Identity.IsAuthenticated)
            {

                
                var idclaim = httpContext.User.Claims.Where(x => x.Type == ClaimTypes.NameIdentifier).FirstOrDefault();
                var id = int.Parse(idclaim.Value);
                return id;

                
            }
            else
            {
                throw new ApplicationException("El usuario no esta autenticado");
            }
            
        }
    }
}

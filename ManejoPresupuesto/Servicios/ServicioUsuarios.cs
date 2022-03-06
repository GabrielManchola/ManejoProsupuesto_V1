namespace ManejoPresupuesto.Servicios
{

    public interface IServicioUsuarios
    {
        int ObtenerusuarioId();
    }
    public class ServicioUsuarios: IServicioUsuarios
    {

        public int ObtenerusuarioId()
        {
            return 1;
        }
    }
}

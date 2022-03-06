using ManejoPresupuesto.Validaciones;
using System.ComponentModel.DataAnnotations;

namespace ManejoPresupuesto.Models
{
    public class Categoria
    {

        public int Id { get; set; }

        [PrimeraLetraMayuscula]
        [Required(ErrorMessage ="Este campo es requerido")]
        [StringLength(maximumLength:50, ErrorMessage ="NO puede ser mayor a {1} caracteres")]
        public string Nombre { get; set; }

        [Display(Name ="Tipo Operacion")]
        public TipoOperacion TipoOperacionId { get; set; }

        public int UsuarioId { get; set; }
    }
}

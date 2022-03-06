using ManejoPresupuesto.Validaciones;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ManejoPresupuesto.Models
{
    public class TipoCuenta //: IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        //ejemplo de creacion de validaciones a mano desde otra clase
        [PrimeraLetraMayuscula]
        [Remote(action: "VerificarExisteTipoCuenta", controller: "TiposCuentas")]
        public string Nombre { get; set; }

        public int UsuarioId { get; set; }

        public int Orden { get; set; }

//        Ejemplo de como hacer validaciones a nivel de modelo para usar todos lso campos facilmente, si se quita la especificacion de campo entonces el errro sera a nivel de medelo
//        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
//        {
//            if(Nombre != null && Nombre.Length > 0)
//            {
//                var primeraLetra = Nombre[0].ToString();

//                if(primeraLetra != primeraLetra.ToUpper())
//                {
//                    yield return new ValidationResult("La primera letra debe ser mayuscula",
//                        new[] {nameof(Nombre)});
//                }
//            }
//        }
   }
}

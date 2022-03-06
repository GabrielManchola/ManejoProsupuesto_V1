using Dapper;
using ManejoPresupuesto.Models;
using ManejoPresupuesto.Servicios;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace ManejoPresupuesto.Controllers
{
    public class TiposCuentasController : Controller 
    {

        private readonly IRepositorioTiposCuentas repositorioTiposCuentas;
        private readonly IServicioUsuarios servicioUsuarios;

        public TiposCuentasController(IRepositorioTiposCuentas repositorioTiposCuentas, IServicioUsuarios servicioUsuarios)
        {
            RepositorioTiposCuentas = repositorioTiposCuentas;
            this.servicioUsuarios = servicioUsuarios;
        }

        public IRepositorioTiposCuentas RepositorioTiposCuentas { get; }

        //Ejemplo de prueba de conexion a bd 
        //private readonly string connecionString;
        //public TiposCuentasController(IConfiguration configuration)
        //{
        //    connecionString = configuration.GetConnectionString("DefaultConnection");
        //}
        //Las validaciones que se hacen a nivel de controlador se hacen normalmente cuando se trata de validar bases de datos 
        public IActionResult Crear()
        {
            //Ejemplo de prueba de conexion a bd 
            //using (var connection = new SqlConnection(connecionString))
            //{
            //    var query = connection.Query("SELECT 1").FirstOrDefault();
            //}
            return View();
        }

        public async Task<IActionResult> Index()
        {
            var usuarioId = servicioUsuarios.ObtenerusuarioId();
            var tiposCuentas = await RepositorioTiposCuentas.Obtener(usuarioId);
            return View(tiposCuentas);
        }

        [HttpPost]
        public async Task<IActionResult> Crear(TipoCuenta tipoCuenta)
        {
            if(!ModelState.IsValid)
            {
                return View(tipoCuenta);
            }

            tipoCuenta.UsuarioId = servicioUsuarios.ObtenerusuarioId(); ;

            var yaExisteTipoCuenta = await RepositorioTiposCuentas.Existe(tipoCuenta.Nombre, tipoCuenta.UsuarioId);

            if (yaExisteTipoCuenta)
            {
                ModelState.AddModelError(nameof(tipoCuenta.Nombre), $"El nombre {tipoCuenta.Nombre} ya existe.");
                return View(tipoCuenta);
            }
            await RepositorioTiposCuentas.Crear(tipoCuenta);

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<ActionResult> Editar(int id)
        {
            var usuarioId = servicioUsuarios.ObtenerusuarioId();
            var tipoCuenta = await RepositorioTiposCuentas.ObtenerPorId(id, usuarioId);

            if (tipoCuenta is null)
            {
                return RedirectToAction("Noencontrado", "Home");
            }

            return View(tipoCuenta);
        }

        [HttpPost]
        public async Task<ActionResult> Editar(TipoCuenta tipoCuenta)
        {
            var usuarioId = servicioUsuarios.ObtenerusuarioId();
            var tipoCuentaExiste = await RepositorioTiposCuentas.ObtenerPorId(tipoCuenta.Id, usuarioId);

            if(tipoCuentaExiste is null)
            {
                return RedirectToAction("NoEncontrado", "Home");

            }

            await RepositorioTiposCuentas.ActualizarTiposCuentas(tipoCuenta);

            return RedirectToAction("Index");

        }

        public async Task<IActionResult> Borrar(int Id)
        {
            var usuarioId = servicioUsuarios.ObtenerusuarioId();
            var tipoCuenta = await RepositorioTiposCuentas.ObtenerPorId(Id, usuarioId);
        
            if (tipoCuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }
            return View(tipoCuenta);

        }

        [HttpPost]
        public async Task<IActionResult> BorrarTipoCuenta(int Id)
        {
            var usuarioId = servicioUsuarios.ObtenerusuarioId();
            var tipoCuenta = await RepositorioTiposCuentas.ObtenerPorId(Id, usuarioId);

            if (tipoCuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }
            await RepositorioTiposCuentas.Borrar(Id);
            return RedirectToAction("Index");
        }


        //con este tipo de metodos puedo validar los campos si ya fueron creados en la bd sin tener que darle enviar 
        [HttpGet]
        public async Task<IActionResult> VerificarExisteTipoCuenta(String nombre)
        {
            var UsuarioId = servicioUsuarios.ObtenerusuarioId(); 
            var yaExisteTipoCuenta = await RepositorioTiposCuentas.Existe(nombre, UsuarioId);
            if (yaExisteTipoCuenta)
            {
                return Json($"EL nombre {nombre} ya existe");
            }
            return Json(true);
        }

        [HttpPost]
        public async Task<IActionResult> Ordenar([FromBody] int[] ids)
        {

            var UsuarioId = servicioUsuarios.ObtenerusuarioId();
            var tiposCuentas = RepositorioTiposCuentas.Obtener(UsuarioId);

            var idsTiposCuentas = tiposCuentas.Result.Select(x => x.Id);
            var idsTiposCuentasNoPertenecenAlUsuario = ids.Except(idsTiposCuentas).ToList();
            if(idsTiposCuentasNoPertenecenAlUsuario.Count > 0)
            {
                return Forbid();
            }

            var tiposCuentasOrdenados = ids.Select((valor, indice) => new TipoCuenta() { Id = valor, Orden = indice + 1 }).AsEnumerable();

            await RepositorioTiposCuentas.Ordenar(tiposCuentasOrdenados);

            return Ok();
        }



    }
}

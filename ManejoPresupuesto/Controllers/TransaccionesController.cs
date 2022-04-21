using AutoMapper;
using ClosedXML.Excel;
using ManejoPresupuesto.Models;
using ManejoPresupuesto.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace ManejoPresupuesto.Controllers
{
    //[Authorize] se usa para definir que todo lo que este dentro de esta clase tiene que ser mediante un usuario autenticado
    public class TransaccionesController : Controller
    {
        private readonly IServicioUsuarios servicioUsuarios;
        private readonly IRepositorioCuentas repositorioCuentas;
        private readonly IRepositorioCategorias repositorioCategorias;
        private readonly IRepositorioTransacciones repositorioTransacciones;
        private readonly IMapper mapper;
        private readonly IServicioReportes servicioReportes;

        
        public TransaccionesController(IServicioUsuarios servicioUsuarios, IRepositorioCuentas repositorioCuentas, 
                                       IRepositorioCategorias repositorioCategorias, IRepositorioTransacciones repositorioTransacciones,
                                       IMapper mapper, IServicioReportes servicioReportes)
        {
            this.servicioUsuarios = servicioUsuarios;
            this.repositorioCuentas = repositorioCuentas;
            this.repositorioCategorias = repositorioCategorias;
            this.repositorioTransacciones = repositorioTransacciones;
            this.mapper = mapper;
            this.servicioReportes = servicioReportes;
        }

        
        public async Task<IActionResult> Index(int mes, int año)
        {
            var usuarioId = servicioUsuarios.ObtenerusuarioId();

            var modelo = await servicioReportes.ObtenerReporteTransaccionesDetalladas(usuarioId, mes, año, ViewBag);

            return View(modelo);  
        }

        public async Task<IActionResult> Semanal(int mes, int año)
        {
            var usuarioId = servicioUsuarios.ObtenerusuarioId();

            IEnumerable<ResultadoObtenerPorSemana> transaccionesPorSemana = await servicioReportes.ObtenerReporteSemanal(usuarioId,mes,año,ViewBag);

            var agrupado = transaccionesPorSemana.GroupBy(x => x.Semana).Select(x =>
            new ResultadoObtenerPorSemana()
            {
                Semana = x.Key,
                Ingresos = x.Where(x => x.TipoOperacionId == TipoOperacion.Ingreso).Select(x => x.Monto).FirstOrDefault(),
                Gastos = x.Where(x => x.TipoOperacionId == TipoOperacion.Gasto).Select(x => x.Monto).FirstOrDefault(),
                

            }).ToList();    

            if (año == 0 || mes == 0)
            {
                var dia = DateTime.Today;
                año = dia.Year;
                mes = dia.Month;    
            }

            var fechaReferencia = new DateTime(año, mes, 1);
            var diasDelMes = Enumerable.Range(1, fechaReferencia.AddMonths(1).AddDays(-1).Day);

            var diasSegmentados = diasDelMes.Chunk(7).ToList();

            for (int i = 0; i < diasSegmentados.Count(); i++)
            {
                var semana = i + 1;
                var fechaInicio = new DateTime(año, mes, diasSegmentados[i].First());
                var fechaFin = new DateTime(año, mes, diasSegmentados[i].Last());   
                var grupoSemana = agrupado.FirstOrDefault(x => x.Semana == semana);

                if(grupoSemana is null)
                {
                    agrupado.Add(new ResultadoObtenerPorSemana()
                    {

                        Semana = semana,
                        FechaInicio = fechaInicio,
                        FechaFin = fechaFin,

                    });
                }
                else{

                    grupoSemana.FechaInicio = fechaInicio;
                    grupoSemana.FechaFin = fechaFin;

                }
            }

            agrupado = agrupado.OrderByDescending(x => x.Semana).ToList();

            var modelo = new ReporteSemanaViewModel();
            modelo.TransaccionesPorSemana = agrupado;
            modelo.FechaReferencia = fechaReferencia;
            

            return View(modelo);
        }

        public async Task<IActionResult> Mensual(int año)
        {
            var usuarioId = servicioUsuarios.ObtenerusuarioId();

            if (año == 0)
            {
                año = DateTime.Today.Year;
            }

            var transaccionesPorMes = await repositorioTransacciones.ObtenerPorMes(usuarioId, año);

            var transaccionesAgrupadas = transaccionesPorMes.GroupBy(x => x.Mes).Select(x => new ResultadoObtenerPorMes()
            {
                Mes = x.Key,
                Ingreso = x.Where(x => x.TipoOperacionId == TipoOperacion.Ingreso).Select(x => x.Monto).FirstOrDefault(),
                Gasto = x.Where(x => x.TipoOperacionId == TipoOperacion.Gasto).Select(x => x.Monto).FirstOrDefault(),


            }).ToList();

            for(int mes = 1; mes <= 12; mes++)
            {
                var transaccion = transaccionesAgrupadas.FirstOrDefault(x => x.Mes == mes);
                var fechaDeRefeencia = new DateTime(año, mes, 1);
                if(transaccion is null)
                {
                    transaccionesAgrupadas.Add(new ResultadoObtenerPorMes()
                    {
                        Mes = mes,
                        FechaReferencia = fechaDeRefeencia
                    });
                }
                else
                {
                    transaccion.FechaReferencia = fechaDeRefeencia;
                }
            }


            transaccionesAgrupadas = transaccionesAgrupadas.OrderByDescending(x => x.Mes).ToList();

            var modelo = new ReporteMensualViewModel();

            modelo.Año = año;
            modelo.TransaccionesPorMes = transaccionesAgrupadas;


            return View(modelo);
        }
        public IActionResult ReporteExcel()
        {
            return View();
        }
        //reportes de excel
        [HttpGet]
        public async Task<FileResult> ExportarExcelPorMes(int mes, int año)
        {
            var fechaInicio = new DateTime(año, mes, 1);
            var fechaFin = fechaInicio.AddMonths(1).AddDays(-1);
            var usuarioId = servicioUsuarios.ObtenerusuarioId();

            var transacciones = await repositorioTransacciones.ObtenerPorUsuarioId(new ParametroObtenerTransaccionesPorusuario
            {
                UsuarioId = usuarioId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            });

            var nombreArchivo = $"Manejo Presupuesto - {fechaInicio.ToString("MMM yyyy")}.xlsx";

            return GenerarExcel(nombreArchivo, transacciones);

        }

        [HttpGet]
        public async Task<FileResult> ExportarExcelPoraño(int año)
        {
            var fechaInicio = new DateTime(año, 1, 1);
            var fechaFin = fechaInicio.AddYears(1).AddDays(-1);
            var usuarioId = servicioUsuarios.ObtenerusuarioId();

            var transacciones = await repositorioTransacciones.ObtenerPorUsuarioId(new ParametroObtenerTransaccionesPorusuario
            {
                UsuarioId = usuarioId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            });

            var nombreArchivo = $"Manejo presupuesto- {fechaInicio.ToString("yyyy")}.xlsx";

            return GenerarExcel(nombreArchivo, transacciones);

        }

        [HttpGet]
        public async Task<FileResult> ExportarExcelConTodo()
        {
            var fechaInicio = DateTime.Today.AddYears(-100);
            var fechaFin = DateTime.Today.AddYears(1000);
            var usuarioId = servicioUsuarios.ObtenerusuarioId();

            var transacciones = await repositorioTransacciones.ObtenerPorUsuarioId(new ParametroObtenerTransaccionesPorusuario
            {
                UsuarioId = usuarioId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            });

            var nombreArchivo = $"Manejo presupuesto- {DateTime.Today.ToString("dd-MM-yyy")}.xlsx";

            return GenerarExcel(nombreArchivo, transacciones);

        }
    

        private FileResult GenerarExcel(string nombreArchivo, IEnumerable<Transaccion> transacciones)
        {
            DataTable dataTable = new DataTable("Transacciones");

            dataTable.Columns.AddRange(new DataColumn[] { 

                new DataColumn("Fecha"),
                new DataColumn("Cuenta"),
                new DataColumn("Categoria"),
                new DataColumn("Nota"),
                new DataColumn("Monto"),
                new DataColumn("Ingreso/Gasto"),

            });

            foreach(var transaccion in transacciones)
            {
                dataTable.Rows.Add(
                    
                    transaccion.FechaTransaccion, 
                    transaccion.Cuenta,
                    transaccion.Categoria,
                    transaccion.Nota,
                    transaccion.Monto,
                    transaccion.TipoOperacionId

                    );
            }

            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dataTable);

                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo);

                }
            }

        }

        //fin reportes de excel

        public IActionResult Calendario()
        {
            return View();
        }

        public async Task<JsonResult> TransaccionesCalendario(DateTime start, DateTime end)
        {
            var usuarioId = servicioUsuarios.ObtenerusuarioId();

            var transacciones = await repositorioTransacciones.ObtenerPorUsuarioId(new ParametroObtenerTransaccionesPorusuario
            {
                UsuarioId = usuarioId,
                FechaInicio = start,
                FechaFin = end
            });

            var eventosCalendario = transacciones.Select(Transaccion => new EventoCalendario()
            {
                Title = Transaccion.Monto.ToString("N"),
                Start = Transaccion.FechaTransaccion.ToString("yyyy-MM-dd"),
                End = Transaccion.FechaTransaccion.ToString("yyyy-MM-dd"),
                Color = (Transaccion.TipoOperacionId == TipoOperacion.Gasto) ? "Red" : null

            });

            return Json(eventosCalendario); 

        }

        public async Task<JsonResult> ObtenerTransaccionesPorFechaCalendario(DateTime fecha)
        {
            var usuarioId = servicioUsuarios.ObtenerusuarioId();

            var transacciones = await repositorioTransacciones.ObtenerPorUsuarioId(new ParametroObtenerTransaccionesPorusuario
            {
                UsuarioId = usuarioId,
                FechaInicio = fecha,
                FechaFin = fecha
            });

            return Json(transacciones);
        }


        public async Task<IActionResult> Crear()
        {
            var usuarioId = servicioUsuarios.ObtenerusuarioId();
            var modelo = new TransaccionCreacionViewModel();
            modelo.Cuentas = await ObtenerCuentas(usuarioId);
            modelo.Categorias = await ObtenerCategorias(usuarioId, modelo.TipoOperacionId); 
            return View(modelo);

        }

        [HttpPost]
        public async Task<IActionResult> Crear(TransaccionCreacionViewModel modelo)
        {
            var usuarioId = servicioUsuarios.ObtenerusuarioId();

            if (!ModelState.IsValid)
            {
                modelo.Cuentas = await ObtenerCuentas(usuarioId);
                modelo.Categorias = await ObtenerCategorias(usuarioId, modelo.TipoOperacionId);
                return View(modelo);
            }

            var cuenta = await repositorioCuentas.ObtenerPorId(modelo.CuentaId, usuarioId);
            if(cuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            var categoria = await repositorioCategorias.ObtenerPorId(modelo.CategoriaId, usuarioId);
            if(categoria is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            modelo.UsuarioId = usuarioId;

            if(modelo.TipoOperacionId == TipoOperacion.Gasto)
            {
                modelo.Monto *= -1;
            }

            await repositorioTransacciones.CrearTransaccion(modelo);
            return RedirectToAction("Index");

        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id, string urlRetorno = null)
        {
            var usuarioId = servicioUsuarios.ObtenerusuarioId();
            var transaccion = await repositorioTransacciones.ObtenerPorId(id, usuarioId);

            if(transaccion is null)
            {
                return RedirectToAction("NoEncontrado", "Home");

            }

            var modelo = mapper.Map<TransaccionActualizacionViewModel>(transaccion);

            modelo.MontoAnterior = modelo.Monto;

            if(modelo.TipoOperacionId == TipoOperacion.Gasto)
            {
                modelo.MontoAnterior = modelo.Monto * -1;   
            }

            modelo.CuentaAnteriorId = transaccion.CuentaId;
            modelo.Categorias = await ObtenerCategorias(usuarioId, transaccion.TipoOperacionId);
            modelo.Cuentas = await ObtenerCuentas(usuarioId);
            modelo.UrlRetorno = urlRetorno;

            return View(modelo);

        }

        [HttpPost]
        public async Task<IActionResult> Editar(TransaccionActualizacionViewModel modelo)
        {
            var usuarioId = servicioUsuarios.ObtenerusuarioId();

            if (!ModelState.IsValid)
            {
                modelo.Cuentas = await ObtenerCuentas(usuarioId);
                modelo.Categorias = await ObtenerCategorias(usuarioId, modelo.TipoOperacionId);
                return View(modelo);
            }

            var cuenta = await repositorioCuentas.ObtenerPorId(modelo.CuentaId, usuarioId);

            if(cuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            var categoria = await repositorioCategorias.ObtenerPorId(modelo.CategoriaId, usuarioId);

            if (categoria is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            var transaccion = mapper.Map<Transaccion>(modelo);

            if(modelo.TipoOperacionId == TipoOperacion.Gasto)
            {
                transaccion.Monto *= -1;
            }

            await repositorioTransacciones.Actualizar(transaccion, modelo.MontoAnterior, modelo.CuentaAnteriorId);

            if (string.IsNullOrEmpty(modelo.UrlRetorno))
            {
                return RedirectToAction("Index");
            }
            else
            {
                return LocalRedirect(modelo.UrlRetorno);
            }


        }

        [HttpPost]
        public async Task<IActionResult> Borrar(int id, string urlRetorno = null)
        {
            var usuarioId = servicioUsuarios.ObtenerusuarioId();
            var transaccion = await repositorioTransacciones.ObtenerPorId(id, usuarioId);

            if (transaccion is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            await repositorioTransacciones.Borrar(id);
           


            if (string.IsNullOrEmpty(urlRetorno))
            {
                return RedirectToAction("Index");
            }
            else
            {
                return LocalRedirect(urlRetorno);
            }

        }




        private async Task<IEnumerable<SelectListItem>> ObtenerCuentas(int usuarioId)
        {
            var cuentas = await repositorioCuentas.Buscar(usuarioId);
            return cuentas.Select(x => new SelectListItem(x.Nombre, x.Id.ToString()));
        }

        private async Task<IEnumerable<SelectListItem>> ObtenerCategorias(int ususarioId, TipoOperacion tipoOperacion)
        {
            var categorias = await repositorioCategorias.Obtener(ususarioId, tipoOperacion);
            return categorias.Select(x => new SelectListItem(x.Nombre, x.Id.ToString()));
        }

        [HttpPost]
        public async Task<IActionResult> ObtenerCategorias([FromBody] TipoOperacion tipoOperacion)
        {
            var usuarioId = servicioUsuarios.ObtenerusuarioId();
            var categorias = await ObtenerCategorias(usuarioId, tipoOperacion);
            return Ok(categorias);

        }

    }
}

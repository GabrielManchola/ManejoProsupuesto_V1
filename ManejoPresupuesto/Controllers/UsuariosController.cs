using ManejoPresupuesto.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ManejoPresupuesto.Controllers
{
    public class UsuariosController : Controller 
    {
        private readonly UserManager<Usuario> userManager;
        private readonly SignInManager<Usuario> signInManager;

        public UsuariosController(UserManager<Usuario> userManager, SignInManager<Usuario> signInManager)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
        }

      
        [AllowAnonymous]
        public IActionResult Registro()
        {
            return View();  
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Registro(RegistroViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);    
            }

            var usuario = new Usuario() { Email = modelo.Email };

            var resultado = await userManager.CreateAsync(usuario, password: modelo.Password);

            if (resultado.Succeeded)
            {
                await signInManager.SignInAsync(usuario, isPersistent: true);
                return RedirectToAction("Index", "Transacciones");
            }
            else
            {
                foreach(var error  in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(modelo);
            }   
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            /*
            if (User.Identity.IsAuthenticated)
            {
                //esto solo se ejecutara cuando el usuario este autenticado
                //codigo siguiente es un ejemplo para sacar el id del usuario 
                var claims = User.Claims.ToList();
                var usuarioIdReal = claims.Where(x => x.Type == ClaimTypes.NameIdentifier).FirstOrDefault();
                var id = usuarioIdReal.Value;
            }
            else
            {
                //signInManager el usuario no esta autenticado 
            }
            */

            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            
            var resultado = await signInManager.PasswordSignInAsync(modelo.Email, modelo.Password, modelo.Recuerdame, lockoutOnFailure: false);
           
            
            if (resultado.Succeeded)
            {
                return RedirectToAction("Index", "Transacciones");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Nombre de usuario o paswword incorrecto");
                return View(modelo);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

            return RedirectToAction("Index", "Transacciones");
        }



    }
}

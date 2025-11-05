using Microsoft.AspNetCore.Mvc;
using HelpFast_Pim.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using HelpFast_Pim.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace HelpFast_Pim.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IUsuarioService _usuarioService;

        public HomeController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        public async Task<IActionResult> Index()
        {
            // obter id do claim
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var usuario = await _usuarioService.ObterPorIdAsync(userId);
            if (usuario == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Account");
            }

            return View(usuario);
        }
    }
}

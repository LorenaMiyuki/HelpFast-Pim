using Microsoft.AspNetCore.Mvc;
using HelpFast_Pim.Models;
using HelpFast_Pim.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace HelpFast_Pim.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUsuarioService _usuarioService;

        public AccountController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new Usuario());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(Usuario model, string? returnUrl = null)
        {
            // Garantir que apenas Email e Senha sejam validados
            ModelState.Clear();

            if (model == null || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Senha))
            {
                ModelState.AddModelError(string.Empty, "Email e senha são obrigatórios.");
                return View(model);
            }

            try
            {
                var valido = await _usuarioService.ValidarLoginAsync(model.Email.Trim(), model.Senha);
                if (!valido)
                {
                    ModelState.AddModelError(string.Empty, "Credenciais inválidas.");
                    return View(model);
                }

                var usuario = await _usuarioService.ObterPorEmailAsync(model.Email.Trim());
                if (usuario == null)
                {
                    ModelState.AddModelError(string.Empty, "Usuário não encontrado.");
                    return View(model);
                }

                await _usuarioService.AtualizarUltimoLoginAsync(usuario.Id);

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                    new Claim(ClaimTypes.Name, usuario.Nome ?? ""),
                    new Claim(ClaimTypes.Email, usuario.Email ?? ""),
                    new Claim("CargoId", usuario.CargoId.ToString()),
                    new Claim(ClaimTypes.Role, usuario.Cargo?.Nome ?? "Cliente")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Erro ao autenticar. Tente novamente.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new Usuario());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Usuario model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Senha) || string.IsNullOrWhiteSpace(model.Nome))
            {
                ModelState.AddModelError(string.Empty, "Nome, email e senha são obrigatórios.");
                return View(model);
            }

            try
            {
                if (await _usuarioService.EmailExisteAsync(model.Email.Trim()))
                {
                    ModelState.AddModelError(string.Empty, "Já existe um usuário com este e-mail.");
                    return View(model);
                }

                var usuario = new Usuario
                {
                    Nome = model.Nome.Trim(),
                    Email = model.Email.Trim(),
                    Telefone = model.Telefone,
                    CargoId = 3
                };

                var criado = await _usuarioService.RegistrarAsync(usuario, model.Senha);
                if (criado == null)
                {
                    ModelState.AddModelError(string.Empty, "Não foi possível registrar o usuário.");
                    return View(model);
                }

                TempData["SuccessMessage"] = "Cadastro realizado com sucesso. Faça login para continuar.";
                return RedirectToAction("Login", "Account");
            }
            catch (ArgumentException aex)
            {
                ModelState.AddModelError(string.Empty, aex.Message);
                return View(model);
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Erro ao registrar o usuário.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}

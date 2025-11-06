using Microsoft.AspNetCore.Mvc;
using HelpFast_Pim.Data;
using HelpFast_Pim.Models;
using HelpFast_Pim.Services;
using Microsoft.EntityFrameworkCore;

namespace HelpFast_Pim.Controllers
{
    [Route("[controller]")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IUsuarioService _usuarioService;

        public AdminController(AppDbContext db, IUsuarioService usuarioService)
        {
            _db = db;
            _usuarioService = usuarioService;
        }

        // GET /Admin/Usuarios - Lista todos os usuários
        [HttpGet("Usuarios")]
        public async Task<IActionResult> Usuarios()
        {
            var usuarios = await _db.Usuarios
                .Include(u => u.Cargo)
                .AsNoTracking()
                .ToListAsync();
            
            return View(usuarios);
        }

        // GET /Admin/CriarTecnico - Tela de criar novo técnico
        [HttpGet("CriarTecnico")]
        public IActionResult CriarTecnico()
        {
            return View();
        }

        // POST /Admin/SalvarTecnico - Salva novo técnico
        [HttpPost("SalvarTecnico")]
        public async Task<IActionResult> SalvarTecnico([FromForm] string nome, [FromForm] string email, [FromForm] string telefone)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(telefone))
                {
                    TempData["Erro"] = "Todos os campos são obrigatórios.";
                    return RedirectToAction("CriarTecnico");
                }

                // Verificar se email já existe
                var emailExiste = await _db.Usuarios.AnyAsync(u => u.Email == email);
                if (emailExiste)
                {
                    TempData["Erro"] = "Este email já está cadastrado.";
                    return RedirectToAction("CriarTecnico");
                }

                // Obter cargo "Técnico"
                var cargoTecnico = await _db.Cargos.FirstOrDefaultAsync(c => c.Nome == "Técnico");
                if (cargoTecnico == null)
                {
                    // Criar cargo se não existir
                    cargoTecnico = new Cargo { Nome = "Técnico" };
                    _db.Cargos.Add(cargoTecnico);
                    await _db.SaveChangesAsync();
                }

                // Criar usuário
                var usuario = new Usuario
                {
                    Nome = nome,
                    Email = email,
                    Telefone = telefone,
                    Senha = Guid.NewGuid().ToString(),
                    CargoId = cargoTecnico.Id
                };

                _db.Usuarios.Add(usuario);
                await _db.SaveChangesAsync();

                TempData["Sucesso"] = $"Técnico '{nome}' cadastrado com sucesso!";
                return RedirectToAction("Usuarios");
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao cadastrar técnico: {ex.Message}";
                return RedirectToAction("CriarTecnico");
            }
        }

        // GET /Admin/Editar/{id} - Tela de editar usuário
        [HttpGet("Editar/{id}")]
        public async Task<IActionResult> Editar(int id)
        {
            var usuario = await _db.Usuarios
                .Include(u => u.Cargo)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null)
            {
                return NotFound();
            }

            var cargos = await _db.Cargos.AsNoTracking().ToListAsync();
            ViewBag.Cargos = cargos;

            return View(usuario);
        }

        // POST /Admin/Atualizar - Atualiza dados do usuário
        [HttpPost("Atualizar")]
        public async Task<IActionResult> Atualizar(int id, [FromForm] string nome, [FromForm] string email, [FromForm] string telefone, [FromForm] int cargoId)
        {
            try
            {
                var usuario = await _db.Usuarios.FindAsync(id);
                if (usuario == null)
                {
                    return NotFound();
                }

                if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(telefone))
                {
                    TempData["Erro"] = "Todos os campos são obrigatórios.";
                    return RedirectToAction("Editar", new { id });
                }

                // Verificar se email já está em uso por outro usuário
                var emailEmUso = await _db.Usuarios.AnyAsync(u => u.Email == email && u.Id != id);
                if (emailEmUso)
                {
                    TempData["Erro"] = "Este email já está em uso por outro usuário.";
                    return RedirectToAction("Editar", new { id });
                }

                usuario.Nome = nome;
                usuario.Email = email;
                usuario.Telefone = telefone;
                usuario.CargoId = cargoId;

                _db.Usuarios.Update(usuario);
                await _db.SaveChangesAsync();

                TempData["Sucesso"] = "Usuário atualizado com sucesso!";
                return RedirectToAction("Usuarios");
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao atualizar usuário: {ex.Message}";
                return RedirectToAction("Editar", new { id });
            }
        }

        // POST /Admin/Excluir/{id} - Exclui um usuário
        [HttpPost("Excluir/{id}")]
        public async Task<IActionResult> Excluir(int id)
        {
            try
            {
                var usuario = await _db.Usuarios.FindAsync(id);
                if (usuario == null)
                {
                    return NotFound();
                }

                _db.Usuarios.Remove(usuario);
                await _db.SaveChangesAsync();

                TempData["Sucesso"] = $"Usuário '{usuario.Nome}' excluído com sucesso!";
                return RedirectToAction("Usuarios");
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao excluir usuário: {ex.Message}";
                return RedirectToAction("Usuarios");
            }
        }
    }
}

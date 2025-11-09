using Microsoft.AspNetCore.Mvc;
using HelpFast_Pim.Data;
using HelpFast_Pim.Models;
using HelpFast_Pim.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HelpFast_Pim.Controllers
{
    [Route("[controller]")]
    public class UsuariosController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IUsuarioService _usuarioService;

        public UsuariosController(AppDbContext db, IUsuarioService usuarioService)
        {
            _db = db;
            _usuarioService = usuarioService;
        }

        // GET /Usuarios/Gerenciar - Lista todos os usuários
        [HttpGet("Gerenciar")]
        public async Task<IActionResult> Gerenciar()
        {
            var usuarios = await _db.Usuarios
                .Include(u => u.Cargo)
                .AsNoTracking()
                .ToListAsync();

            return View(usuarios);
        }

        // GET /Usuarios/CriarTecnico - Tela de criar novo técnico
        [HttpGet("CriarTecnico")]
        public IActionResult CriarTecnico()
        {
            return View();
        }

        // POST /Usuarios/SalvarTecnico - Salva novo técnico
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

                var emailExiste = await _db.Usuarios.AnyAsync(u => u.Email == email);
                if (emailExiste)
                {
                    TempData["Erro"] = "Este email já está cadastrado.";
                    return RedirectToAction("CriarTecnico");
                }

                var usuario = new Usuario
                {
                    Nome = nome,
                    Email = email,
                    Telefone = telefone,
                    Senha = Guid.NewGuid().ToString(),
                    CargoId = 2
                };

                _db.Usuarios.Add(usuario);
                await _db.SaveChangesAsync();

                TempData["Sucesso"] = $"Técnico '{nome}' cadastrado com sucesso!";
                return RedirectToAction("Gerenciar");
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao cadastrar técnico: {ex.Message}";
                return RedirectToAction("CriarTecnico");
            }
        }

        // GET /Usuarios/Editar/{id} - Tela de editar usuário
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

        // POST /Usuarios/Atualizar - Atualiza dados do usuário
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
                return RedirectToAction("Gerenciar");
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao atualizar usuário: {ex.Message}";
                return RedirectToAction("Editar", new { id });
            }
        }

        // POST /Usuarios/Excluir - Exclui um usuário (recebe id via form)
        [HttpPost("Excluir")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir([FromForm] int id)
        {
            try
            {
                var usuario = await _db.Usuarios.FindAsync(id);
                if (usuario == null)
                {
                    TempData["Erro"] = "Usuário não encontrado.";
                    return RedirectToAction("Gerenciar");
                }

                var iaResultsDirectos = await _db.ChatIaResults
                    .Include(r => r.Chat)
                    .Where(r => r.Chat != null && (r.Chat.RemetenteId == id || r.Chat.DestinatarioId == id))
                    .ToListAsync();
                if (iaResultsDirectos.Any())
                {
                    _db.ChatIaResults.RemoveRange(iaResultsDirectos);
                    await _db.SaveChangesAsync();
                }

                var chatsDirectos = await _db.Chats
                    .Where(c => c.RemetenteId == id || c.DestinatarioId == id)
                    .ToListAsync();
                if (chatsDirectos.Any())
                {
                    _db.Chats.RemoveRange(chatsDirectos);
                    await _db.SaveChangesAsync();
                }

                var chamados = await _db.Chamados
                    .Where(c => c.ClienteId == id || c.TecnicoId == id)
                    .ToListAsync();

                var chamadoIds = chamados.Select(c => c.Id).ToList();

                if (chamadoIds.Any())
                {
                    var iaResultsChamados = await _db.ChatIaResults
                        .Include(r => r.Chat)
                        .Where(r => r.Chat != null && r.Chat.ChamadoId.HasValue && chamadoIds.Contains(r.Chat.ChamadoId.Value))
                        .ToListAsync();
                    if (iaResultsChamados.Any())
                    {
                        _db.ChatIaResults.RemoveRange(iaResultsChamados);
                        await _db.SaveChangesAsync();
                    }

                    var chatsChamados = await _db.Chats
                        .Where(c => c.ChamadoId.HasValue && chamadoIds.Contains(c.ChamadoId.Value))
                        .ToListAsync();
                    if (chatsChamados.Any())
                    {
                        _db.Chats.RemoveRange(chatsChamados);
                        await _db.SaveChangesAsync();
                    }

                    var historicos = await _db.HistoricoChamados
                        .Where(h => chamadoIds.Contains(h.ChamadoId))
                        .ToListAsync();
                    if (historicos.Any())
                    {
                        _db.HistoricoChamados.RemoveRange(historicos);
                        await _db.SaveChangesAsync();
                    }

                    _db.Chamados.RemoveRange(chamados);
                    await _db.SaveChangesAsync();
                }

                _db.Usuarios.Remove(usuario);
                await _db.SaveChangesAsync();

                TempData["Sucesso"] = $"Usuário '{usuario.Nome}' excluído com sucesso!";
                return RedirectToAction("Gerenciar");
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao excluir usuário: {ex.Message}";
                return RedirectToAction("Gerenciar");
            }
        }
    }
}

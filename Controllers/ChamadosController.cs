using Microsoft.AspNetCore.Mvc;
using HelpFast_Pim.Models;
using HelpFast_Pim.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace HelpFast_Pim.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class ChamadosController : Controller
    {
        private readonly AppDbContext _context;

        public ChamadosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Chamado model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Motivo))
            {
                ModelState.AddModelError(string.Empty, "Motivo é obrigatório.");
                return View(model);
            }

            // obter id do usuário autenticado (cliente)
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
            {
                return RedirectToAction("Login", "Account");
            }

            model.ClienteId = userId;
            model.Status = "Aberto";
            model.DataAbertura = DateTime.UtcNow;
            model.TecnicoId = null;
            model.DataFechamento = null;

            _context.Chamados.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Faq", "Faqs", new { chamadoId = model.Id, motivo = model.Motivo });
        }

        [HttpGet("Meus")]
        public async Task<IActionResult> Meus()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
                return RedirectToAction("Login", "Account");

            var meus = await _context.Chamados
                .Where(c => c.ClienteId == userId)
                .OrderByDescending(c => c.DataAbertura)
                .ToListAsync();

            return View(meus);
        }

        [HttpGet("MeusChamados")]
        public async Task<IActionResult> MeusChamados()
        {

            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
                return RedirectToAction("Login", "Account");

            var chamados = await _context.Chamados
                .Where(c => c.ClienteId == userId)
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            return View(chamados);
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var chamado = await _context.Chamados.FindAsync(id);
            if (chamado == null) return NotFound();

            return View("Details", chamado);
        }

        // Admin - Listar todos os chamados em aberto
        [HttpGet("ChamadosAdmin")]
        public async Task<IActionResult> ChamadosAdmin()
        {
            var chamados = await _context.Chamados
                .Where(c => c.Status == "Aberto" || c.Status == "Andamento")
                .OrderByDescending(c => c.DataAbertura)
                .ToListAsync();

            return View(chamados);
        }

        // Admin - Visualizar chamado e chat
        [HttpGet("VisualizarChamadoAdmin/{id}")]
        public async Task<IActionResult> VisualizarChamadoAdmin(int id)
        {
            var chamado = await _context.Chamados
                .Include(c => c.Chats)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (chamado == null) return NotFound();

            return View(chamado);
        }

        // Admin - Cancelar chamado
        [HttpPost("CancelarChamado/{id}")]
        public async Task<IActionResult> CancelarChamado(int id)
        {
            var chamado = await _context.Chamados.FindAsync(id);
            if (chamado == null) return NotFound();

            chamado.Status = "Cancelado";
            chamado.DataFechamento = DateTime.UtcNow;
            _context.Update(chamado);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Chamado cancelado com sucesso!";
            return RedirectToAction("ChamadosAdmin");
        }

        // Admin - Finalizar chamado
        [HttpPost("FinalizarChamado/{id}")]
        public async Task<IActionResult> FinalizarChamado(int id)
        {
            var chamado = await _context.Chamados.FindAsync(id);
            if (chamado == null) return NotFound();

            chamado.Status = "Finalizado";
            chamado.DataFechamento = DateTime.UtcNow;
            _context.Update(chamado);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Chamado finalizado com sucesso!";
            return RedirectToAction("ChamadosAdmin");
        }

        // User - Ver chamado finalizado
        [HttpGet("ChamadoFinalizado/{id}")]
        public async Task<IActionResult> ChamadoFinalizado(int id)
        {
            var chamado = await _context.Chamados.FindAsync(id);
            if (chamado == null) return NotFound();

            if (chamado.Status != "Finalizado") return RedirectToAction("MeusChamados");

            return View(chamado);
        }

        // Técnico - Consultar chamados atribuídos
        [HttpGet("ConsultarChamados")]
        public async Task<IActionResult> ConsultarChamados()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
                return RedirectToAction("Login", "Account");

            var chamados = await _context.Chamados
                .Where(c => c.TecnicoId == userId)
                .OrderByDescending(c => c.DataAbertura)
                .ToListAsync();

            return View(chamados);
        }

        // Técnico - Visualizar chamado atribuído
        [HttpGet("VisualizarChamadoTecnico/{id}")]
        public async Task<IActionResult> VisualizarChamadoTecnico(int id)
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
                return RedirectToAction("Login", "Account");

            var chamado = await _context.Chamados
                .Include(c => c.Chats)
                .FirstOrDefaultAsync(c => c.Id == id && c.TecnicoId == userId);

            if (chamado == null) return NotFound();

            return View(chamado);
        }

        // Técnico - Cancelar chamado
        [HttpPost("CancelarChamadoTecnico/{id}")]
        public async Task<IActionResult> CancelarChamadoTecnico(int id)
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
                return RedirectToAction("Login", "Account");

            var chamado = await _context.Chamados.FindAsync(id);
            if (chamado == null || chamado.TecnicoId != userId) return NotFound();

            chamado.Status = "Cancelado";
            chamado.DataFechamento = DateTime.UtcNow;
            _context.Update(chamado);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Chamado cancelado com sucesso!";
            return RedirectToAction("ConsultarChamados");
        }

        // Técnico - Concluir chamado
        [HttpPost("ConcluirChamadoTecnico/{id}")]
        public async Task<IActionResult> ConcluirChamadoTecnico(int id)
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
                return RedirectToAction("Login", "Account");

            var chamado = await _context.Chamados.FindAsync(id);
            if (chamado == null || chamado.TecnicoId != userId) return NotFound();

            chamado.Status = "Finalizado";
            chamado.DataFechamento = DateTime.UtcNow;
            _context.Update(chamado);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Chamado concluído com sucesso!";
            return RedirectToAction("ConsultarChamados");
        }
    }
}

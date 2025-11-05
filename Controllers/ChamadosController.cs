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

            // criar e então redirecionar o usuário para a tela de FAQ
            // passa o id do chamado e motivo via querystring para a FAQ poder encaminhar ao chat
            return RedirectToAction("Faq", "Faqs", new { chamadoId = model.Id, motivo = model.Motivo });
        }

        [HttpGet("Chamados/Meus")]
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

        [HttpGet("Chamados/MeusChamados")]
        public async Task<IActionResult> MeusChamados()
        {
            // requer usuário autenticado e retorna apenas os chamados do cliente
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
                return RedirectToAction("Login", "Account");

            var chamados = await _context.Chamados
                .Where(c => c.ClienteId == userId)
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            return View(chamados);
        }

        public async Task<IActionResult> Details(int id)
        {
            var chamado = await _context.Chamados.FindAsync(id);
            if (chamado == null) return NotFound();

            return View("Details", chamado);
        }
    }
}

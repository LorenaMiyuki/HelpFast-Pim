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
            // Desabilitar cache HTTP
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
                return RedirectToAction("Login", "Account");

            var meus = await _context.Chamados
                .AsNoTracking()
                .Where(c => c.ClienteId == userId 
                         && c.Status != "Finalizado" 
                         && c.Status != "Cancelado")
                .OrderByDescending(c => c.DataAbertura)
                .ToListAsync();

            return View(meus);
        }

        [HttpGet("MeusChamados")]
        public async Task<IActionResult> MeusChamados()
        {
            // Desabilitar cache HTTP
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
                return RedirectToAction("Login", "Account");

            var chamados = await _context.Chamados
                .AsNoTracking()
                .Where(c => c.ClienteId == userId 
                         && c.Status != "Finalizado" 
                         && c.Status != "Cancelado")
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
            // Desabilitar cache HTTP
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            
            var chamados = await _context.Chamados
                .AsNoTracking()
                .Where(c => c.Status == "Aberto" || c.Status == "Andamento")
                .OrderByDescending(c => c.DataAbertura)
                .ToListAsync();

            return View(chamados);
        }

        // Admin - Visualizar chamado e chat
        [HttpGet("VisualizarChamadoAdmin/{id}")]
        public async Task<IActionResult> VisualizarChamadoAdmin(int id)
        {
            // Desabilitar cache HTTP
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            
            var chamado = await _context.Chamados
                .AsNoTracking()
                .Include(c => c.Chats.OrderBy(ch => ch.DataEnvio))
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

        // Técnico - Ver tela de finalização (layout específico)
        [HttpGet("ChamadoFinalizadoTecnico/{id}")]
        public async Task<IActionResult> ChamadoFinalizadoTecnico(int id)
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
                return RedirectToAction("Login", "Account");

            var chamado = await _context.Chamados.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (chamado == null) return NotFound();
            if (chamado.TecnicoId != userId)
            {
                // Admins também podem visualizar
                var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
                var isAdmin = roleClaim == "Administrador";
                if (!isAdmin) return NotFound();
            }
            if (chamado.Status != "Finalizado") return RedirectToAction("ConsultarChamados");

            return View("ChamadoFinalizadoTecnico", chamado);
        }

        // Técnico - Consultar chamados atribuídos
        [HttpGet("ConsultarChamados")]
        public async Task<IActionResult> ConsultarChamados()
        {
            // Desabilitar cache HTTP
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
                return RedirectToAction("Login", "Account");

            // Forçar busca do banco de dados (sem cache do EF)
            var chamados = await _context.Chamados
                .AsNoTracking()
                .Where(c => c.TecnicoId == userId 
                         && c.Status != "Finalizado" 
                         && c.Status != "Cancelado")
                .OrderByDescending(c => c.DataAbertura)
                .ToListAsync();

            return View(chamados);
        }

        // Técnico - Visualizar chamado atribuído
        [HttpGet("VisualizarChamadoTecnico/{id}")]
        public async Task<IActionResult> VisualizarChamadoTecnico(int id)
        {
            // Desabilitar cache HTTP
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
                return RedirectToAction("Login", "Account");

            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            var isAdmin = roleClaim == "Administrador";

            // Admin pode ver qualquer chamado, técnico só vê os atribuídos a ele
            var chamado = await _context.Chamados
                .AsNoTracking()
                .Include(c => c.Chats.OrderBy(ch => ch.DataEnvio))
                .FirstOrDefaultAsync(c => c.Id == id && (isAdmin || c.TecnicoId == userId));

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
            return RedirectToAction("ChamadoFinalizadoTecnico", new { id });
        }

        // Status simples do chamado para polling nos chats
        [HttpGet("Status/{id}")]
        public async Task<IActionResult> GetStatus(int id)
        {
            var chamado = await _context.Chamados.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (chamado == null) return NotFound(new { success = false, error = "Chamado não encontrado" });
            return Ok(new { success = true, status = chamado.Status });
        }
    }
}

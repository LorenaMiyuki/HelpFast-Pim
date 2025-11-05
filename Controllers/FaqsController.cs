using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using HelpFast_Pim.Models;
// Ajuste o using abaixo para apontar ao seu DbContext real (namespace / nome)
using HelpFast_Pim.Data;

namespace HelpFast_Pim.Controllers
{
	// Controller para exibir a view Faq.cshtml
	public class FaqsController : Controller
	{
		private readonly AppDbContext _context; 

		public FaqsController(AppDbContext context)
		{
			_context = context;
		}

		// GET: /Faqs/Faq
		public async Task<IActionResult> Faq(int? chamadoId, string motivo)
		{
			// Certifique-se de que o seu DbContext tem DbSet<Faq> Faqs
			var faqs = await _context.Faqs.OrderBy(f => f.Id).ToListAsync();

			// se for chamado encaminhado do fluxo de criação, passa para a view para link direto ao chat
			if (chamadoId.HasValue)
			{
				var chamado = await _context.Chamados.FindAsync(chamadoId.Value);
				if (chamado != null)
				{
					ViewBag.ChamadoNumericId = chamado.Id;
					ViewBag.Motivo = !string.IsNullOrWhiteSpace(motivo) ? motivo : chamado.Motivo;
				}
			}

			return View("Faq", faqs);
		}

		// opcional: manter Index caso exista navegação para /Faqs
		public async Task<IActionResult> Index()
		{
			var faqs = await _context.Faqs.OrderBy(f => f.Id).ToListAsync();
			return View("Faq", faqs);
		}
	}
}

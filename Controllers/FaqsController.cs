using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using HelpFast_Pim.Data;

namespace HelpFast_Pim.Controllers
{
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
			var faqs = await _context.Faqs.OrderBy(f => f.Id).ToListAsync();

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
		public async Task<IActionResult> Index()
		{
			var faqs = await _context.Faqs.OrderBy(f => f.Id).ToListAsync();
			return View("Faq", faqs);
		}
	}
}

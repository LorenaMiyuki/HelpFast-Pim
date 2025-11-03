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
		public async Task<IActionResult> Faq()
		{
			// Certifique-se de que o seu DbContext tem DbSet<Faq> Faqs
			var faqs = await _context.Faqs.OrderBy(f => f.Id).ToListAsync();
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

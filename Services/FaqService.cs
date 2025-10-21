using Microsoft.EntityFrameworkCore;
using WebAppSuporteIA.Models;
using WebAppSuporteIA.Data;

namespace WebAppSuporteIA.Services
{
    public class FaqService : IFaqService
    {
        private readonly AppDbContext _context;
        public FaqService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Faq>> ListarTodosAsync()
        {
            return await _context.Faqs.ToListAsync();
        }

        public async Task<Faq?> ObterPorIdAsync(int id)
        {
            return await _context.Faqs.FindAsync(id);
        }

        public async Task<Faq> CriarAsync(Faq faq)
        {
            _context.Faqs.Add(faq);
            await _context.SaveChangesAsync();
            return faq;
        }

        public async Task<Faq?> AtualizarAsync(int id, Faq faq)
        {
            if (id != faq.Id) return null;
            _context.Entry(faq).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return faq;
        }

        public async Task<bool> ExcluirAsync(int id)
        {
            var faq = await _context.Faqs.FindAsync(id);
            if (faq == null) return false;
            _context.Faqs.Remove(faq);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
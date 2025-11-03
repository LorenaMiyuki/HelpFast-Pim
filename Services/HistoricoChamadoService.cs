using Microsoft.EntityFrameworkCore;
using HelpFast_Pim.Models;
using HelpFast_Pim.Data;

namespace HelpFast_Pim.Services
{
    public class HistoricoChamadoService : IHistoricoChamadoService
    {
        private readonly AppDbContext _context;
        public HistoricoChamadoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<HistoricoChamado>> ListarTodosAsync()
        {
            return await _context.HistoricoChamados.Include(h => h.Chamado).ToListAsync();
        }

        public async Task<HistoricoChamado?> ObterPorIdAsync(int id)
        {
            return await _context.HistoricoChamados.Include(h => h.Chamado).FirstOrDefaultAsync(h => h.Id == id);
        }

        public async Task<HistoricoChamado> CriarAsync(HistoricoChamado historico)
        {
            historico.Data = DateTime.Now;
            _context.HistoricoChamados.Add(historico);
            await _context.SaveChangesAsync();
            return historico;
        }

        public async Task<HistoricoChamado?> AtualizarAsync(int id, HistoricoChamado historico)
        {
            if (id != historico.Id) return null;
            _context.Entry(historico).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return historico;
        }

        public async Task<bool> ExcluirAsync(int id)
        {
            var historico = await _context.HistoricoChamados.FindAsync(id);
            if (historico == null) return false;
            _context.HistoricoChamados.Remove(historico);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
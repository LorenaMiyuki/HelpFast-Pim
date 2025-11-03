using Microsoft.EntityFrameworkCore;
using HelpFast_Pim.Models;
using HelpFast_Pim.Data;

namespace HelpFast_Pim.Services
{
    public class ChamadoService : IChamadoService
    {
        private readonly AppDbContext _context;
        public ChamadoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Chamado>> ListarTodosAsync()
        {
            return await _context.Chamados.Include(c => c.Cliente).Include(c => c.Tecnico).ToListAsync();
        }

        public async Task<Chamado?> ObterPorIdAsync(int id)
        {
            return await _context.Chamados.Include(c => c.Cliente).Include(c => c.Tecnico).FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Chamado> CriarAsync(Chamado chamado)
        {
            chamado.DataAbertura = DateTime.Now;
            _context.Chamados.Add(chamado);
            await _context.SaveChangesAsync();
            return chamado;
        }

        public async Task<Chamado?> AtualizarAsync(int id, Chamado chamado)
        {
            if (id != chamado.Id) return null;
            _context.Entry(chamado).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return chamado;
        }

        public async Task<bool> ExcluirAsync(int id)
        {
            var chamado = await _context.Chamados.FindAsync(id);
            if (chamado == null) return false;
            _context.Chamados.Remove(chamado);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
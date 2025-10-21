using Microsoft.EntityFrameworkCore;
using WebAppSuporteIA.Models;
using WebAppSuporteIA.Data;

namespace WebAppSuporteIA.Services
{
    public class CargoService : ICargoService
    {
        private readonly AppDbContext _context;
        public CargoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Cargo>> ListarTodosAsync()
        {
            return await _context.Cargos.ToListAsync();
        }

        public async Task<Cargo?> ObterPorIdAsync(int id)
        {
            return await _context.Cargos.FindAsync(id);
        }

        public async Task<Cargo> CriarAsync(Cargo cargo)
        {
            _context.Cargos.Add(cargo);
            await _context.SaveChangesAsync();
            return cargo;
        }

        public async Task<Cargo?> AtualizarAsync(int id, Cargo cargo)
        {
            if (id != cargo.Id) return null;
            _context.Entry(cargo).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return cargo;
        }

        public async Task<bool> ExcluirAsync(int id)
        {
            var cargo = await _context.Cargos.FindAsync(id);
            if (cargo == null) return false;
            _context.Cargos.Remove(cargo);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
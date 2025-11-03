using Microsoft.EntityFrameworkCore;
using HelpFast_Pim.Models;
using HelpFast_Pim.Data;

namespace HelpFast_Pim.Services
{
    public class ChatIaResultService : IChatIaResultService
    {
        private readonly AppDbContext _context;
        public ChatIaResultService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ChatIaResult>> ListarTodosAsync()
        {
            return await _context.ChatIaResults.ToListAsync();
        }

        public async Task<ChatIaResult?> ObterPorIdAsync(int id)
        {
            return await _context.ChatIaResults.FindAsync(id);
        }

        public async Task<ChatIaResult> CriarAsync(ChatIaResult result)
        {
            _context.ChatIaResults.Add(result);
            await _context.SaveChangesAsync();
            return result;
        }

        public async Task<ChatIaResult?> AtualizarAsync(int id, ChatIaResult result)
        {
            if (id != result.Id) return null;
            _context.Entry(result).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return result;
        }

        public async Task<bool> ExcluirAsync(int id)
        {
            var result = await _context.ChatIaResults.FindAsync(id);
            if (result == null) return false;
            _context.ChatIaResults.Remove(result);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
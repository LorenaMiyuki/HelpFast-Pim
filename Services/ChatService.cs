using Microsoft.EntityFrameworkCore;
using HelpFast_Pim.Models;
using HelpFast_Pim.Data;

namespace HelpFast_Pim.Services
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _context;
        public ChatService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Chat>> ListarTodosAsync()
        {
            return await _context.Chats.Include(c => c.Chamado).ToListAsync();
        }

        public async Task<Chat?> ObterPorIdAsync(int id)
        {
            return await _context.Chats.Include(c => c.Chamado).FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Chat> CriarAsync(Chat chat)
        {
            chat.DataEnvio = DateTime.Now;
            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();
            return chat;
        }

        public async Task<Chat?> AtualizarAsync(int id, Chat chat)
        {
            if (id != chat.Id) return null;
            _context.Entry(chat).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return chat;
        }

        public async Task<bool> ExcluirAsync(int id)
        {
            var chat = await _context.Chats.FindAsync(id);
            if (chat == null) return false;
            _context.Chats.Remove(chat);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
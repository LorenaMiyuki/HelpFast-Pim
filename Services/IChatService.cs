using HelpFast_Pim.Models;

namespace HelpFast_Pim.Services;

public interface IChatService
{
    Task<List<Chat>> ListarTodosAsync();
    Task<Chat?> ObterPorIdAsync(int id);
    Task<Chat> CriarAsync(Chat chat);
    Task<Chat?> AtualizarAsync(int id, Chat chat);
    Task<bool> ExcluirAsync(int id);
}
using HelpFast_Pim.Models;

namespace HelpFast_Pim.Services;

public interface IChatIaResultService
{
    Task<List<ChatIaResult>> ListarTodosAsync();
    Task<ChatIaResult?> ObterPorIdAsync(int id);
    Task<ChatIaResult> CriarAsync(ChatIaResult result);
    Task<ChatIaResult?> AtualizarAsync(int id, ChatIaResult result);
    Task<bool> ExcluirAsync(int id);
    Task<bool> ChatExisteAsync(int chatId);
}
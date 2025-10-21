using WebAppSuporteIA.Models;

namespace WebAppSuporteIA.Services;

public interface IChatIaResultService
{
    Task<List<ChatIaResult>> ListarTodosAsync();
    Task<ChatIaResult?> ObterPorIdAsync(int id);
    Task<ChatIaResult> CriarAsync(ChatIaResult result);
    Task<ChatIaResult?> AtualizarAsync(int id, ChatIaResult result);
    Task<bool> ExcluirAsync(int id);
}
using WebAppSuporteIA.Models;

namespace WebAppSuporteIA.Services;

public interface IChamadoService
{
    Task<List<Chamado>> ListarTodosAsync();
    Task<Chamado?> ObterPorIdAsync(int id);
    Task<Chamado> CriarAsync(Chamado chamado);
    Task<Chamado?> AtualizarAsync(int id, Chamado chamado);
    Task<bool> ExcluirAsync(int id);
}
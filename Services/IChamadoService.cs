using HelpFast_Pim.Models;

namespace HelpFast_Pim.Services;

public interface IChamadoService
{
    Task<List<Chamado>> ListarTodosAsync();
    Task<Chamado?> ObterPorIdAsync(int id);
    Task<Chamado> CriarAsync(Chamado chamado);
    Task<Chamado?> AtualizarAsync(int id, Chamado chamado);
    Task<bool> ExcluirAsync(int id);
}
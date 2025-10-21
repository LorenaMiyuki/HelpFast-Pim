using WebAppSuporteIA.Models;

namespace WebAppSuporteIA.Services;

public interface IHistoricoChamadoService
{
    Task<List<HistoricoChamado>> ListarTodosAsync();
    Task<HistoricoChamado?> ObterPorIdAsync(int id);
    Task<HistoricoChamado> CriarAsync(HistoricoChamado historico);
    Task<HistoricoChamado?> AtualizarAsync(int id, HistoricoChamado historico);
    Task<bool> ExcluirAsync(int id);
}
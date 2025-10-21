using WebAppSuporteIA.Models;

namespace WebAppSuporteIA.Services;

public interface ICargoService
{
    Task<List<Cargo>> ListarTodosAsync();
    Task<Cargo?> ObterPorIdAsync(int id);
    Task<Cargo> CriarAsync(Cargo cargo);
    Task<Cargo?> AtualizarAsync(int id, Cargo cargo);
    Task<bool> ExcluirAsync(int id);
}
using WebAppSuporteIA.Models;

namespace WebAppSuporteIA.Services;

public interface IFaqService
{
    Task<List<Faq>> ListarTodosAsync();
    Task<Faq?> ObterPorIdAsync(int id);
    Task<Faq> CriarAsync(Faq faq);
    Task<Faq?> AtualizarAsync(int id, Faq faq);
    Task<bool> ExcluirAsync(int id);
}
using HelpFast_Pim.Models;

namespace HelpFast_Pim.Services;

public interface IFaqService
{
    Task<List<Faq>> ListarTodosAsync();
    Task<Faq?> ObterPorIdAsync(int id);
    Task<Faq> CriarAsync(Faq faq);
    Task<Faq?> AtualizarAsync(int id, Faq faq);
    Task<bool> ExcluirAsync(int id);
}
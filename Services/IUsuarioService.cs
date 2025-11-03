using HelpFast_Pim.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HelpFast_Pim.Services
{
    public interface IUsuarioService
    {
        Task<Usuario?> ObterPorEmailAsync(string email);
        Task<Usuario?> ObterPorIdAsync(int id);
        Task<Usuario> CriarUsuarioAsync(Usuario usuario);
        Task<bool> ValidarLoginAsync(string email, string senha);
        Task AtualizarUltimoLoginAsync(int usuarioId);
        Task<bool> EmailExisteAsync(string email);

        Task<Usuario> CriarClienteAsync(Usuario cliente);
        Task<Usuario> CriarTecnicoAsync(Usuario tecnico, int criadoPorId);
        Task<Usuario> CriarAdministradorAsync(Usuario administrador, int criadoPorId);

        Task<List<Usuario>> ListarUsuariosPorCargoAsync(string cargoNome);
        Task<int> ObterCargoIdPorNomeAsync(string cargoNome);
        Task<List<HistoricoChamado>> ListarHistoricoChamadosAsync();
        Task<List<Usuario>> ListarTecnicosAsync();
        Task<Usuario?> EditarTecnicoAsync(int tecnicoId, Usuario dadosAtualizados);
        Task<bool> ExcluirTecnicoAsync(int tecnicoId);

        // Helpers usados pelo MVC/API
        Task<Usuario?> RegistrarAsync(Usuario user, string senha);

        // Opcional: retorno do login (Usuario sem senha) - alguns flows usam ValidarLoginAsync + ObterPorEmailAsync
        Task<Usuario?> LoginAsync(string email, string senha);
    }
}
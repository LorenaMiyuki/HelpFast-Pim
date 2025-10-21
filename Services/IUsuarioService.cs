using WebAppSuporteIA.Models;

namespace WebAppSuporteIA.Services;

public interface IUsuarioService
{
    Task<Usuario?> ObterPorEmailAsync(string email);
    Task<Usuario?> ObterPorIdAsync(int id);
    Task<Usuario> CriarUsuarioAsync(Usuario usuario);
    Task<bool> ValidarLoginAsync(string email, string senha);
    Task AtualizarUltimoLoginAsync(int usuarioId);
    Task<bool> EmailExisteAsync(string email);
    
    // Métodos específicos por tipo de usuário
    
    // Métodos de listagem por tipo
    
    // Validações de permissão
        Task<Usuario> CriarClienteAsync(Usuario cliente);
        Task<Usuario> CriarTecnicoAsync(Usuario tecnico, int criadoPorId);
        Task<Usuario> CriarAdministradorAsync(Usuario administrador, int criadoPorId);
        Task<List<Usuario>> ListarUsuariosPorCargoAsync(string cargoNome);
        Task<List<HistoricoChamado>> ListarHistoricoChamadosAsync();
        Task<int> ObterCargoIdPorNomeAsync(string cargoNome);
}
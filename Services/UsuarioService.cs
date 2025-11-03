using Microsoft.EntityFrameworkCore;
using HelpFast_Pim.Models;
using HelpFast_Pim.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace HelpFast_Pim.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UsuarioService> _logger;
        private const int FALLBACK_CLIENTE_ID = 3;

        public UsuarioService(AppDbContext context, ILogger<UsuarioService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Usuario?> ObterPorEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email)) return null;

                return await _context.Usuarios
                    .Include(u => u.Cargo)
                    .FirstOrDefaultAsync(u => u.Email == email.Trim());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao obter usuário por email: {Email}", email);
                return null;
            }
        }

        public async Task<Usuario?> ObterPorIdAsync(int id)
        {
            try
            {
                return await _context.Usuarios
                    .Include(u => u.Cargo)
                    .FirstOrDefaultAsync(u => u.Id == id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao obter usuário por id: {Id}", id);
                return null;
            }
        }

        public async Task<Usuario> CriarUsuarioAsync(Usuario usuario)
        {
            try
            {
                if (usuario == null) throw new ArgumentNullException(nameof(usuario));
                if (!string.IsNullOrEmpty(usuario.Senha))
                    usuario.Senha = HashSenha(usuario.Senha);

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();
                return usuario;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao criar usuário: {Email}", usuario?.Email);
                throw;
            }
        }

        public async Task<bool> ValidarLoginAsync(string email, string senha)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha)) return false;
                var usuario = await ObterPorEmailAsync(email.Trim());
                if (usuario == null) return false;
                var senhaHash = HashSenha(senha);
                return usuario.Senha == senhaHash;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao validar login para email: {Email}", email);
                return false;
            }
        }

        public async Task AtualizarUltimoLoginAsync(int usuarioId)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(usuarioId);
                if (usuario != null)
                {
                    usuario.UltimoLogin = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Não foi possível atualizar ultimo login para usuarioId: {Id}", usuarioId);
            }
        }

        public async Task<bool> EmailExisteAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email)) return false;
                return await _context.Usuarios.AnyAsync(u => u.Email == email.Trim());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao checar existência de email: {Email}", email);
                return false;
            }
        }

        public async Task<Usuario> CriarClienteAsync(Usuario cliente)
        {
            cliente.CargoId = await ObterCargoIdPorNomeOuFallbackAsync("Cliente");
            return await CriarUsuarioAsync(cliente);
        }

        public async Task<Usuario> CriarTecnicoAsync(Usuario tecnico, int criadoPorId)
        {
            if (tecnico == null) throw new ArgumentNullException(nameof(tecnico));
            if (string.IsNullOrWhiteSpace(tecnico.Nome) || string.IsNullOrWhiteSpace(tecnico.Email) || string.IsNullOrWhiteSpace(tecnico.Senha))
                throw new ArgumentException("Todos os campos são obrigatórios para registrar um técnico.");

            if (await EmailExisteAsync(tecnico.Email))
                throw new ArgumentException("Já existe um usuário com este e-mail.");

            tecnico.CargoId = await ObterCargoIdPorNomeOuFallbackAsync("Tecnico");
            return await CriarUsuarioAsync(tecnico);
        }

        public async Task<Usuario> CriarAdministradorAsync(Usuario administrador, int criadoPorId)
        {
            if (administrador == null) throw new ArgumentNullException(nameof(administrador));
            administrador.CargoId = await ObterCargoIdPorNomeOuFallbackAsync("Administrador");
            return await CriarUsuarioAsync(administrador);
        }

        public async Task<List<Usuario>> ListarUsuariosPorCargoAsync(string cargoNome)
        {
            if (string.IsNullOrWhiteSpace(cargoNome)) return new List<Usuario>();
            return await _context.Usuarios
                .Include(u => u.Cargo)
                .Where(u => u.Cargo != null && u.Cargo.Nome == cargoNome)
                .OrderBy(u => u.Nome)
                .ToListAsync();
        }

        public async Task<int> ObterCargoIdPorNomeAsync(string cargoNome)
        {
            if (string.IsNullOrWhiteSpace(cargoNome)) throw new ArgumentException("cargoNome é obrigatório.");
            var cargo = await _context.Cargos.FirstOrDefaultAsync(c => c.Nome == cargoNome);
            if (cargo == null) throw new ArgumentException($"Cargo '{cargoNome}' não encontrado.");
            return cargo.Id;
        }

        public async Task<List<HistoricoChamado>> ListarHistoricoChamadosAsync()
        {
            return await _context.HistoricoChamados.ToListAsync();
        }

        public async Task<List<Usuario>> ListarTecnicosAsync()
        {
            return await _context.Usuarios
                .Include(u => u.Cargo)
                .Where(u => u.Cargo != null && u.Cargo.Nome == "Tecnico")
                .OrderBy(u => u.Nome)
                .ToListAsync();
        }

        public async Task<Usuario?> EditarTecnicoAsync(int tecnicoId, Usuario dadosAtualizados)
        {
            var tecnico = await _context.Usuarios.Include(u => u.Cargo).FirstOrDefaultAsync(u => u.Id == tecnicoId);
            if (tecnico == null || tecnico.Cargo?.Nome != "Tecnico") return null;

            tecnico.Nome = dadosAtualizados.Nome;
            tecnico.Email = dadosAtualizados.Email;
            if (!string.IsNullOrWhiteSpace(dadosAtualizados.Senha))
                tecnico.Senha = HashSenha(dadosAtualizados.Senha);
            tecnico.Telefone = dadosAtualizados.Telefone;

            await _context.SaveChangesAsync();
            return tecnico;
        }

        public async Task<bool> ExcluirTecnicoAsync(int tecnicoId)
        {
            var tecnico = await _context.Usuarios.Include(u => u.Cargo).FirstOrDefaultAsync(u => u.Id == tecnicoId);
            if (tecnico == null || tecnico.Cargo?.Nome != "Tecnico") return false;
            _context.Usuarios.Remove(tecnico);
            await _context.SaveChangesAsync();
            return true;
        }

        // Helper resiliente
        private async Task<int> ObterCargoIdPorNomeOuFallbackAsync(string cargoNome)
        {
            try
            {
                var cargo = await _context.Cargos.FirstOrDefaultAsync(c => c.Nome == cargoNome);
                if (cargo != null) return cargo.Id;

                var cliente = await _context.Cargos.FirstOrDefaultAsync(c => c.Nome == "Cliente");
                if (cliente != null) return cliente.Id;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Erro ao obter cargo por nome: {CargoNome}", cargoNome);
            }

            return FALLBACK_CLIENTE_ID;
        }

        // Registrar (usado pelo AccountController)
        public async Task<Usuario?> RegistrarAsync(Usuario user, string senha)
        {
            try
            {
                if (user == null) return null;
                if (string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(senha))
                    throw new ArgumentException("Email e senha são obrigatórios.");

                if (await EmailExisteAsync(user.Email))
                    throw new ArgumentException("Email já cadastrado.");

                if (user.CargoId == 0)
                    user.CargoId = await ObterCargoIdPorNomeOuFallbackAsync("Cliente");

                user.Senha = HashSenha(senha);
                _context.Usuarios.Add(user);
                await _context.SaveChangesAsync();

                user.Senha = null;
                return user;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao registrar usuário: {Email}", user?.Email);
                return null;
            }
        }

        public async Task<Usuario?> LoginAsync(string email, string senha)
        {
            try
            {
                var valido = await ValidarLoginAsync(email, senha);
                if (!valido) return null;

                var usuario = await ObterPorEmailAsync(email.Trim());
                if (usuario == null) return null;

                usuario.Senha = null;
                await AtualizarUltimoLoginAsync(usuario.Id);
                return usuario;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro durante LoginAsync para email: {Email}", email);
                return null;
            }
        }

        private string HashSenha(string senha)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(senha);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
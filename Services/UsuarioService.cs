using Microsoft.EntityFrameworkCore;
using WebAppSuporteIA.Models;
using WebAppSuporteIA.Data;
using System.Security.Cryptography;
using System.Text;

namespace WebAppSuporteIA.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly AppDbContext _context;

        public UsuarioService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Usuario?> ObterPorEmailAsync(string email)
        {
            return await _context.Usuarios.Include(u => u.Cargo)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<Usuario?> ObterPorIdAsync(int id)
        {
            return await _context.Usuarios.Include(u => u.Cargo)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<Usuario> CriarUsuarioAsync(Usuario usuario)
        {
            usuario.Senha = HashSenha(usuario.Senha);
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return usuario;
        }

        public async Task<bool> ValidarLoginAsync(string email, string senha)
        {
            var usuario = await ObterPorEmailAsync(email);
            if (usuario == null)
                return false;
            // Gere o hash da senha informada e compare com o hash salvo
            var senhaHash = HashSenha(senha);
            return usuario.Senha == senhaHash;
        }

        public async Task AtualizarUltimoLoginAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario != null)
            {
                usuario.UltimoLogin = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> EmailExisteAsync(string email)
        {
            return await _context.Usuarios.AnyAsync(u => u.Email == email);
        }

        public async Task<Usuario> CriarClienteAsync(Usuario cliente)
        {
            cliente.CargoId = await ObterCargoIdPorNomeAsync("Cliente");
            return await CriarUsuarioAsync(cliente);
        }

        public async Task<Usuario> CriarTecnicoAsync(Usuario tecnico, int criadoPorId)
        {
            if (string.IsNullOrWhiteSpace(tecnico.Nome) || string.IsNullOrWhiteSpace(tecnico.Email) || string.IsNullOrWhiteSpace(tecnico.Senha))
                throw new ArgumentException("Todos os campos são obrigatórios para registrar um técnico.");

            var emailExistente = await _context.Usuarios.AnyAsync(u => u.Email == tecnico.Email);
            if (emailExistente)
                throw new ArgumentException("Já existe um usuário com este e-mail.");

            tecnico.CargoId = await ObterCargoIdPorNomeAsync("Tecnico");
            return await CriarUsuarioAsync(tecnico);
        }

        public async Task<Usuario> CriarAdministradorAsync(Usuario administrador, int criadoPorId)
        {
            administrador.CargoId = await ObterCargoIdPorNomeAsync("Administrador");
            return await CriarUsuarioAsync(administrador);
        }

        public async Task<List<Usuario>> ListarUsuariosPorCargoAsync(string cargoNome)
        {
            return await _context.Usuarios.Include(u => u.Cargo)
                .Where(u => u.Cargo.Nome == cargoNome)
                .OrderBy(u => u.Nome)
                .ToListAsync();
        }

        public async Task<int> ObterCargoIdPorNomeAsync(string cargoNome)
        {
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
            return await _context.Usuarios.Include(u => u.Cargo)
                .Where(u => u.Cargo.Nome == "Tecnico")
                .OrderBy(u => u.Nome)
                .ToListAsync();
        }

        public async Task<Usuario?> EditarTecnicoAsync(int tecnicoId, Usuario dadosAtualizados)
        {
            var tecnico = await _context.Usuarios.FindAsync(tecnicoId);
            if (tecnico == null || tecnico.Cargo.Nome != "Tecnico") return null;
            tecnico.Nome = dadosAtualizados.Nome;
            tecnico.Email = dadosAtualizados.Email;
            tecnico.Senha = HashSenha(dadosAtualizados.Senha);
            tecnico.Telefone = dadosAtualizados.Telefone;
            await _context.SaveChangesAsync();
            return tecnico;
        }

        public async Task<bool> ExcluirTecnicoAsync(int tecnicoId)
        {
            var tecnico = await _context.Usuarios.FindAsync(tecnicoId);
            if (tecnico == null || tecnico.Cargo.Nome != "Tecnico") return false;
            _context.Usuarios.Remove(tecnico);
            await _context.SaveChangesAsync();
            return true;
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
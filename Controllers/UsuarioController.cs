using Microsoft.AspNetCore.Mvc;
using HelpFast_Pim.Models;
using HelpFast_Pim.Services;

namespace HelpFast_Pim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;
        public UsuarioController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Usuario>> GetById(int id)
        {
            var usuario = await _usuarioService.ObterPorIdAsync(id);
            if (usuario == null) return NotFound();
            return Ok(usuario);
        }

        [HttpGet("email/{email}")]
        public async Task<ActionResult<Usuario>> GetByEmail(string email)
        {
            var usuario = await _usuarioService.ObterPorEmailAsync(email);
            if (usuario == null) return NotFound();
            return Ok(usuario);
        }

        [HttpPost]
        public async Task<ActionResult<Usuario>> Create([FromBody] Usuario usuario)
        {
            var created = await _usuarioService.CriarUsuarioAsync(usuario);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpGet("cargo/{cargoNome}")]
        public async Task<ActionResult<List<Usuario>>> ListarPorCargo(string cargoNome)
        {
            var lista = await _usuarioService.ListarUsuariosPorCargoAsync(cargoNome);
            return Ok(lista);
        }

        [HttpGet("email-existe/{email}")]
        public async Task<ActionResult<bool>> EmailExiste(string email)
        {
            var existe = await _usuarioService.EmailExisteAsync(email);
            return Ok(existe);
        }
    }
}
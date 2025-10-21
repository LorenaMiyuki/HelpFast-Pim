using Microsoft.AspNetCore.Mvc;
using WebAppSuporteIA.Models;
using WebAppSuporteIA.Data;
using WebAppSuporteIA.Services;
using Microsoft.EntityFrameworkCore;

namespace WebAppSuporteIA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _service;
        public ChatController(IChatService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Chat>>> GetAll()
        {
            var lista = await _service.ListarTodosAsync();
            return Ok(lista);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Chat>> GetById(int id)
        {
            var chat = await _service.ObterPorIdAsync(id);
            if (chat == null) return NotFound();
            return Ok(chat);
        }

        [HttpPost]
        public async Task<ActionResult<Chat>> Create([FromBody] Chat chat)
        {
            var criado = await _service.CriarAsync(chat);
            return CreatedAtAction(nameof(GetById), new { id = criado.Id }, criado);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Chat chat)
        {
            var atualizado = await _service.AtualizarAsync(id, chat);
            if (atualizado == null) return BadRequest();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var removido = await _service.ExcluirAsync(id);
            if (!removido) return NotFound();
            return NoContent();
        }
    }
}
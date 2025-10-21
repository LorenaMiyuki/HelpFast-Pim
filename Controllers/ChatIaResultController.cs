using Microsoft.AspNetCore.Mvc;
using WebAppSuporteIA.Models;
using WebAppSuporteIA.Data;
using WebAppSuporteIA.Services;
using Microsoft.EntityFrameworkCore;

namespace WebAppSuporteIA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatIaResultController : ControllerBase
    {
        private readonly IChatIaResultService _service;
        public ChatIaResultController(IChatIaResultService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChatIaResult>>> GetAll()
        {
            var lista = await _service.ListarTodosAsync();
            return Ok(lista);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ChatIaResult>> GetById(int id)
        {
            var result = await _service.ObterPorIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<ChatIaResult>> Create([FromBody] ChatIaResult result)
        {
            var criado = await _service.CriarAsync(result);
            return CreatedAtAction(nameof(GetById), new { id = criado.Id }, criado);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ChatIaResult result)
        {
            var atualizado = await _service.AtualizarAsync(id, result);
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
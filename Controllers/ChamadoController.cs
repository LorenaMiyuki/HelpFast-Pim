using Microsoft.AspNetCore.Mvc;
using HelpFast_Pim.Models;
using HelpFast_Pim.Data;
using HelpFast_Pim.Services;
using Microsoft.EntityFrameworkCore;

namespace HelpFast_Pim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChamadoController : ControllerBase
    {
        private readonly IChamadoService _service;
        public ChamadoController(IChamadoService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Chamado>>> GetAll()
        {
            var lista = await _service.ListarTodosAsync();
            return Ok(lista);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Chamado>> GetById(int id)
        {
            var chamado = await _service.ObterPorIdAsync(id);
            if (chamado == null) return NotFound();
            return Ok(chamado);
        }

        [HttpPost]
        public async Task<ActionResult<Chamado>> Create([FromBody] Chamado chamado)
        {
            var criado = await _service.CriarAsync(chamado);
            return CreatedAtAction(nameof(GetById), new { id = criado.Id }, criado);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Chamado chamado)
        {
            var atualizado = await _service.AtualizarAsync(id, chamado);
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
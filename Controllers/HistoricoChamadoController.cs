using Microsoft.AspNetCore.Mvc;
using HelpFast_Pim.Models;
using HelpFast_Pim.Data;
using Microsoft.EntityFrameworkCore;
using HelpFast_Pim.Services;

namespace HelpFast_Pim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HistoricoChamadoController : ControllerBase
    {
        private readonly IHistoricoChamadoService _service;
        public HistoricoChamadoController(IHistoricoChamadoService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<HistoricoChamado>>> GetAll()
        {
            var lista = await _service.ListarTodosAsync();
            return Ok(lista);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<HistoricoChamado>> GetById(int id)
        {
            var historico = await _service.ObterPorIdAsync(id);
            if (historico == null) return NotFound();
            return Ok(historico);
        }

        [HttpPost]
        public async Task<ActionResult<HistoricoChamado>> Create([FromBody] HistoricoChamado historico)
        {
            var criado = await _service.CriarAsync(historico);
            return CreatedAtAction(nameof(GetById), new { id = criado.Id }, criado);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] HistoricoChamado historico)
        {
            var atualizado = await _service.AtualizarAsync(id, historico);
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
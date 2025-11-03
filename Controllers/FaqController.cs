using Microsoft.AspNetCore.Mvc;
using HelpFast_Pim.Models;
using HelpFast_Pim.Data;
using HelpFast_Pim.Services;
using Microsoft.EntityFrameworkCore;

namespace HelpFast_Pim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FaqController : ControllerBase
    {
        private readonly IFaqService _service;
        public FaqController(IFaqService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Faq>>> GetAll()
        {
            var lista = await _service.ListarTodosAsync();
            return Ok(lista);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Faq>> GetById(int id)
        {
            var faq = await _service.ObterPorIdAsync(id);
            if (faq == null) return NotFound();
            return Ok(faq);
        }

        [HttpPost]
        public async Task<ActionResult<Faq>> Create([FromBody] Faq faq)
        {
            var criado = await _service.CriarAsync(faq);
            return CreatedAtAction(nameof(GetById), new { id = criado.Id }, criado);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Faq faq)
        {
            var atualizado = await _service.AtualizarAsync(id, faq);
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
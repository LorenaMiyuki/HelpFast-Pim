using Microsoft.AspNetCore.Mvc;
using WebAppSuporteIA.Models;
using WebAppSuporteIA.Data;
using WebAppSuporteIA.Services;
using Microsoft.EntityFrameworkCore;

namespace WebAppSuporteIA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CargoController : ControllerBase
    {
        private readonly ICargoService _service;
        public CargoController(ICargoService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cargo>>> GetAll()
        {
            var lista = await _service.ListarTodosAsync();
            return Ok(lista);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Cargo>> GetById(int id)
        {
            var cargo = await _service.ObterPorIdAsync(id);
            if (cargo == null) return NotFound();
            return Ok(cargo);
        }

        [HttpPost]
        public async Task<ActionResult<Cargo>> Create([FromBody] Cargo cargo)
        {
            var criado = await _service.CriarAsync(cargo);
            return CreatedAtAction(nameof(GetById), new { id = criado.Id }, criado);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Cargo cargo)
        {
            var atualizado = await _service.AtualizarAsync(id, cargo);
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
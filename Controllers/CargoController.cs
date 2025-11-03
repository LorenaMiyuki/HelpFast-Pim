using Microsoft.AspNetCore.Mvc;
using HelpFast_Pim.Models;
using HelpFast_Pim.Data;
using HelpFast_Pim.Services;
using Microsoft.EntityFrameworkCore;

namespace HelpFast_Pim.Controllers
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
            // Garantir ordem por Id (usa a ordem persistida do banco)
            var ordenada = lista?.OrderBy(c => c.Id).ToList();
            return Ok(ordenada);
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
            // validar id no payload (se aplicável)
            if (cargo == null) return BadRequest();
            if (cargo.Id != 0 && cargo.Id != id) return BadRequest("Id no corpo diferente do id da rota.");

            // tenta atualizar; o serviço deve retornar null se não encontrou
            var atualizado = await _service.AtualizarAsync(id, cargo);
            if (atualizado == null) return NotFound();
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
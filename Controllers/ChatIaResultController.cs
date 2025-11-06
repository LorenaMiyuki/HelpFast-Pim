using Microsoft.AspNetCore.Mvc;
using HelpFast_Pim.Models;
using HelpFast_Pim.Data;
using HelpFast_Pim.Services;
using Microsoft.EntityFrameworkCore;

namespace HelpFast_Pim.Controllers
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

        // Endpoint para receber webhooks externos (n8n / outros) que contenham o resultado da IA
        // Exemplo de payload esperado (Content-Type: application/json):
        // {
        //   "chatId": 456,
        //   "chamadoId": 123,
        //   "reply": "Resposta resumida para exibição rápida",
        //   "resultJson": { ... }
        // }
        [HttpPost("webhook")]
        public async Task<ActionResult<ChatIaResult>> ReceiveWebhook([FromBody] Models.ChatIaWebhookDto dto)
        {
            if (dto == null) return BadRequest("Payload is null");

            if (dto.ChatId <= 0) return BadRequest("chatId is required and must be greater than zero.");

            // Serializar o objeto ResultJson (se houver) para string. Se não houver, compor um pequeno JSON com reply
            string resultJsonString;
            if (dto.ResultJson.HasValue)
            {
                // Serialize preserving structure
                resultJsonString = System.Text.Json.JsonSerializer.Serialize(dto.ResultJson.Value);
            }
            else
            {
                // criar um JSON simples contendo a reply e metadata
                var simple = new { reply = dto.Reply, chamadoId = dto.ChamadoId };
                resultJsonString = System.Text.Json.JsonSerializer.Serialize(simple);
            }

            // verificar se o chat existe para evitar violação de FK
            var chatExists = await _service.ChatExisteAsync(dto.ChatId);
            if (!chatExists)
            {
                return BadRequest($"Chat with id {dto.ChatId} does not exist.");
            }

            var entity = new ChatIaResult
            {
                ChatId = dto.ChatId,
                ResultJson = resultJsonString,
                CreatedAt = DateTime.UtcNow
            };

            var criado = await _service.CriarAsync(entity);

            return CreatedAtAction(nameof(GetById), new { id = criado.Id }, criado);
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
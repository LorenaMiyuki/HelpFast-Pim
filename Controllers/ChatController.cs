using System;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HelpFast_Pim.Data;
using HelpFast_Pim.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HelpFast_Pim.Controllers
{
    [Route("[controller]")]
    public class ChatController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatController> _logger;

        public ChatController(AppDbContext db, IConfiguration configuration, ILogger<ChatController> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger;
        }

        // GET /Chat/Chat/{id}?motivo=xxx
        [HttpGet("Chat/{id}")]
        public async Task<IActionResult> ChatView(int id, string? motivo)
        {
            // Verificar se o chamado existe e está finalizado
            var chamado = await _db.Chamados.FirstOrDefaultAsync(c => c.Id == id);
            
            if (chamado != null && (chamado.Status == "Finalizado" || chamado.Status == "Cancelado"))
            {
                // Se está finalizado ou cancelado, redirecionar para a view de status
                return RedirectToAction("ChamadoFinalizado", "Chamados", new { id = id });
            }

            ViewBag.ChamadoNumericId = id;
            ViewBag.ChamadoId = id.ToString();
            ViewBag.Motivo = motivo ?? string.Empty;
            ViewBag.InitialBotMessage = string.Empty;
            return View("Chat");
        }

        // POST /Chat/Send - Recebe mensagem do usuário, chama webhook do n8n
        [HttpPost("Send")]
        public async Task<IActionResult> Send([FromBody] JsonElement body)
        {
            try
            {
                string message = body.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String
                    ? msgProp.GetString() ?? ""
                    : "";

                int chamadoId = body.TryGetProperty("chamadoId", out var cidProp) && cidProp.ValueKind == JsonValueKind.Number
                    ? cidProp.GetInt32()
                    : 0;

                string motivo = body.TryGetProperty("motivo", out var motProp) && motProp.ValueKind == JsonValueKind.String
                    ? motProp.GetString() ?? ""
                    : "";

                var userId = await EnsureSystemUserAsync(null);

                // Salvar mensagem do usuário
                var userChat = new Chat
                {
                    ChamadoId = chamadoId > 0 ? chamadoId : (int?)null,
                    Mensagem = message,
                    RemetenteId = userId,
                    DestinatarioId = userId,
                    DataEnvio = DateTime.Now
                };

                _db.Chats.Add(userChat);
                await _db.SaveChangesAsync();

                // Chamar webhook n8n
                var webhookUrl = _configuration.GetValue<string>("Webhook:Url");
                if (string.IsNullOrWhiteSpace(webhookUrl))
                {
                    return StatusCode(500, new { error = "Webhook URL não configurado" });
                }

                try
                {
                    using var client = new HttpClient();
                    var payload = new
                    {
                        message = message,
                        chamadoId = chamadoId,
                        motivo = motivo,
                        chatId = userChat.Id
                    };

                    var json = System.Text.Json.JsonSerializer.Serialize(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(webhookUrl, content);
                    var responseText = await response.Content.ReadAsStringAsync();

                    _logger.LogInformation($"Webhook response: {responseText}");

                    // Retornar que estamos processando - o n8n vai chamar ReceiveAiResponse depois
                    return Ok(new { processing = true, chatId = userChat.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao chamar webhook");
                    return Ok(new { processing = true, chatId = userChat.Id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro em /Chat/Send");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST /Chat/ReceiveAiResponse - Recebe resposta do n8n e salva como mensagem do assistente
        [HttpPost("ReceiveAiResponse")]
        public async Task<IActionResult> ReceiveAiResponse([FromBody] JsonElement body)
        {
            try
            {
                // Extrair dados da resposta do n8n
                string message = "";
                if (body.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
                    message = msgProp.GetString() ?? "";
                if (string.IsNullOrEmpty(message) && body.TryGetProperty("output", out var outProp) && outProp.ValueKind == JsonValueKind.String)
                    message = outProp.GetString() ?? "";
                if (string.IsNullOrEmpty(message) && body.TryGetProperty("text", out var txtProp) && txtProp.ValueKind == JsonValueKind.String)
                    message = txtProp.GetString() ?? "";

                int chatId = body.TryGetProperty("chatId", out var cidProp) && cidProp.ValueKind == JsonValueKind.Number
                    ? cidProp.GetInt32()
                    : 0;

                int chamadoId = body.TryGetProperty("chamadoId", out var chamProp) && chamProp.ValueKind == JsonValueKind.Number
                    ? chamProp.GetInt32()
                    : 0;

                var userId = await EnsureSystemUserAsync(null);

                // Determinar o ChamadoId baseado no chatId original se necessário
                int? finalChamadoId = chamadoId > 0 ? chamadoId : (int?)null;
                if (chatId > 0 && !finalChamadoId.HasValue)
                {
                    var originalChat = await _db.Chats.FindAsync(chatId);
                    if (originalChat != null)
                        finalChamadoId = originalChat.ChamadoId;
                }

                // Salvar resposta do assistente
                var assistantChat = new Chat
                {
                    ChamadoId = finalChamadoId,
                    Mensagem = message,
                    RemetenteId = userId,
                    DestinatarioId = userId,
                    DataEnvio = DateTime.Now
                };

                _db.Chats.Add(assistantChat);
                await _db.SaveChangesAsync();

                // Salvar em ChatIaResult
                var resultJson = System.Text.Json.JsonSerializer.Serialize(body);
                var iaResult = new ChatIaResult
                {
                    ChatId = assistantChat.Id,
                    ResultJson = resultJson,
                    CreatedAt = DateTime.Now
                };

                _db.ChatIaResults.Add(iaResult);
                await _db.SaveChangesAsync();

                return Ok(new { saved = true, assistantChatId = assistantChat.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro em /Chat/ReceiveAiResponse");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET /Chat/AssistantReplyForChat/{chatId} - Frontend poll para pegar resposta do assistente
        [HttpGet("AssistantReplyForChat/{chatId}")]
        public async Task<IActionResult> AssistantReplyForChat(int chatId)
        {
            try
            {
                var userChat = await _db.Chats.FindAsync(chatId);
                if (userChat == null)
                    return Ok(new { found = false });

                // Procurar mensagem mais recente do assistente para o mesmo ChamadoId após esta mensagem
                Chat? assistantChat = null;

                if (userChat.ChamadoId.HasValue)
                {
                    assistantChat = await _db.Chats
                        .AsNoTracking()
                        .Where(c => c.ChamadoId == userChat.ChamadoId 
                                 && c.DataEnvio > userChat.DataEnvio)
                        .OrderByDescending(c => c.DataEnvio)
                        .FirstOrDefaultAsync();
                }

                if (assistantChat == null)
                    return Ok(new { found = false });

                return Ok(new 
                { 
                    found = true, 
                    message = assistantChat.Mensagem, 
                    assistantChatId = assistantChat.Id 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro em /Chat/AssistantReplyForChat");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private async Task<int> EnsureSystemUserAsync(int? preferredUserId)
        {
            if (preferredUserId.HasValue && preferredUserId > 0)
                return preferredUserId.Value;

            // Buscar primeiro usuário
            var existingUser = await _db.Usuarios.FirstOrDefaultAsync();
            if (existingUser != null)
                return existingUser.Id;

            // Criar cargo padrão
            var cargo = new Cargo { Nome = "Sistema" };
            _db.Cargos.Add(cargo);
            await _db.SaveChangesAsync();

            // Criar usuário sistema
            var systemUser = new Usuario
            {
                Nome = "Sistema",
                Email = "system@helpfast",
                Senha = "system123",
                CargoId = cargo.Id,
                Telefone = "0000000000"
            };

            _db.Usuarios.Add(systemUser);
            await _db.SaveChangesAsync();

            return systemUser.Id;
        }
    }
}

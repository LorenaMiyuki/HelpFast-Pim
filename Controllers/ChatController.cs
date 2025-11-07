using System;
using System.Collections.Generic;
using System.Linq;
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
        public IActionResult ChatView(int id, string? motivo)
        {
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
                    DataEnvio = DateTime.Now,
                    Tipo = "Usuario"
                };

                _db.Chats.Add(userChat);
                await _db.SaveChangesAsync();

                // Verificar se o chamado tem um técnico atribuído
                Chamado? chamado = null;
                if (chamadoId > 0)
                {
                    chamado = await _db.Chamados.FindAsync(chamadoId);
                }

                // Se o chamado tem técnico atribuído, enviar mensagem diretamente para ele
                if (chamado != null && chamado.TecnicoId.HasValue)
                {
                    _logger.LogInformation($"Chamado {chamadoId} já tem técnico atribuído ({chamado.TecnicoId}). Mensagem do usuário será visualizada pelo técnico.");
                    return Ok(new { processing = false, chatId = userChat.Id, hasTechnician = true });
                }

                // Chamar webhook n8n (somente se não houver técnico)
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
                _logger.LogInformation($"Recebendo resposta do N8N: {body}");

                // Extrair dados da resposta do n8n - tentar múltiplas propriedades
                string message = "";
                if (body.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
                    message = msgProp.GetString() ?? "";
                if (string.IsNullOrEmpty(message) && body.TryGetProperty("output", out var outProp) && outProp.ValueKind == JsonValueKind.String)
                    message = outProp.GetString() ?? "";
                if (string.IsNullOrEmpty(message) && body.TryGetProperty("text", out var txtProp) && txtProp.ValueKind == JsonValueKind.String)
                    message = txtProp.GetString() ?? "";
                if (string.IsNullOrEmpty(message) && body.TryGetProperty("response", out var respProp) && respProp.ValueKind == JsonValueKind.String)
                    message = respProp.GetString() ?? "";

                // Validar se a mensagem não é vazia
                message = message?.Trim() ?? "";
                
                if (string.IsNullOrWhiteSpace(message))
                {
                    _logger.LogWarning($"Resposta vazia ou nula do N8N recebida");
                    return Ok(new { saved = false, error = "Resposta vazia do workflow" });
                }

                // Filtrar respostas de status/erro do N8N
                var msgLower = message.ToLower();
                if (msgLower.Contains("workflow was started") || 
                    msgLower.Contains("workflow started") ||
                    msgLower.Equals("success") || 
                    msgLower.Equals("ok") ||
                    msgLower.Contains("processing") ||
                    msgLower.Contains("iniciado"))
                {
                    _logger.LogWarning($"Resposta de status do N8N recebida: {message}");
                    return Ok(new { saved = false, error = "Resposta de status, não é mensagem real" });
                }

                // chatId pode vir como número ou string
                int chatId = 0;
                if (body.TryGetProperty("chatId", out var cidProp))
                {
                    if (cidProp.ValueKind == JsonValueKind.Number)
                        chatId = cidProp.GetInt32();
                    else if (cidProp.ValueKind == JsonValueKind.String)
                    {
                        var s = cidProp.GetString();
                        if (!string.IsNullOrWhiteSpace(s) && int.TryParse(s, out var parsed))
                            chatId = parsed;
                    }
                }

                // chamadoId também pode vir como string
                int chamadoId = 0;
                if (body.TryGetProperty("chamadoId", out var chamProp))
                {
                    if (chamProp.ValueKind == JsonValueKind.Number)
                        chamadoId = chamProp.GetInt32();
                    else if (chamProp.ValueKind == JsonValueKind.String)
                    {
                        var s = chamProp.GetString();
                        if (!string.IsNullOrWhiteSpace(s) && int.TryParse(s, out var parsed))
                            chamadoId = parsed;
                    }
                }

                var userId = await EnsureSystemUserAsync(null);

                // Determinar o ChamadoId baseado no chatId original se necessário
                int? finalChamadoId = chamadoId > 0 ? chamadoId : (int?)null;
                if (chatId > 0 && !finalChamadoId.HasValue)
                {
                    var originalChat = await _db.Chats.AsNoTracking().FirstOrDefaultAsync(c => c.Id == chatId);
                    if (originalChat != null)
                        finalChamadoId = originalChat.ChamadoId;
                }

                // Se já existe técnico atribuído ao chamado, não salvar mais respostas da IA
                if (finalChamadoId.HasValue)
                {
                    var chamadoChk = await _db.Chamados.AsNoTracking().FirstOrDefaultAsync(c => c.Id == finalChamadoId.Value);
                    if (chamadoChk != null && chamadoChk.TecnicoId.HasValue)
                    {
                        _logger.LogInformation($"Ignorando resposta do N8N para chamado {finalChamadoId.Value} pois já possui técnico atribuído ({chamadoChk.TecnicoId}).");
                        return Ok(new { saved = false, ignored = true, reason = "hasTechnician" });
                    }
                }

                _logger.LogInformation($"Salvando resposta do assistente - chatId: {chatId}, chamadoId: {finalChamadoId}, mensagem: {message}");

                // Salvar resposta do assistente
                var assistantChat = new Chat
                {
                    ChamadoId = finalChamadoId,
                    Mensagem = message,
                    RemetenteId = userId,
                    DestinatarioId = userId,
                    DataEnvio = DateTime.Now,
                    Tipo = "Assistente"
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

                _logger.LogInformation($"Resposta salva com sucesso - assistantChatId: {assistantChat.Id}");
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
                                 && c.DataEnvio > userChat.DataEnvio
                                 && c.Tipo == "Assistente")
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

        // POST /Chat/AssignToTechnician/{chamadoId} - Atribuir chamado a um técnico aleatório
        [HttpPost("AssignToTechnician/{chamadoId}")]
        public async Task<IActionResult> AssignToTechnician(int chamadoId)
        {
            try
            {
                var chamado = await _db.Chamados.FindAsync(chamadoId);
                if (chamado == null)
                    return NotFound(new { error = "Chamado não encontrado" });

                // Se já estiver atribuído, não reatribua
                if (chamado.TecnicoId.HasValue)
                {
                    var tecnicoExistente = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == chamado.TecnicoId.Value);
                    return Ok(new { success = true, tecnicoId = chamado.TecnicoId, tecnicoNome = tecnicoExistente?.Nome, existing = true });
                }

                // Buscar técnicos disponíveis
                var tecnicos = await _db.Usuarios
                    .Include(u => u.Cargo)
                    .Where(u => u.Cargo != null && (
                        u.Cargo.Nome == "Técnico" ||
                        u.Cargo.Nome == "Tecnico" ||
                        EF.Functions.Like(u.Cargo.Nome, "%tecnico%") ||
                        EF.Functions.Like(u.Cargo.Nome, "%técnico%")
                    ))
                    .ToListAsync();

                if (tecnicos.Count == 0)
                    return BadRequest(new { error = "Nenhum técnico disponível" });

                // Atribuir ao técnico com menos chamados em andamento (fallback: aleatório)
                var carga = await _db.Chamados
                    .AsNoTracking()
                    .Where(c => c.Status == "Andamento" && c.TecnicoId != null)
                    .GroupBy(c => c.TecnicoId!.Value)
                    .Select(g => new { TecnicoId = g.Key, Qtd = g.Count() })
                    .ToListAsync();

                var tecnicoSelecionado = tecnicos
                    .OrderBy(t => carga.FirstOrDefault(c => c.TecnicoId == t.Id)?.Qtd ?? 0)
                    .ThenBy(t => t.Id)
                    .FirstOrDefault() ?? tecnicos.First();

                chamado.TecnicoId = tecnicoSelecionado.Id;
                chamado.Status = "Andamento";
                _db.Chamados.Update(chamado);
                await _db.SaveChangesAsync();

                // Não registrar mensagem automática para evitar duplicidade com a mensagem de transferência do frontend

                return Ok(new 
                { 
                    success = true, 
                    tecnicoId = tecnicoSelecionado.Id,
                    tecnicoNome = tecnicoSelecionado.Nome,
                    status = chamado.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro em /Chat/AssignToTechnician");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET /Chat/History/{chamadoId} - Carregar histórico do chat
        [HttpGet("History/{chamadoId}")]
        public async Task<IActionResult> GetHistory(int chamadoId)
        {
            try
            {
                var messages = await _db.Chats
                    .AsNoTracking()
                    .Where(c => c.ChamadoId == chamadoId)
                    .OrderBy(c => c.DataEnvio)
                    .ToListAsync();

                // Validar e garantir que Tipo está preenchido
                var validMessages = messages.Where(m => !string.IsNullOrEmpty(m.Tipo) && m.Tipo != "undefined").ToList();

                return Ok(new 
                { 
                    success = true, 
                    messages = validMessages.Select(m => new 
                    {
                        m.Id,
                        m.Mensagem,
                        m.Tipo,
                        m.DataEnvio
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro em /Chat/History");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST /Chat/TechnicianSend - Técnico envia mensagem ao cliente
        [HttpPost("TechnicianSend")]
        public async Task<IActionResult> TechnicianSend([FromBody] JsonElement body)
        {
            try
            {
                string message = body.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String
                    ? (msgProp.GetString() ?? "").Trim()
                    : string.Empty;
                int chamadoId = 0;
                if (body.TryGetProperty("chamadoId", out var cidProp))
                {
                    if (cidProp.ValueKind == JsonValueKind.Number)
                        chamadoId = cidProp.GetInt32();
                    else if (cidProp.ValueKind == JsonValueKind.String && int.TryParse(cidProp.GetString(), out var tmp))
                        chamadoId = tmp;
                }

                if (string.IsNullOrWhiteSpace(message) || chamadoId <= 0)
                    return BadRequest(new { error = "Mensagem ou chamadoId inválido" });

                var chamado = await _db.Chamados.FindAsync(chamadoId);
                if (chamado == null)
                    return NotFound(new { error = "Chamado não encontrado" });

                // usuário técnico autenticado
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var tecnicoId))
                    return Unauthorized(new { error = "Usuário não autenticado" });

                var chat = new Chat
                {
                    ChamadoId = chamado.Id,
                    Mensagem = message,
                    RemetenteId = tecnicoId,
                    DestinatarioId = chamado.ClienteId,
                    DataEnvio = DateTime.Now,
                    Tipo = "Tecnico"
                };

                _db.Chats.Add(chat);
                await _db.SaveChangesAsync();

                return Ok(new { success = true, chatId = chat.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro em /Chat/TechnicianSend");
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
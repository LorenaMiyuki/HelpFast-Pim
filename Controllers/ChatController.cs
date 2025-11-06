<<<<<<< HEAD
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
=======
<<<<<<< HEAD
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using HelpFast_Pim.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Collections.Generic;
using HelpFast_Pim.Models;

namespace HelpFast_Pim.Controllers
{
	[Route("Chat")]
	[ApiExplorerSettings(IgnoreApi = true)]
	public class ChatController : Controller
	{
		private readonly AppDbContext _context;

		public ChatController(AppDbContext context)
		{
			_context = context;
		}

		[HttpGet("")]
		[HttpGet("Index")]
		public async Task<IActionResult> Index(int? id, string assunto)
		{
			// tenta carregar o caralho do chamado aqui
			int? chamadoNumericId = null;
			string displayId = "#--";
			string motivoFromDb = null;

			if (id.HasValue)
			{
				var chamado = await _context.Chamados.FirstOrDefaultAsync(c => c.Id == id.Value);
				if (chamado != null)
				{

					var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
					if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId) || chamado.ClienteId != userId)
					{
						return Forbid();
					}

					chamadoNumericId = chamado.Id;
					displayId = $"#{chamado.Id}";
					motivoFromDb = chamado.Motivo;
				}
			}


			ViewBag.ChamadoNumericId = chamadoNumericId;
			ViewBag.ChamadoId = displayId;
			ViewBag.Motivo = !string.IsNullOrWhiteSpace(motivoFromDb) ? motivoFromDb :
				(!string.IsNullOrWhiteSpace(assunto) ? assunto : "Motivo não informado");
			ViewBag.Assunto = ViewBag.Motivo;


			try
			{
				ViewBag.InitialBotMessage = await TriggerStartAndGetReplyAsync(chamadoNumericId, (string)ViewBag.Motivo);
			}
			catch
			{
				ViewBag.InitialBotMessage = null;
			}

			return View("Chat");
		}

		[HttpGet("Chat")]
		public Task<IActionResult> Chat(int? id, string assunto)
		{
			return Index(id, assunto);
		}

		// POST: /Chat/Send
		[HttpPost("Send")]
		public async Task<IActionResult> Send()
		{
			var webhook = "https://n8n.grupoopt.com.br/webhook-test/4bafbef8-1d6a-4c74-b0b3-358ea7f43007";


			var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			int remetenteId = 0;
			if (!string.IsNullOrWhiteSpace(idClaim) && int.TryParse(idClaim, out var uid)) remetenteId = uid;

			if (Request.HasFormContentType)
			{
				var form = await Request.ReadFormAsync();
				var message = form["message"].FirstOrDefault() ?? "";
				var chamadoId = form["chamadoId"].FirstOrDefault();
				var motivo = form["motivo"].FirstOrDefault() ?? form["assunto"].FirstOrDefault();

				int userChatId = 0;
				try
				{
					var c = new Chat { ChamadoId = string.IsNullOrWhiteSpace(chamadoId) ? (int?)null : int.Parse(chamadoId), RemetenteId = remetenteId == 0 ? 1 : remetenteId, DestinatarioId = remetenteId == 0 ? 1 : remetenteId, Mensagem = message ?? string.Empty, DataEnvio = DateTime.UtcNow };
					_context.Chats.Add(c);
					await _context.SaveChangesAsync();
					userChatId = c.Id;
				}
				catch { userChatId = 0; }

				using var multipart = new MultipartFormDataContent();

				multipart.Add(new StringContent(message ?? ""), "message");
				if (!string.IsNullOrEmpty(chamadoId)) multipart.Add(new StringContent(chamadoId), "chamadoId");
				if (!string.IsNullOrEmpty(motivo)) multipart.Add(new StringContent(motivo), "motivo");
				if (userChatId != 0) multipart.Add(new StringContent(userChatId.ToString()), "chatId");


				foreach (var f in form.Files)
				{
					if (f?.Length > 0)
					{
						using var ms = new MemoryStream();
						await f.CopyToAsync(ms);
						ms.Position = 0;
						var fileContent = new ByteArrayContent(ms.ToArray());
						fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(f.ContentType ?? "application/octet-stream");
						multipart.Add(fileContent, "file", f.FileName);
					}
				}

				try
				{

					using var client = new HttpClient();
					var resp = await client.PostAsync(webhook, multipart);
					var respText = await resp.Content.ReadAsStringAsync();
					var ct = resp.Content.Headers.ContentType?.MediaType ?? "";
					if (ct.Contains("json")) return Content(respText, "application/json");
					return Content(respText, "text/plain");
				}
				catch (HttpRequestException ex)
				{
					return StatusCode(502, $"Erro ao contatar webhook: {ex.Message}");
				}
			}
			else
			{
				// vou usar JSON para payload no corpo
				using var reader = new StreamReader(Request.Body, Encoding.UTF8);
				var body = await reader.ReadToEndAsync();

				int? chamadoFromJson = null;
				try
				{
					using var doc = JsonDocument.Parse(body);
					if (doc.RootElement.TryGetProperty("chamadoId", out var ch) && ch.ValueKind == JsonValueKind.Number && ch.TryGetInt32(out var chv)) chamadoFromJson = chv;
				}
				catch { }

				int userChatId2 = 0;
				try
				{
					var c = new Chat { ChamadoId = chamadoFromJson, RemetenteId = remetenteId == 0 ? 1 : remetenteId, DestinatarioId = remetenteId == 0 ? 1 : remetenteId, Mensagem = body ?? string.Empty, DataEnvio = DateTime.UtcNow };
					_context.Chats.Add(c);
					await _context.SaveChangesAsync();
					userChatId2 = c.Id;
				}
				catch { userChatId2 = 0; }

				try
				{
					// insere chatId no payload JSON antes de enviar
					Dictionary<string, object>? map = null;
					try { map = JsonSerializer.Deserialize<Dictionary<string, object>>(body); } catch { map = new Dictionary<string, object>(); }
					if (map == null) map = new Dictionary<string, object>();
					if (userChatId2 != 0) map["chatId"] = userChatId2;
					var toSend = JsonSerializer.Serialize(map);
					using var content = new StringContent(toSend, Encoding.UTF8, "application/json");
					using var client = new HttpClient();
					var resp = await client.PostAsync(webhook, content);
					var respText = await resp.Content.ReadAsStringAsync();
					var ct = resp.Content.Headers.ContentType?.MediaType ?? "";
					if (ct.Contains("json")) return Content(respText, "application/json");
					return Content(respText, "text/plain");
				}
				catch (HttpRequestException ex)
				{
					return StatusCode(502, $"Erro ao contatar webhook: {ex.Message}");
				}
			}
		}

		// Envia evento "start" ao webhook e retorna um texto de resposta (ou null)
		private async Task<string> TriggerStartAndGetReplyAsync(int? chamadoId, string motivo)
		{
			var webhook = "https://n8n.grupoopt.com.br/webhook-test/4bafbef8-1d6a-4c74-b0b3-358ea7f43007";
			try
			{
				using var client = new System.Net.Http.HttpClient { Timeout = System.TimeSpan.FromSeconds(8) };
				var payload = new { @event = "start", chamadoId = chamadoId, motivo = motivo };
				var json = JsonSerializer.Serialize(payload);
				using var content = new StringContent(json, Encoding.UTF8, "application/json");
				using var resp = await client.PostAsync(webhook, content);
				if (!resp.IsSuccessStatusCode) return null;
				var ct = resp.Content.Headers.ContentType?.MediaType ?? "";
				var text = await resp.Content.ReadAsStringAsync();
				if (ct.Contains("json"))
				{
					try
					{
						using var doc = JsonDocument.Parse(text);
						if (doc.RootElement.TryGetProperty("reply", out var r)) return r.GetString();
						if (doc.RootElement.TryGetProperty("message", out var m)) return m.GetString();
						if (doc.RootElement.TryGetProperty("text", out var t)) return t.GetString();
					}
					catch { return text; }
				}
				return string.IsNullOrWhiteSpace(text) ? null : text;
			}
			catch
			{

				return null;
			}
		}

		// Endpoint que recebe a resposta da IA
		[HttpPost("ReceiveAiResponse")]
		public async Task<IActionResult> ReceiveAiResponse([FromBody] JsonElement payload)
		{
			// raw
			var raw = payload.ToString();

			int? chatId = null;
			if (payload.TryGetProperty("chatId", out var p) && p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var cid)) chatId = cid;

			if (!chatId.HasValue && payload.TryGetProperty("chamadoId", out var ch) && ch.ValueKind == JsonValueKind.Number && ch.TryGetInt32(out var chv))
			{
				var last = await _context.Chats.Where(c => c.ChamadoId == chv).OrderByDescending(c => c.Id).FirstOrDefaultAsync();
				if (last != null) chatId = last.Id;
			}

			string resultJson = string.Empty;
			if (payload.TryGetProperty("resultJson", out var rj)) resultJson = rj.ToString() ?? string.Empty;
			else if (payload.TryGetProperty("reply", out var rp) && rp.ValueKind == JsonValueKind.String) resultJson = rp.GetString() ?? string.Empty;
			else if (payload.TryGetProperty("text", out var tx) && tx.ValueKind == JsonValueKind.String) resultJson = tx.GetString() ?? string.Empty;
			else resultJson = raw ?? string.Empty;

			int? chamadoForAssistant = null;
			Chat? userChat = null;
			if (chatId.HasValue)
			{
				userChat = await _context.Chats.FindAsync(chatId.Value);
				if (userChat != null) chamadoForAssistant = userChat.ChamadoId;
			}
			else
			{
				if (payload.TryGetProperty("chamadoId", out var ch2) && ch2.ValueKind == JsonValueKind.Number && ch2.TryGetInt32(out var chv2)) chamadoForAssistant = chv2;
			}

			var assistantSenderId = 1;
			var recipientId = userChat?.RemetenteId ?? assistantSenderId;

			var assistantText = resultJson ?? string.Empty;
			if (assistantText.Length > 500) assistantText = assistantText.Substring(0, 500);

			var assistantChat = new Chat
			{
				ChamadoId = chamadoForAssistant,
				RemetenteId = assistantSenderId,
				DestinatarioId = recipientId,
				Mensagem = assistantText,
				DataEnvio = DateTime.UtcNow
			};
			_context.Chats.Add(assistantChat);
			await _context.SaveChangesAsync();

			var entity = new ChatIaResult { ChatId = assistantChat.Id, ResultJson = resultJson ?? string.Empty, CreatedAt = DateTime.UtcNow };
			_context.ChatIaResults.Add(entity);
			await _context.SaveChangesAsync();

			return Ok(new { saved = true, chatIaResultId = entity.Id, assistantChatId = assistantChat.Id });
		}
	}
}
=======
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
>>>>>>> 62fdcefa2d6fd5eba8359d2833967e5c8be7df22
>>>>>>> cd06833240d736b7c58c15cc4ac4c4332a89314c

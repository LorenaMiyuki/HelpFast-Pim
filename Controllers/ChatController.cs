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

		// Expor /Chat e /Chat/Index
		[HttpGet("")]
		[HttpGet("Index")]
		public async Task<IActionResult> Index(int? id, string assunto)
		{
			// tenta carregar o chamado pelo id, se fornecido
			int? chamadoNumericId = null;
			string displayId = "#--";
			string motivoFromDb = null;

			if (id.HasValue)
			{
				var chamado = await _context.Chamados.FirstOrDefaultAsync(c => c.Id == id.Value);
				if (chamado != null)
				{
					// somente o cliente que criou o chamado pode visualizar/abrir o chat
					var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
					if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId) || chamado.ClienteId != userId)
					{
						// negar acesso para outro cliente
						return Forbid();
					}

					chamadoNumericId = chamado.Id;
					displayId = $"#{chamado.Id}";
					motivoFromDb = chamado.Motivo;
				}
			}

			// prioridade: motivo vindo do banco, senão querystring (param 'assunto' usado como fallback), senão mensagem padrão
			ViewBag.ChamadoNumericId = chamadoNumericId;
			ViewBag.ChamadoId = displayId;
			ViewBag.Motivo = !string.IsNullOrWhiteSpace(motivoFromDb) ? motivoFromDb :
				(!string.IsNullOrWhiteSpace(assunto) ? assunto : "Motivo não informado");
			ViewBag.Assunto = ViewBag.Motivo;

			// inicia atendimento automaticamente chamando o webhook e capturando resposta inicial
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

		// Expor /Chat/Chat para compatibilidade
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

			// tenta identificar usuário autenticado (para salvar mensagem localmente antes de enviar ao fluxo)
			var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			int remetenteId = 0;
			if (!string.IsNullOrWhiteSpace(idClaim) && int.TryParse(idClaim, out var uid)) remetenteId = uid;

			// Se for multipart/form-data com arquivo
			if (Request.HasFormContentType)
			{
				var form = await Request.ReadFormAsync();
				var message = form["message"].FirstOrDefault() ?? "";
				var chamadoId = form["chamadoId"].FirstOrDefault();
				var motivo = form["motivo"].FirstOrDefault() ?? form["assunto"].FirstOrDefault();

				// persiste uma entrada de Chat local representando a mensagem do usuário
				int userChatId = 0;
				try {
					var c = new Chat { ChamadoId = string.IsNullOrWhiteSpace(chamadoId) ? (int?)null : int.Parse(chamadoId), RemetenteId = remetenteId == 0 ? 1 : remetenteId, DestinatarioId = remetenteId == 0 ? 1 : remetenteId, Mensagem = message ?? string.Empty, DataEnvio = DateTime.UtcNow };
					_context.Chats.Add(c);
					await _context.SaveChangesAsync();
					userChatId = c.Id;
				} catch { userChatId = 0; }

				using var multipart = new MultipartFormDataContent();

				// campos simples
				multipart.Add(new StringContent(message ?? ""), "message");
				if (!string.IsNullOrEmpty(chamadoId)) multipart.Add(new StringContent(chamadoId), "chamadoId");
				if (!string.IsNullOrEmpty(motivo)) multipart.Add(new StringContent(motivo), "motivo");
				if (userChatId != 0) multipart.Add(new StringContent(userChatId.ToString()), "chatId");

				// todos os arquivos (se houver) -> envia vários campos "file"
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
					// usa HttpClient local para não depender de IHttpClientFactory
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
				// assume JSON payload no corpo
				using var reader = new StreamReader(Request.Body, Encoding.UTF8);
				var body = await reader.ReadToEndAsync();

				// tenta extrair chamadoId do JSON para salvar o chat
				int? chamadoFromJson = null;
				try {
					using var doc = JsonDocument.Parse(body);
					if (doc.RootElement.TryGetProperty("chamadoId", out var ch) && ch.ValueKind == JsonValueKind.Number && ch.TryGetInt32(out var chv)) chamadoFromJson = chv;
				} catch {}

				int userChatId2 = 0;
				try {
					var c = new Chat { ChamadoId = chamadoFromJson, RemetenteId = remetenteId == 0 ? 1 : remetenteId, DestinatarioId = remetenteId == 0 ? 1 : remetenteId, Mensagem = body ?? string.Empty, DataEnvio = DateTime.UtcNow };
					_context.Chats.Add(c);
					await _context.SaveChangesAsync();
					userChatId2 = c.Id;
				} catch { userChatId2 = 0; }

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
				// falha silenciosa: não impede carregamento da página
				return null;
			}
		}

		// Endpoint que recebe a resposta da IA (usado pelo fluxo n8n)
		[HttpPost("ReceiveAiResponse")]
		public async Task<IActionResult> ReceiveAiResponse([FromBody] JsonElement payload)
		{
			// raw
			var raw = payload.ToString();

			int? chatId = null;
			if (payload.TryGetProperty("chatId", out var p) && p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var cid)) chatId = cid;

			// se veio apenas chamadoId, tenta ligar ao último chat daquele chamado
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

			// Cria um registro de Chat para a mensagem do assistente e persiste o resultado da IA
			// determina chamadoId (prefere do chat existente se disponível)
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

			var assistantSenderId = 1; // id do usuário "sistema"/assistente - ajuste se necessário
			var recipientId = userChat?.RemetenteId ?? assistantSenderId;

			// cria mensagem do assistente no Chats (limita Mensagem a 500 chars conforme modelo Chat)
			// usa o texto já extraído em resultJson como conteúdo do balão do assistente
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

		// GET: /Chat/Results/{chatId} - endpoint de debug para listar resultados IA salvos para um chat
		[HttpGet("Results/{chatId}")]
		public async Task<IActionResult> Results(int chatId)
		{
			var results = await _context.ChatIaResults
				.Where(r => r.ChatId == chatId)
				.OrderByDescending(r => r.CreatedAt)
				.Select(r => new { r.Id, r.ChatId, r.ResultJson, r.CreatedAt })
				.ToListAsync();

			if (results == null || results.Count == 0) return NotFound(new { ok = false, message = "Nenhum resultado encontrado para esse chatId." });
			return Ok(results);
		}
	}
}
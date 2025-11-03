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
			var webhook = "https://n8n.grupoopt.com.br/webhook/c1a4a3ff-2949-46a5-9954-16ee6f95f453/chat";

			// Se for multipart/form-data com arquivo
			if (Request.HasFormContentType)
			{
				var form = await Request.ReadFormAsync();
				var message = form["message"].FirstOrDefault() ?? "";
				var chamadoId = form["chamadoId"].FirstOrDefault();
				var motivo = form["motivo"].FirstOrDefault() ?? form["assunto"].FirstOrDefault();

				using var multipart = new MultipartFormDataContent();

				// campos simples
				multipart.Add(new StringContent(message ?? ""), "message");
				if (!string.IsNullOrEmpty(chamadoId)) multipart.Add(new StringContent(chamadoId), "chamadoId");
				if (!string.IsNullOrEmpty(motivo)) multipart.Add(new StringContent(motivo), "motivo");

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
				try
				{
					using var content = new StringContent(body, Encoding.UTF8, "application/json");
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
			var webhook = "https://n8n.grupoopt.com.br/webhook/c1a4a3ff-2949-46a5-9954-16ee6f95f453/chat";
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
	}
}
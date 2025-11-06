using System.Text.Json;
namespace HelpFast_Pim.Models
{
	// DTO recebido pelo webhook externo (ex: n8n, ferramenta de automações ou qualquer webhook)
	public class ChatIaWebhookDto
	{
		// Id do chat local (deve existir na tabela Chats)
		public int ChatId { get; set; }

		// Opcional: id do chamado relacionado
		public int? ChamadoId { get; set; }

		// Texto resumido / reply que você quer exibir no chat
		public string? Reply { get; set; }

		// Objeto JSON com o resultado completo da IA. Usamos JsonElement para aceitar qualquer JSON
		public JsonElement? ResultJson { get; set; }
	}
}

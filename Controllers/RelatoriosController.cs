using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using HelpFast_Pim.Models;
using HelpFast_Pim.Data;

namespace HelpFast_Pim.Controllers
{
    public class RelatoriosController : Controller
    {
        private readonly AppDbContext _context;

        public RelatoriosController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult ExtrairRelatorio()
        {
            return View();
        }

        [HttpPost]
        public IActionResult GerarRelatorio(string dataSelecionada)
        {
            try
            {
                if (string.IsNullOrEmpty(dataSelecionada))
                {
                    TempData["Erro"] = "Selecione uma data.";
                    return RedirectToAction("ExtrairRelatorio");
                }

                var partes = dataSelecionada.Split('-');
                if (partes.Length != 2 || !int.TryParse(partes[0], out int ano) || !int.TryParse(partes[1], out int mes))
                {
                    TempData["Erro"] = "Data inválida.";
                    return RedirectToAction("ExtrairRelatorio");
                }

                var chamados = _context.Chamados
                    .Include(c => c.Cliente)
                    .Include(c => c.Tecnico)
                    .Where(c => c.DataAbertura.HasValue && c.DataAbertura.Value.Year == ano && c.DataAbertura.Value.Month == mes && c.Status != "Fechado")
                    .OrderBy(c => c.DataAbertura)
                    .ToList();

                if (chamados.Count == 0)
                {
                    TempData["Mensagem"] = "Nenhum chamado encontrado para este mês.";
                    return RedirectToAction("ExtrairRelatorio");
                }

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Chamados");

                    // Cabeçalho
                    worksheet.Cells[1, 1].Value = "ID";
                    worksheet.Cells[1, 2].Value = "Assunto";
                    worksheet.Cells[1, 3].Value = "Motivo";
                    worksheet.Cells[1, 4].Value = "Status";
                    worksheet.Cells[1, 5].Value = "Data Abertura";
                    worksheet.Cells[1, 6].Value = "Cliente";
                    worksheet.Cells[1, 7].Value = "Técnico";

                    var headerRange = worksheet.Cells[1, 1, 1, 7];
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(192, 192, 192));

                    // Dados
                    int row = 2;
                    foreach (var chamado in chamados)
                    {
                        worksheet.Cells[row, 1].Value = chamado.Id;
                        worksheet.Cells[row, 2].Value = chamado.Assunto ?? "";
                        worksheet.Cells[row, 3].Value = chamado.Motivo ?? "";
                        worksheet.Cells[row, 4].Value = chamado.Status ?? "";
                        worksheet.Cells[row, 5].Value = chamado.DataAbertura?.ToString("dd/MM/yyyy HH:mm") ?? "N/A";
                        worksheet.Cells[row, 6].Value = chamado.Cliente?.Nome ?? "N/A";
                        worksheet.Cells[row, 7].Value = chamado.Tecnico?.Nome ?? "N/A";
                        row++;
                    }

                    worksheet.Cells.AutoFitColumns();

                    var fileName = $"Relatorio_Chamados_{mes:00}_{ano}.xlsx";
                    var fileBytes = package.GetAsByteArray();

                    return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao gerar relatório: {ex.Message}";
                return RedirectToAction("ExtrairRelatorio");
            }
        }
    }
}

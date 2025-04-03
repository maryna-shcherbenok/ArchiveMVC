using ArchiveDomain.Model;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace ArchiveInfrastructure.Services
{
    public class DocumentExportService : IExportService<Document>
    {
        private readonly DbarchiveContext _context;

        private static readonly IReadOnlyList<string> HeaderNames =
        new[]
        {
            "Назва",
            "Дата публікації",
            "Мова",
            "Кількість",
            "Інформація",
            "Автори",
            "Категорії",
            "Тип документа",
            "Інвентарний номер",
            "Стан",
            "Доступність"
        };

        public DocumentExportService(DbarchiveContext context)
        {
            _context = context;
        }

        private static void WriteHeader(IXLWorksheet worksheet)
        {
            for (int i = 0; i < HeaderNames.Count; i++)
            {
                worksheet.Cell(1, i + 1).Value = HeaderNames[i];
            }
            worksheet.Row(1).Style.Font.Bold = true;
        }

        private async Task WriteDocumentsAsync(IXLWorksheet worksheet, List<Document> documents, CancellationToken cancellationToken)
        {
            WriteHeader(worksheet);
            int rowIndex = 2;

            foreach (var document in documents)
            {
                var authorNames = await _context.AuthorDocuments
                    .Where(ad => ad.DocumentId == document.Id)
                    .Include(ad => ad.Author)
                    .Select(ad => ad.Author.Name)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                string authors = authorNames.Any() ? string.Join(", ", authorNames) : "—";

                var categoryNames = await _context.CategoryDocuments
                    .Where(cd => cd.DocumentId == document.Id)
                    .Include(cd => cd.Category)
                    .Select(cd => cd.Category.Name)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                string categories = categoryNames.Any() ? string.Join(", ", categoryNames) : "—";

                var instances = await _context.DocumentInstances
                    .Where(di => di.DocumentId == document.Id)
                    .OrderBy(di => di.InventoryNumber)
                    .ToListAsync(cancellationToken);

                string typeName = document.Type?.Name ?? "—";

                if (instances.Any())
                {
                    foreach (var instance in instances)
                    {
                        int columnIndex = 1;

                        worksheet.Cell(rowIndex, columnIndex++).Value = document.Title;
                        worksheet.Cell(rowIndex, columnIndex++).Value = document.PublicationDate;
                        worksheet.Cell(rowIndex, columnIndex++).Value = document.Language;
                        worksheet.Cell(rowIndex, columnIndex++).Value = document.Quantity;
                        worksheet.Cell(rowIndex, columnIndex++).Value = document.Info;
                        worksheet.Cell(rowIndex, columnIndex++).Value = authors;
                        worksheet.Cell(rowIndex, columnIndex++).Value = categories;
                        worksheet.Cell(rowIndex, columnIndex++).Value = typeName;
                        worksheet.Cell(rowIndex, columnIndex++).Value = instance.InventoryNumber;
                        worksheet.Cell(rowIndex, columnIndex++).Value = instance.State;
                        worksheet.Cell(rowIndex, columnIndex++).Value = instance.Available ? "Доступний" : "Недоступний";

                        rowIndex++;
                    }
                }
                else
                {
                    int columnIndex = 1;

                    worksheet.Cell(rowIndex, columnIndex++).Value = document.Title;
                    worksheet.Cell(rowIndex, columnIndex++).Value = document.PublicationDate;
                    worksheet.Cell(rowIndex, columnIndex++).Value = document.Language;
                    worksheet.Cell(rowIndex, columnIndex++).Value = document.Quantity;
                    worksheet.Cell(rowIndex, columnIndex++).Value = document.Info;
                    worksheet.Cell(rowIndex, columnIndex++).Value = authors;
                    worksheet.Cell(rowIndex, columnIndex++).Value = categories;
                    worksheet.Cell(rowIndex, columnIndex++).Value = typeName;
                    worksheet.Cell(rowIndex, columnIndex++).Value = "—";
                    worksheet.Cell(rowIndex, columnIndex++).Value = "—";
                    worksheet.Cell(rowIndex, columnIndex++).Value = "—";

                    rowIndex++;
                }
            }
        }

        private async Task WriteAllAsync(XLWorkbook workbook, CancellationToken cancellationToken)
        {
            var documents = await _context.Documents
                .Include(d => d.Type)
                .AsSplitQuery()
                .ToListAsync(cancellationToken);

            var worksheet = workbook.Worksheets.Add("Документи");
            await WriteDocumentsAsync(worksheet, documents, cancellationToken);
        }

        public async Task WriteToAsync(Stream stream, CancellationToken cancellationToken)
        {
            if (!stream.CanWrite)
                throw new ArgumentException("Stream is not writable", nameof(stream));

            var workbook = new XLWorkbook();
            await WriteAllAsync(workbook, cancellationToken);
            workbook.SaveAs(stream);
        }
    }
}

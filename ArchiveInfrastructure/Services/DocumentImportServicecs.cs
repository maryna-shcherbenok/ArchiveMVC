using ArchiveDomain.Model;
using Author = ArchiveDomain.Model.Author;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace ArchiveInfrastructure.Services
{
    public class DocumentImportService : IImportService<Document>
    {
        private readonly DbarchiveContext _context;

        public DocumentImportService(DbarchiveContext context)
        {
            _context = context;
        }

        public async Task ImportFromStreamAsync(Stream stream, CancellationToken cancellationToken)
        {
            if (!stream.CanRead)
                throw new ArgumentException("Stream нечитабельний", nameof(stream));

            using var workBook = new XLWorkbook(stream);

            foreach (var worksheet in workBook.Worksheets)
            {
                var expectedHeaders = new[]
                {
                    "Назва", "Дата публікації", "Мова", "Інформація", "Автори",
                    "Категорії", "Тип документа", "Інвентарний номер", "Стан", "Доступність"
                };

                var actualHeaders = worksheet.Row(1).Cells(1, expectedHeaders.Length)
                    .Select(c => c.GetString().Trim())
                    .ToArray();

                for (int i = 0; i < expectedHeaders.Length; i++)
                {
                    if (!string.Equals(expectedHeaders[i], actualHeaders[i], StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"Очікувався стовпець \"{expectedHeaders[i]}\" у позиції {i + 1}, але знайдено \"{actualHeaders[i]}\"");
                    }
                }

                foreach (var row in worksheet.RowsUsed().Skip(1))
                {
                    await AddDocumentWithInstanceAsync(row, cancellationToken);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task AddDocumentWithInstanceAsync(IXLRow row, CancellationToken cancellationToken)
        {
            string title = row.Cell(1).GetString();
            string publicationDate = row.Cell(2).GetString();
            string language = row.Cell(3).GetString();
            string info = row.Cell(4).GetString();

            string authorsText = row.Cell(5).GetString();
            string categoriesText = row.Cell(6).GetString();
            string typeName = row.Cell(7).GetString();

            string inventoryNumberText = row.Cell(8).GetFormattedString();
            string state = row.Cell(9).GetString();
            bool available = row.Cell(10).GetString().ToLower().Contains("доступ");

            var type = await _context.DocumentTypes.FirstOrDefaultAsync(t => t.Name == typeName, cancellationToken);
            if (type == null)
            {
                type = new DocumentType { Name = typeName };
                _context.DocumentTypes.Add(type);
                await _context.SaveChangesAsync(cancellationToken);
            }

            string normalizedPublicationDate = string.IsNullOrWhiteSpace(publicationDate) || publicationDate.Trim() == "—" ? null : publicationDate;
            string normalizedInfo = string.IsNullOrWhiteSpace(info) || info.Trim() == "—" ? null : info;

            var existingDocument = await _context.Documents
                .Include(d => d.AuthorDocuments).ThenInclude(ad => ad.Author)
                .Include(d => d.CategoryDocuments).ThenInclude(cd => cd.Category)
                .FirstOrDefaultAsync(d =>
                    d.Title == title &&
                    d.Language == language &&
                    d.TypeId == type.Id &&
                    d.PublicationDate == normalizedPublicationDate,
                    cancellationToken);

            bool isDuplicate = false;
            Document document;

            if (existingDocument != null)
            {
                var existingAuthors = existingDocument.AuthorDocuments.Select(ad => ad.Author.Name).ToList();
                var existingCategories = existingDocument.CategoryDocuments.Select(cd => cd.Category.Name).ToList();

                var inputAuthors = string.IsNullOrWhiteSpace(authorsText) || authorsText.Trim() == "—"
                    ? new List<string>()
                    : authorsText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

                var inputCategories = string.IsNullOrWhiteSpace(categoriesText)
                    ? new List<string>()
                    : categoriesText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

                inputAuthors.Sort();
                existingAuthors.Sort();
                inputCategories.Sort();
                existingCategories.Sort();

                isDuplicate =
                    existingDocument.Title == title &&
                    existingDocument.PublicationDate == normalizedPublicationDate &&
                    existingDocument.Language == language &&
                    existingDocument.TypeId == type.Id &&
                    existingAuthors.SequenceEqual(inputAuthors) &&
                    existingCategories.SequenceEqual(inputCategories);

                document = existingDocument;
            }
            else
            {
                document = new Document
                {
                    Title = title,
                    PublicationDate = normalizedPublicationDate,
                    Language = language,
                    Quantity = 0,
                    Info = normalizedInfo,
                    TypeId = type.Id
                };
                _context.Documents.Add(document);
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Додавання авторів з перевіркою
            if (!string.IsNullOrWhiteSpace(authorsText) && authorsText.Trim() != "—")
            {
                var authors = authorsText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var authorName in authors)
                {
                    var author = await _context.Authors.FirstOrDefaultAsync(a => a.Name == authorName, cancellationToken);
                    if (author == null)
                    {
                        author = new Author { Name = authorName };
                        _context.Authors.Add(author);
                        await _context.SaveChangesAsync(cancellationToken);
                    }

                    bool alreadyLinked = await _context.AuthorDocuments
                        .AnyAsync(ad => ad.AuthorId == author.Id && ad.DocumentId == document.Id, cancellationToken);

                    if (!alreadyLinked)
                    {
                        _context.AuthorDocuments.Add(new AuthorDocument
                        {
                            AuthorId = author.Id,
                            DocumentId = document.Id
                        });
                    }
                }
            }

            // Додавання категорій з перевіркою
            if (!string.IsNullOrWhiteSpace(categoriesText))
            {
                var categories = categoriesText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var categoryName in categories)
                {
                    var category = await _context.Categories.FirstOrDefaultAsync(c => c.Name == categoryName, cancellationToken);
                    if (category == null)
                    {
                        category = new Category { Name = categoryName };
                        _context.Categories.Add(category);
                        await _context.SaveChangesAsync(cancellationToken);
                    }

                    bool alreadyLinked = await _context.CategoryDocuments
                        .AnyAsync(cd => cd.CategoryId == category.Id && cd.DocumentId == document.Id, cancellationToken);

                    if (!alreadyLinked)
                    {
                        _context.CategoryDocuments.Add(new CategoryDocument
                        {
                            CategoryId = category.Id,
                            DocumentId = document.Id
                        });
                    }
                }
            }

            if (!int.TryParse(inventoryNumberText, out int inventoryNumber))
                throw new Exception($"Інвентарний номер некоректний або порожній: '{inventoryNumberText}'");

            bool instanceExists = await _context.DocumentInstances
                .AnyAsync(di => di.InventoryNumber == inventoryNumber, cancellationToken);

            if (instanceExists)
                throw new Exception($"Екземпляр з інвентарним номером '{inventoryNumber}' вже існує у базі даних.");

            var documentInstance = new DocumentInstance
            {
                DocumentId = document.Id,
                InventoryNumber = inventoryNumber,
                State = state,
                Available = available
            };

            _context.DocumentInstances.Add(documentInstance);
            document.Quantity += 1;
        }
    }
}

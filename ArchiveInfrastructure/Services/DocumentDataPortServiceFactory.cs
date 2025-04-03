using ArchiveDomain.Model;
using ArchiveInfrastructure.Services;

namespace ArchiveInfrastructure.Factories
{
    public class DocumentDataPortServiceFactory : IDataPortServiceFactory<Document>
    {
        private readonly DbarchiveContext _context;

        public DocumentDataPortServiceFactory(DbarchiveContext context)
        {
            _context = context;
        }

        public IImportService<Document> GetImportService(string contentType)
        {
            if (contentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                return new DocumentImportService(_context);
            }

            throw new NotImplementedException(
                $"No import service implemented for documents with content type {contentType}"
            );
        }

        public IExportService<Document> GetExportService(string contentType)
        {
            if (contentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                return new DocumentExportService(_context);
            }

            throw new NotImplementedException(
                $"No export service implemented for documents with content type {contentType}"
            );
        }
    }
}

using UserModule.Models.ImportModels;
using DevExtreme.AspNet.Data;

namespace Web.Services.Admin;

public interface IImportDepartmentService
{
    Task<ImportResult> ImportAsync(IFormFile excelFile);
    Task<byte[]> ExportAsync(DataSourceLoadOptions loadOptions, CancellationToken cancellationToken = default);
}

using UserModule.Models.ImportModels;
using DevExtreme.AspNet.Data;

namespace Web.Services.Admin;

public interface IImportUserService
{
    Task<ImportResult> ImportUserAsync(IFormFile excelFile);
    Task<byte[]> ExportAsync(DataSourceLoadOptions loadOptions, CancellationToken cancellationToken = default);
}

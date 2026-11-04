namespace UserModule.Models.ImportModels;

public class ImportResult
{
    public int TotalRows { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public List<ImportRowError> Errors { get; set; } = [];

    public bool HasErrors => Errors.Count > 0;
}

namespace UserModule.Models.ImportModels;

public class ImportRowError
{
    public int Row { get; set; }
    public string Issuer { get; set; } = string.Empty;
    public string SerialNo { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public List<string> Messages { get; set; } = [];
}

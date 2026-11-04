namespace Web.Models
{
    public class BaseDataGridFM
    {
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 20;
        public string DataGridSort { get; set; } = string.Empty;
        public string DataGridFilter { get; set; } = string.Empty;
    }
}

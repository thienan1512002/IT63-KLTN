namespace Web.Models.Enums
{
    public enum ImportAction
    {
        Create,
        Update,
        Delete
    }

    public static class ImportActionHelper
    {
        public static bool TryParse(string raw, out ImportAction action)
        {
            action = raw switch
            {
                "C" => ImportAction.Create,
                "U" => ImportAction.Update,
                "D" => ImportAction.Delete,
                _ => default
            };
            return raw is "C" or "U" or "D";
        }
    }
}
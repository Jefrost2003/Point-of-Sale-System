namespace IT15_INATO_POS.ViewModels
{
    public class ArchivedUserDisplayViewModel
    {
        public int ArchivedId { get; set; }
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty; // displayed role
        public DateTime CreatedDate { get; set; }
        public DateTime ArchivedDate { get; set; }
        public string? ArchivedBy { get; set; }
    }
}
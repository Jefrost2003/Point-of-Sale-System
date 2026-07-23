using IT15_INATO_POS.Models;

namespace IT15_INATO_POS.ViewModels
{
    public class CombinedArchiveViewModel
    {
        public List<Archive> ArchivedProducts { get; set; } = new();
        public List<ArchivedUser> ArchivedUsers { get; set; } = new();
    }
}
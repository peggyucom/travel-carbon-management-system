using StarterM.Models;

namespace StarterM.Services.Interfaces
{
    public interface IFaqService
    {
        Task<List<Faq>> SearchAsync(string? keyword, string? category = null);
        Task<List<Faq>> GetAllActiveAsync();
        Task<Faq?> GetByIdAsync(int id);
        Task<Faq> CreateAsync(Faq faq);
        Task<Faq> UpdateAsync(Faq faq);
        Task<bool> DeleteAsync(int id);
    }
}

using Microsoft.EntityFrameworkCore;
using StarterM.Data;
using StarterM.Models;
using StarterM.Services.Interfaces;

namespace StarterM.Services.Implementations
{
    public class FaqService : IFaqService
    {
        private readonly ApplicationDbContext _db;

        public FaqService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<Faq>> SearchAsync(string? keyword, string? category = null)
        {
            var query = _db.Faqs.Where(f => f.IsActive);

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(f => f.Category == category);

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(f => f.Question.Contains(keyword) || f.Answer.Contains(keyword));

            return await query.ToListAsync();
        }

        public async Task<List<Faq>> GetAllActiveAsync()
        {
            return await _db.Faqs
                .Where(f => f.IsActive)
                .ToListAsync();
        }

        public async Task<Faq?> GetByIdAsync(int id)
        {
            return await _db.Faqs.FindAsync(id);
        }

        public async Task<Faq> CreateAsync(Faq faq)
        {
            _db.Faqs.Add(faq);
            await _db.SaveChangesAsync();
            return faq;
        }

        public async Task<Faq> UpdateAsync(Faq faq)
        {
            var existing = await _db.Faqs.FindAsync(faq.Id);
            if (existing == null) throw new InvalidOperationException("找不到 FAQ");

            existing.Question = faq.Question;
            existing.Answer = faq.Answer;
            existing.Category = faq.Category;
            existing.IsActive = faq.IsActive;
            await _db.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var faq = await _db.Faqs.FindAsync(id);
            if (faq == null) return false;
            _db.Faqs.Remove(faq);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}

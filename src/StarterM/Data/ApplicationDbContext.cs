using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StarterM.Models;

namespace StarterM.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Department> Departments => Set<Department>();
        public DbSet<DailyTrip> DailyTrips => Set<DailyTrip>();
        public DbSet<ExpenseRecord> ExpenseRecords => Set<ExpenseRecord>();
        public DbSet<Application> Applications => Set<Application>();
        public DbSet<ApprovalHistory> ApprovalHistories => Set<ApprovalHistory>();
        public DbSet<ReportSnapshot> ReportSnapshots => Set<ReportSnapshot>();
        public DbSet<CarbonEmissionRecord> CarbonEmissionRecords => Set<CarbonEmissionRecord>();
        public DbSet<EmissionFactor> EmissionFactors => Set<EmissionFactor>();
        public DbSet<Faq> Faqs => Set<Faq>();
        public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
        public DbSet<MealAllowanceHistory> MealAllowanceHistories => Set<MealAllowanceHistory>();
        public DbSet<CarAllowanceHistory> CarAllowanceHistories => Set<CarAllowanceHistory>();
        public DbSet<EmissionFactorHistory> EmissionFactorHistories => Set<EmissionFactorHistory>();
        public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
        public DbSet<ExpenseItem> ExpenseItems => Set<ExpenseItem>();
        public DbSet<VehicleType> VehicleTypes => Set<VehicleType>();
        public DbSet<ExpenseItemVehicleTypeMapping> ExpenseItemVehicleTypeMappings => Set<ExpenseItemVehicleTypeMapping>();
        public DbSet<ApplicationStatus> ApplicationStatuses => Set<ApplicationStatus>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ApplicationUser
            builder.Entity<ApplicationUser>(e =>
            {
                e.HasOne(u => u.Department)
                 .WithMany(d => d.Users)
                 .HasForeignKey(u => u.DepartmentId)
                 .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(u => u.Manager)
                 .WithMany()
                 .HasForeignKey(u => u.ManagerId)
                 .OnDelete(DeleteBehavior.NoAction);

                e.HasIndex(u => u.DepartmentId);
                e.HasIndex(u => u.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
            });

            // ExpenseCategory
            builder.Entity<ExpenseCategory>(e =>
            {
                e.HasIndex(c => c.Code).IsUnique();
            });

            // ExpenseItem
            builder.Entity<ExpenseItem>(e =>
            {
                e.HasOne(i => i.Category)
                 .WithMany(c => c.ExpenseItems)
                 .HasForeignKey(i => i.CategoryId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(i => new { i.CategoryId, i.Code }).IsUnique();
            });

            // VehicleType
            builder.Entity<VehicleType>(e =>
            {
                e.HasIndex(v => v.Code).IsUnique();
            });

            // ExpenseItemVehicleTypeMapping
            builder.Entity<ExpenseItemVehicleTypeMapping>(e =>
            {
                e.HasOne(m => m.ExpenseItem)
                 .WithOne(i => i.VehicleTypeMapping)
                 .HasForeignKey<ExpenseItemVehicleTypeMapping>(m => m.ExpenseItemId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(m => m.VehicleType)
                 .WithMany(v => v.ExpenseItemMappings)
                 .HasForeignKey(m => m.VehicleTypeId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(m => m.ExpenseItemId).IsUnique();
            });

            // ApplicationStatus
            builder.Entity<ApplicationStatus>(e =>
            {
                e.HasIndex(s => s.Code).IsUnique();
            });

            // Application (取代 MonthlyReport)
            builder.Entity<Application>(e =>
            {
                e.HasOne(a => a.Employee)
                 .WithMany(u => u.Applications)
                 .HasForeignKey(a => a.EmployeeId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(a => a.Department)
                 .WithMany()
                 .HasForeignKey(a => a.DepartmentId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(a => a.Status)
                 .WithMany()
                 .HasForeignKey(a => a.StatusId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(a => a.Approver)
                 .WithMany()
                 .HasForeignKey(a => a.ApproverId)
                 .OnDelete(DeleteBehavior.NoAction);

                e.HasIndex(a => a.EmployeeId);
                e.HasIndex(a => a.DepartmentId);
                e.HasIndex(a => a.StatusId);
            });

            // DailyTrip
            builder.Entity<DailyTrip>(e =>
            {
                e.HasOne(d => d.Employee)
                 .WithMany(u => u.DailyTrips)
                 .HasForeignKey(d => d.EmployeeId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(d => d.Department)
                 .WithMany()
                 .HasForeignKey(d => d.DepartmentId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(d => d.Application)
                 .WithMany(a => a.DailyTrips)
                 .HasForeignKey(d => d.ApplicationId)
                 .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(d => d.DepartmentId);
                e.HasIndex(d => new { d.EmployeeId, d.Date });
            });

            // ExpenseRecord
            builder.Entity<ExpenseRecord>(e =>
            {
                e.HasOne(x => x.Employee)
                 .WithMany(u => u.Expenses)
                 .HasForeignKey(x => x.EmployeeId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Department)
                 .WithMany()
                 .HasForeignKey(x => x.DepartmentId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.DailyTrip)
                 .WithMany(d => d.Expenses)
                 .HasForeignKey(x => x.DailyTripId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.ExpenseCategory)
                 .WithMany()
                 .HasForeignKey(x => x.ExpenseCategoryId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.ExpenseItem)
                 .WithMany()
                 .HasForeignKey(x => x.ExpenseItemId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(x => new { x.EmployeeId, x.Date });
                e.HasIndex(x => x.DepartmentId);
                e.HasIndex(x => x.DailyTripId);
                e.HasIndex(x => x.ExpenseCategoryId);
            });

            // ReportSnapshot
            builder.Entity<ReportSnapshot>(e =>
            {
                e.Property(s => s.SnapshotType).HasMaxLength(50);

                e.HasOne(s => s.Application)
                 .WithMany(a => a.Snapshots)
                 .HasForeignKey(s => s.ApplicationId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(s => s.CreatedBy)
                 .WithMany()
                 .HasForeignKey(s => s.CreatedById)
                 .OnDelete(DeleteBehavior.NoAction);

                e.HasIndex(s => new { s.ApplicationId, s.SnapshotType, s.CreatedAt });
            });

            // ApprovalHistory
            builder.Entity<ApprovalHistory>(e =>
            {
                e.HasOne(a => a.Application)
                 .WithMany(app => app.ApprovalHistories)
                 .HasForeignKey(a => a.ApplicationId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(a => a.Actor)
                 .WithMany()
                 .HasForeignKey(a => a.ActorId)
                 .OnDelete(DeleteBehavior.NoAction);
            });

            // CarbonEmissionRecord
            builder.Entity<CarbonEmissionRecord>(e =>
            {
                e.HasOne(c => c.Expense)
                 .WithOne(x => x.CarbonEmission)
                 .HasForeignKey<CarbonEmissionRecord>(c => c.ExpenseId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(c => c.EmissionFactor)
                 .WithMany()
                 .HasForeignKey(c => c.EmissionFactorId)
                 .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(c => c.ExpenseId);
            });

            // SystemConfig
            builder.Entity<SystemConfig>(e =>
            {
                e.HasIndex(c => c.Key).IsUnique();
                e.HasOne(c => c.UpdatedBy)
                 .WithMany()
                 .HasForeignKey(c => c.UpdatedById)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // EmissionFactor
            builder.Entity<EmissionFactor>(e =>
            {
                e.HasOne(f => f.UpdatedBy)
                 .WithMany()
                 .HasForeignKey(f => f.UpdatedById)
                 .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(f => f.VehicleType)
                 .WithMany(v => v.EmissionFactors)
                 .HasForeignKey(f => f.VehicleTypeId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(f => new { f.VehicleTypeId, f.EffectiveFrom });
            });

            // MealAllowanceHistory
            builder.Entity<MealAllowanceHistory>(e =>
            {
                e.Property(h => h.Rate).HasPrecision(18, 2);
                e.HasOne(h => h.UpdatedBy)
                 .WithMany()
                 .HasForeignKey(h => h.UpdatedById)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // CarAllowanceHistory
            builder.Entity<CarAllowanceHistory>(e =>
            {
                e.Property(h => h.RatePerKm).HasPrecision(5, 1);
                e.HasOne(h => h.UpdatedBy)
                 .WithMany()
                 .HasForeignKey(h => h.UpdatedById)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // EmissionFactorHistory
            builder.Entity<EmissionFactorHistory>(e =>
            {
                e.HasOne(h => h.EmissionFactor)
                 .WithMany()
                 .HasForeignKey(h => h.EmissionFactorId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(h => h.UpdatedBy)
                 .WithMany()
                 .HasForeignKey(h => h.UpdatedById)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // === Seed Data ===

            // 費用分類
            builder.Entity<ExpenseCategory>().HasData(
                new ExpenseCategory { Id = 1, Code = "DomesticTransport", Name = "國內交通費", SortOrder = 1, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new ExpenseCategory { Id = 2, Code = "Lodging", Name = "住宿費", SortOrder = 2, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new ExpenseCategory { Id = 3, Code = "MealAllowance", Name = "膳雜費", SortOrder = 3, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new ExpenseCategory { Id = 4, Code = "Other", Name = "其他費用", SortOrder = 4, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            // 費用項目
            builder.Entity<ExpenseItem>().HasData(
                new ExpenseItem { Id = 1, CategoryId = 1, Code = "PersonalCar", Name = "自用車", SortOrder = 1, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new ExpenseItem { Id = 2, CategoryId = 1, Code = "HSR", Name = "高鐵", SortOrder = 2, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new ExpenseItem { Id = 3, CategoryId = 1, Code = "Train", Name = "火車", SortOrder = 3, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new ExpenseItem { Id = 4, CategoryId = 1, Code = "Taxi", Name = "計程車", SortOrder = 4, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new ExpenseItem { Id = 5, CategoryId = 2, Code = "Lodging", Name = "住宿費", SortOrder = 1, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new ExpenseItem { Id = 6, CategoryId = 3, Code = "MealAllowance", Name = "膳雜費", SortOrder = 1, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new ExpenseItem { Id = 7, CategoryId = 4, Code = "Other", Name = "其他費用", SortOrder = 1, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            // 交通工具種類
            builder.Entity<VehicleType>().HasData(
                new VehicleType { Id = 1, Code = "PersonalCar", Name = "自用車", SortOrder = 1, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new VehicleType { Id = 2, Code = "HSR", Name = "高鐵", SortOrder = 2, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new VehicleType { Id = 3, Code = "Train", Name = "火車", SortOrder = 3, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new VehicleType { Id = 4, Code = "Taxi", Name = "計程車", SortOrder = 4, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            // 費用項目與交通工具 mapping
            builder.Entity<ExpenseItemVehicleTypeMapping>().HasData(
                new ExpenseItemVehicleTypeMapping { Id = 1, ExpenseItemId = 1, VehicleTypeId = 1, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new ExpenseItemVehicleTypeMapping { Id = 2, ExpenseItemId = 2, VehicleTypeId = 2, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new ExpenseItemVehicleTypeMapping { Id = 3, ExpenseItemId = 3, VehicleTypeId = 3, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new ExpenseItemVehicleTypeMapping { Id = 4, ExpenseItemId = 4, VehicleTypeId = 4, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            // 申請單狀態
            builder.Entity<ApplicationStatus>().HasData(
                new ApplicationStatus { Id = 1, Code = "Draft", Name = "草稿", SortOrder = 1, IsActive = true },
                new ApplicationStatus { Id = 2, Code = "Submitted", Name = "送出", SortOrder = 2, IsActive = true },
                new ApplicationStatus { Id = 3, Code = "Approved", Name = "核准", SortOrder = 3, IsActive = true },
                new ApplicationStatus { Id = 4, Code = "Rejected", Name = "駁回", SortOrder = 4, IsActive = true },
                new ApplicationStatus { Id = 5, Code = "Voided", Name = "作廢", SortOrder = 5, IsActive = true }
            );

            // Seed EmissionFactors（依交通工具種類維護版本）
            builder.Entity<EmissionFactor>().HasData(
                new EmissionFactor { Id = 1, VehicleTypeId = 1, Co2PerKm = 0.165m, EffectiveFrom = new DateTime(2025, 1, 1), Source = "環保署" },
                new EmissionFactor { Id = 2, VehicleTypeId = 2, Co2PerKm = 0.03m, EffectiveFrom = new DateTime(2025, 1, 1), Source = "台灣高鐵 ESG 報告" },
                new EmissionFactor { Id = 3, VehicleTypeId = 3, Co2PerKm = 0.05m, EffectiveFrom = new DateTime(2025, 1, 1), Source = "台鐵統計" },
                new EmissionFactor { Id = 4, VehicleTypeId = 4, Co2PerKm = 0.18m, EffectiveFrom = new DateTime(2025, 1, 1), Source = "交通部統計" }
            );

            // Seed SystemConfig — 膳雜費預設費率
            builder.Entity<SystemConfig>().HasData(
                new SystemConfig { Id = 1, Key = "MealAllowanceDailyRate", Value = "500", Description = "膳雜費每日費率（元）", UpdatedAt = new DateTime(2025, 1, 1) }
            );

            // Seed FAQ
            builder.Entity<Faq>().HasData(
                new Faq { Id = 1, Question = "報支流程怎麼開始？", Answer = "先建每日差旅與費用，再勾選建立申請單。", Category = "報支流程", IsActive = true },
                new Faq { Id = 2, Question = "系統可報哪些費用？", Answer = "可報交通、住宿、膳雜與其他四類費用。", Category = "費用說明", IsActive = true },
                new Faq { Id = 3, Question = "自用車金額如何計算？", Answer = "依往返公里數乘當日生效每公里費率。", Category = "費用說明", IsActive = true },
                new Faq { Id = 4, Question = "膳雜費如何帶入金額？", Answer = "依差旅日期套用當日生效的膳雜費率。", Category = "費用說明", IsActive = true },
                new Faq { Id = 5, Question = "申請送出前要注意什麼？", Answer = "需已有差旅與費用，並設定部門與主管。", Category = "報支流程", IsActive = true },
                new Faq { Id = 6, Question = "國內膳雜費超標要報薪資嗎？", Answer = "經理級700元、其他600元，超過須列薪資申報。", Category = "費用說明", IsActive = true }
            );
        }
    }
}

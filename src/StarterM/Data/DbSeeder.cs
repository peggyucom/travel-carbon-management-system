using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StarterM.Models;
using StarterM.Models.Enums;
using System.Text.Json;

namespace StarterM.Data
{
    public static class DbSeeder
    {
        private static readonly DateTime BulkSeedStartDate = new(2025, 1, 1);
        private static readonly DateTime BulkSeedEndDate = new(2026, 3, 22);

        private const string DefaultDepartmentName = "資訊科技部";
        private const string ManagerEmail = "manager.demo@travel.com";
        private const string ManagerPassword = "DemoManager123!";
        private const string EmployeePassword = "DemoEmployee123!";
        private const string EmployeeLiEmail = "employee.li.demo@travel.com";
        private const string EmployeeChenEmail = "employee.chen.demo@travel.com";
        private const string EmployeeLinEmail = "employee.lin.demo@travel.com";
        private const string RejectedSnapshotType = "Rejected";

        private enum ApplicationScenario
        {
            Approved,
            Rejected,
            VoidedDraft,
            VoidedRejected,
            Submitted
        }

        private enum TransportMode
        {
            PersonalCar,
            HSR,
            Train,
            Taxi
        }

        private sealed record RatePlan(DateTime EffectiveFrom, decimal Rate);

        private sealed record EmissionPlan(DateTime EffectiveFrom, decimal Co2PerKm, string Source);

        private sealed record DestinationProfile(string City, int BaseDistanceKm, int HsrFare, int TrainFare);

        private sealed record ApplicationSeed(
            ApplicationUser Employee,
            DateTime StartDate,
            int DayCount,
            ApplicationScenario Scenario,
            string Purpose,
            DestinationProfile Destination,
            TransportMode LongDistanceMode);

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var db = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await EnsureRolesAsync(roleManager);

            var defaultDepartment = await EnsureDepartmentAsync(db);
            var manager = await EnsureManagerAsync(userManager, defaultDepartment.Id);

            var employees = new List<ApplicationUser>
            {
                await EnsureEmployeeAsync(userManager, manager, "李員工", EmployeeLiEmail),
                await EnsureEmployeeAsync(userManager, manager, "陳員工", EmployeeChenEmail),
                await EnsureEmployeeAsync(userManager, manager, "林員工", EmployeeLinEmail)
            };

            var mealRatePlans = BuildMealRatePlans();
            var carRatePlans = BuildCarRatePlans();
            var emissionPlans = BuildEmissionPlans();

            await SeedAllowanceHistoriesAsync(db, manager.Id, mealRatePlans, carRatePlans);
            await SeedEmissionFactorVersionsAsync(db, manager.Id, emissionPlans);

            var statusIds = await db.ApplicationStatuses
                .AsNoTracking()
                .ToDictionaryAsync(s => s.Code, s => s.Id);

            var liEmployee = employees.Single(e => string.Equals(e.Email, EmployeeLiEmail, StringComparison.OrdinalIgnoreCase));
            await SeedDemoApplicationAsync(db, liEmployee, manager, defaultDepartment.Id, statusIds, mealRatePlans, carRatePlans, emissionPlans);
            await SeedBulkApplicationsAsync(db, employees, manager, defaultDepartment.Id, statusIds, mealRatePlans, carRatePlans, emissionPlans);
        }

        private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "Employee", "Manager" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static async Task<Department> EnsureDepartmentAsync(ApplicationDbContext db)
        {
            var department = await db.Departments.FirstOrDefaultAsync(d => d.Name == DefaultDepartmentName);
            if (department != null)
            {
                return department;
            }

            department = new Department
            {
                Name = DefaultDepartmentName
            };

            db.Departments.Add(department);
            await db.SaveChangesAsync();
            return department;
        }

        private static async Task<ApplicationUser> EnsureManagerAsync(UserManager<ApplicationUser> userManager, int departmentId)
        {
            var manager = await userManager.FindByEmailAsync(ManagerEmail);
            if (manager == null)
            {
                manager = new ApplicationUser
                {
                    UserName = ManagerEmail,
                    Email = ManagerEmail,
                    Name = "王主管",
                    Role = "Manager",
                    IsActive = true,
                    DepartmentId = departmentId,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(manager, ManagerPassword);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"建立主管帳號失敗: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                manager.UserName = ManagerEmail;
                manager.Email = ManagerEmail;
                manager.Name = "王主管";
                manager.Role = "Manager";
                manager.IsActive = true;
                manager.DepartmentId = departmentId;
                manager.EmailConfirmed = true;
                await userManager.UpdateAsync(manager);
            }

            if (!await userManager.IsInRoleAsync(manager, "Manager"))
            {
                await userManager.AddToRoleAsync(manager, "Manager");
            }

            return manager;
        }

        private static async Task<ApplicationUser> EnsureEmployeeAsync(
            UserManager<ApplicationUser> userManager,
            ApplicationUser manager,
            string name,
            string email)
        {
            var employee = await userManager.FindByEmailAsync(email);
            if (employee == null)
            {
                employee = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    Name = name,
                    Role = "Employee",
                    IsActive = true,
                    ManagerId = manager.Id,
                    DepartmentId = manager.DepartmentId,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(employee, EmployeePassword);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"建立員工帳號失敗: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                employee.UserName = email;
                employee.Email = email;
                employee.Name = name;
                employee.Role = "Employee";
                employee.IsActive = true;
                employee.ManagerId = manager.Id;
                employee.DepartmentId = manager.DepartmentId;
                employee.EmailConfirmed = true;
                await userManager.UpdateAsync(employee);
            }

            if (!await userManager.IsInRoleAsync(employee, "Employee"))
            {
                await userManager.AddToRoleAsync(employee, "Employee");
            }

            return employee;
        }

        private static IReadOnlyList<RatePlan> BuildMealRatePlans()
        {
            var random = new Random(20260322);
            var dates = BuildPlanDates(random, BulkSeedStartDate, BulkSeedEndDate, includeStartDate: true);

            return new List<RatePlan>
            {
                new(dates[0], 320m + (random.Next(0, 4) * 20m)),
                new(dates[1], 340m + (random.Next(0, 4) * 20m)),
                new(dates[2], 300m + (random.Next(0, 5) * 20m))
            }
            .OrderBy(p => p.EffectiveFrom)
            .ToList();
        }

        private static IReadOnlyList<RatePlan> BuildCarRatePlans()
        {
            var random = new Random(20260323);
            var dates = BuildPlanDates(random, BulkSeedStartDate, BulkSeedEndDate, includeStartDate: true);

            return new List<RatePlan>
            {
                new(dates[0], 4.8m + (random.Next(0, 5) * 0.2m)),
                new(dates[1], 5.0m + (random.Next(0, 4) * 0.2m)),
                new(dates[2], 4.9m + (random.Next(0, 5) * 0.2m))
            }
            .OrderBy(p => p.EffectiveFrom)
            .ToList();
        }

        private static Dictionary<TransportMode, IReadOnlyList<EmissionPlan>> BuildEmissionPlans()
        {
            return new Dictionary<TransportMode, IReadOnlyList<EmissionPlan>>
            {
                [TransportMode.PersonalCar] = new List<EmissionPlan>
                {
                    new(new DateTime(2025, 1, 1), 0.162m, "環保署 2025 版"),
                    new(new DateTime(2026, 1, 1), 0.158m, "環保署 2026 版")
                },
                [TransportMode.HSR] = new List<EmissionPlan>
                {
                    new(new DateTime(2025, 1, 1), 0.029m, "台灣高鐵 ESG 報告 2025"),
                    new(new DateTime(2026, 1, 1), 0.027m, "台灣高鐵 ESG 報告 2026")
                },
                [TransportMode.Train] = new List<EmissionPlan>
                {
                    new(new DateTime(2025, 1, 1), 0.047m, "台鐵統計 2025"),
                    new(new DateTime(2026, 1, 1), 0.045m, "台鐵統計 2026")
                },
                [TransportMode.Taxi] = new List<EmissionPlan>
                {
                    new(new DateTime(2025, 1, 1), 0.176m, "交通部統計 2025"),
                    new(new DateTime(2026, 1, 1), 0.171m, "交通部統計 2026")
                }
            };
        }

        private static async Task SeedAllowanceHistoriesAsync(
            ApplicationDbContext db,
            string updatedById,
            IReadOnlyList<RatePlan> mealRatePlans,
            IReadOnlyList<RatePlan> carRatePlans)
        {
            foreach (var plan in mealRatePlans)
            {
                var history = await db.MealAllowanceHistories
                    .FirstOrDefaultAsync(h => h.EffectiveFrom == plan.EffectiveFrom.Date);

                if (history == null)
                {
                    db.MealAllowanceHistories.Add(new MealAllowanceHistory
                    {
                        Rate = plan.Rate,
                        EffectiveFrom = plan.EffectiveFrom.Date,
                        UpdatedAt = ToUtc(plan.EffectiveFrom, 9, 0),
                        UpdatedById = updatedById
                    });
                }
                else
                {
                    history.Rate = plan.Rate;
                    history.UpdatedAt = ToUtc(plan.EffectiveFrom, 9, 0);
                    history.UpdatedById = updatedById;
                }
            }

            foreach (var plan in carRatePlans)
            {
                var history = await db.CarAllowanceHistories
                    .FirstOrDefaultAsync(h => h.EffectiveFrom == plan.EffectiveFrom.Date);

                if (history == null)
                {
                    db.CarAllowanceHistories.Add(new CarAllowanceHistory
                    {
                        RatePerKm = plan.Rate,
                        EffectiveFrom = plan.EffectiveFrom.Date,
                        UpdatedAt = ToUtc(plan.EffectiveFrom, 9, 5),
                        UpdatedById = updatedById
                    });
                }
                else
                {
                    history.RatePerKm = plan.Rate;
                    history.UpdatedAt = ToUtc(plan.EffectiveFrom, 9, 5);
                    history.UpdatedById = updatedById;
                }
            }

            await UpsertSystemConfigAsync(db, "MealAllowanceDailyRate", mealRatePlans.Last().Rate.ToString("F0"), "膳雜費每日費率（元）", updatedById);
            await UpsertSystemConfigAsync(db, "CarAllowancePerKm", carRatePlans.Last().Rate.ToString("F1"), "自用車補助每公里費率（元）", updatedById);

            await db.SaveChangesAsync();
        }

        private static async Task SeedEmissionFactorVersionsAsync(
            ApplicationDbContext db,
            string updatedById,
            IReadOnlyDictionary<TransportMode, IReadOnlyList<EmissionPlan>> emissionPlans)
        {
            foreach (var (mode, plans) in emissionPlans)
            {
                var vehicleTypeId = GetVehicleTypeId(mode);
                var vehicleTypeName = GetVehicleTypeName(mode);
                var factor = await db.EmissionFactors.SingleAsync(f => f.VehicleTypeId == vehicleTypeId);
                var orderedPlans = plans.OrderBy(p => p.EffectiveFrom).ToList();

                foreach (var historicalPlan in orderedPlans.Take(orderedPlans.Count - 1))
                {
                    var history = await db.EmissionFactorHistories
                        .FirstOrDefaultAsync(h => h.EmissionFactorId == factor.Id
                            && h.EffectiveFrom == historicalPlan.EffectiveFrom.Date);

                    if (history == null)
                    {
                        db.EmissionFactorHistories.Add(new EmissionFactorHistory
                        {
                            EmissionFactorId = factor.Id,
                            VehicleType = vehicleTypeName,
                            Co2PerKm = historicalPlan.Co2PerKm,
                            EffectiveFrom = historicalPlan.EffectiveFrom.Date,
                            Source = historicalPlan.Source,
                            UpdatedAt = ToUtc(historicalPlan.EffectiveFrom, 9, 10),
                            UpdatedById = updatedById
                        });
                    }
                    else
                    {
                        history.VehicleType = vehicleTypeName;
                        history.Co2PerKm = historicalPlan.Co2PerKm;
                        history.Source = historicalPlan.Source;
                        history.UpdatedAt = ToUtc(historicalPlan.EffectiveFrom, 9, 10);
                        history.UpdatedById = updatedById;
                    }
                }

                var currentPlan = orderedPlans.Last();
                factor.Co2PerKm = currentPlan.Co2PerKm;
                factor.EffectiveFrom = currentPlan.EffectiveFrom.Date;
                factor.Source = currentPlan.Source;
                factor.UpdatedAt = ToUtc(currentPlan.EffectiveFrom, 9, 15);
                factor.UpdatedById = updatedById;
            }

            await db.SaveChangesAsync();
        }

        private static async Task SeedDemoApplicationAsync(
            ApplicationDbContext db,
            ApplicationUser employee,
            ApplicationUser manager,
            int departmentId,
            IReadOnlyDictionary<string, int> statusIds,
            IReadOnlyList<RatePlan> mealRatePlans,
            IReadOnlyList<RatePlan> carRatePlans,
            IReadOnlyDictionary<TransportMode, IReadOnlyList<EmissionPlan>> emissionPlans)
        {
            var demoTripDate = new DateTime(2026, 3, 18);
            const string demoTripReason = "Demo 客戶拜訪與簡報";

            var demoExists = await db.Applications
                .Include(a => a.DailyTrips)
                .AnyAsync(a => a.EmployeeId == employee.Id
                    && a.DailyTrips.Any(d => d.Date == demoTripDate && d.TripReason == demoTripReason));

            if (demoExists)
            {
                return;
            }

            var carRate = ResolveRate(carRatePlans, demoTripDate);
            var mealRate = ResolveRate(mealRatePlans, demoTripDate);
            var emissionPlan = ResolveEmissionPlan(emissionPlans, TransportMode.PersonalCar, demoTripDate);

            var application = new Application
            {
                EmployeeId = employee.Id,
                DepartmentId = departmentId,
                StatusId = statusIds["Approved"],
                SubmittedAt = ToUtc(demoTripDate.AddDays(1), 9, 0),
                ApprovedAt = ToUtc(demoTripDate.AddDays(1), 15, 0),
                ApproverId = manager.Id,
                CreatedAt = ToUtc(demoTripDate, 8, 30)
            };

            db.Applications.Add(application);
            await db.SaveChangesAsync();

            var dailyTrip = new DailyTrip
            {
                EmployeeId = employee.Id,
                DepartmentId = departmentId,
                ApplicationId = application.Id,
                Date = demoTripDate,
                TripReason = demoTripReason,
                CreatedAt = ToUtc(demoTripDate, 8, 40),
                UpdatedAt = ToUtc(demoTripDate, 8, 40)
            };

            db.DailyTrips.Add(dailyTrip);
            await db.SaveChangesAsync();

            var transportExpense = new ExpenseRecord
            {
                EmployeeId = employee.Id,
                DepartmentId = departmentId,
                DailyTripId = dailyTrip.Id,
                Date = demoTripDate,
                ExpenseCategoryId = 1,
                ExpenseItemId = 1,
                Amount = (int)Math.Ceiling(120m * carRate),
                DistanceKm = 120m,
                Description = "拜訪新竹客戶，使用自用車往返",
                Origin = "台北",
                Destination = "新竹",
                IsRoundTrip = true,
                CreatedAt = ToUtc(demoTripDate, 9, 0),
                UpdatedAt = ToUtc(demoTripDate, 9, 0)
            };

            var mealExpense = new ExpenseRecord
            {
                EmployeeId = employee.Id,
                DepartmentId = departmentId,
                DailyTripId = dailyTrip.Id,
                Date = demoTripDate,
                ExpenseCategoryId = 3,
                ExpenseItemId = 6,
                Amount = (int)mealRate,
                Description = "外勤當日膳雜費",
                IsRoundTrip = true,
                CreatedAt = ToUtc(demoTripDate, 9, 10),
                UpdatedAt = ToUtc(demoTripDate, 9, 10)
            };

            db.ExpenseRecords.AddRange(transportExpense, mealExpense);
            await db.SaveChangesAsync();

            db.CarbonEmissionRecords.Add(new CarbonEmissionRecord
            {
                ExpenseId = transportExpense.Id,
                VehicleType = GetVehicleTypeName(TransportMode.PersonalCar),
                DistanceKm = 120m,
                Co2PerKm = emissionPlan.Co2PerKm,
                TotalCo2 = decimal.Round(120m * emissionPlan.Co2PerKm, 4),
                EmissionFactorId = 1
            });

            db.ApprovalHistories.AddRange(
                new ApprovalHistory
                {
                    ApplicationId = application.Id,
                    Action = ApprovalAction.Submit,
                    ActorId = employee.Id,
                    CreatedAt = application.SubmittedAt ?? ToUtc(demoTripDate.AddDays(1), 9, 0)
                },
                new ApprovalHistory
                {
                    ApplicationId = application.Id,
                    Action = ApprovalAction.Approve,
                    ActorId = manager.Id,
                    CreatedAt = application.ApprovedAt ?? ToUtc(demoTripDate.AddDays(1), 15, 0)
                });

            await db.SaveChangesAsync();
        }

        private static async Task SeedBulkApplicationsAsync(
            ApplicationDbContext db,
            IReadOnlyList<ApplicationUser> employees,
            ApplicationUser manager,
            int departmentId,
            IReadOnlyDictionary<string, int> statusIds,
            IReadOnlyList<RatePlan> mealRatePlans,
            IReadOnlyList<RatePlan> carRatePlans,
            IReadOnlyDictionary<TransportMode, IReadOnlyList<EmissionPlan>> emissionPlans)
        {
            var seeds = BuildApplicationSeeds(employees);

            foreach (var seed in seeds)
            {
                await SeedApplicationAsync(db, seed, manager, departmentId, statusIds, mealRatePlans, carRatePlans, emissionPlans);
            }
        }

        private static List<ApplicationSeed> BuildApplicationSeeds(IReadOnlyList<ApplicationUser> employees)
        {
            var employeeMap = employees.ToDictionary(e => e.Email!, e => e, StringComparer.OrdinalIgnoreCase);
            var destinations = BuildDestinationProfiles();
            var purposes = new[]
            {
                "客戶需求訪談",
                "專案驗收會議",
                "系統教育訓練",
                "供應商設備測試",
                "現場問題排除",
                "年度巡檢",
                "方案簡報"
            };

            var scenarioOverrides = new Dictionary<string, ApplicationScenario>(StringComparer.OrdinalIgnoreCase)
            {
                [$"{EmployeeChenEmail}|2025-04"] = ApplicationScenario.VoidedDraft,
                [$"{EmployeeLiEmail}|2025-11"] = ApplicationScenario.VoidedRejected,
                [$"{EmployeeLiEmail}|2026-01"] = ApplicationScenario.Rejected,
                [$"{EmployeeChenEmail}|2026-02"] = ApplicationScenario.Rejected,
                [$"{EmployeeChenEmail}|2026-03"] = ApplicationScenario.Submitted
            };

            var employeeSchedules = new Dictionary<string, IReadOnlyList<(int Year, int Month)>>(StringComparer.OrdinalIgnoreCase)
            {
                [EmployeeLiEmail] = new List<(int, int)>
                {
                    (2025, 1), (2025, 3), (2025, 5), (2025, 7), (2025, 9), (2025, 11), (2026, 1)
                },
                [EmployeeChenEmail] = new List<(int, int)>
                {
                    (2025, 2), (2025, 4), (2025, 6), (2025, 8), (2025, 10), (2025, 12), (2026, 2), (2026, 3)
                },
                [EmployeeLinEmail] = new List<(int, int)>
                {
                    (2025, 1), (2025, 3), (2025, 5), (2025, 7), (2025, 9), (2025, 11), (2026, 1), (2026, 3)
                }
            };

            var seeds = new List<ApplicationSeed>();
            var employeeOffsets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                [EmployeeLiEmail] = 0,
                [EmployeeChenEmail] = 1,
                [EmployeeLinEmail] = 2
            };

            foreach (var (email, schedule) in employeeSchedules)
            {
                var employee = employeeMap[email];
                var offset = employeeOffsets[email];

                for (var i = 0; i < schedule.Count; i++)
                {
                    var (year, month) = schedule[i];
                    var startDate = BuildStartDate(year, month, offset, i);
                    var dayCount = 1 + ((i + offset) % 3);
                    var destination = destinations[(i + offset) % destinations.Count];
                    var purpose = purposes[(i + offset) % purposes.Length];
                    var longDistanceMode = (TransportMode)((i + offset) % 3);
                    var scenarioKey = $"{email}|{year:D4}-{month:D2}";
                    var scenario = scenarioOverrides.TryGetValue(scenarioKey, out var overriddenScenario)
                        ? overriddenScenario
                        : ApplicationScenario.Approved;

                    seeds.Add(new ApplicationSeed(
                        employee,
                        startDate,
                        dayCount,
                        scenario,
                        purpose,
                        destination,
                        longDistanceMode));
                }
            }

            return seeds
                .OrderBy(s => s.StartDate)
                .ThenBy(s => s.Employee.Name)
                .ToList();
        }

        private static async Task SeedApplicationAsync(
            ApplicationDbContext db,
            ApplicationSeed seed,
            ApplicationUser manager,
            int departmentId,
            IReadOnlyDictionary<string, int> statusIds,
            IReadOnlyList<RatePlan> mealRatePlans,
            IReadOnlyList<RatePlan> carRatePlans,
            IReadOnlyDictionary<TransportMode, IReadOnlyList<EmissionPlan>> emissionPlans)
        {
            var tripReason = $"{seed.Destination.City}{seed.Purpose}";

            var exists = await db.Applications
                .Include(a => a.DailyTrips)
                .AnyAsync(a => a.EmployeeId == seed.Employee.Id
                    && a.DailyTrips.Any(d => d.Date == seed.StartDate.Date && d.TripReason == tripReason));

            if (exists)
            {
                return;
            }

            var submittedAt = ToUtc(seed.StartDate.AddDays(seed.DayCount), 9, 0);
            var approvedAt = submittedAt.AddHours(5);
            var rejectedAt = submittedAt.AddHours(6);
            var voidedAt = seed.Scenario == ApplicationScenario.VoidedDraft
                ? ToUtc(seed.StartDate.AddDays(seed.DayCount + 1), 10, 30)
                : rejectedAt.AddHours(18);

            var application = new Application
            {
                EmployeeId = seed.Employee.Id,
                DepartmentId = departmentId,
                StatusId = GetFinalStatusId(seed.Scenario, statusIds),
                VoidedFromStatusCode = seed.Scenario switch
                {
                    ApplicationScenario.VoidedDraft => "Draft",
                    ApplicationScenario.VoidedRejected => "Rejected",
                    _ => null
                },
                SubmittedAt = seed.Scenario is ApplicationScenario.Approved or ApplicationScenario.Rejected or ApplicationScenario.VoidedRejected or ApplicationScenario.Submitted
                    ? submittedAt
                    : null,
                ApprovedAt = seed.Scenario == ApplicationScenario.Approved ? approvedAt : null,
                ApproverId = seed.Scenario is ApplicationScenario.Approved or ApplicationScenario.Rejected or ApplicationScenario.VoidedRejected or ApplicationScenario.Submitted
                    ? manager.Id
                    : null,
                CreatedAt = ToUtc(seed.StartDate.AddDays(-1), 14, 0)
            };

            db.Applications.Add(application);
            await db.SaveChangesAsync();

            var dailyTrips = new List<DailyTrip>();
            for (var dayIndex = 0; dayIndex < seed.DayCount; dayIndex++)
            {
                var tripDate = seed.StartDate.AddDays(dayIndex);
                dailyTrips.Add(new DailyTrip
                {
                    EmployeeId = seed.Employee.Id,
                    DepartmentId = departmentId,
                    ApplicationId = application.Id,
                    Date = tripDate,
                    TripReason = tripReason,
                    CreatedAt = ToUtc(tripDate, 8, 30),
                    UpdatedAt = ToUtc(tripDate, 8, 30)
                });
            }

            db.DailyTrips.AddRange(dailyTrips);
            await db.SaveChangesAsync();

            var expenses = new List<ExpenseRecord>();
            var transportExpenseModes = new Dictionary<ExpenseRecord, TransportMode>();

            for (var dayIndex = 0; dayIndex < dailyTrips.Count; dayIndex++)
            {
                var trip = dailyTrips[dayIndex];
                var carRate = ResolveRate(carRatePlans, trip.Date);
                var mealRate = ResolveRate(mealRatePlans, trip.Date);

                var transportExpense = BuildTransportExpense(seed, trip, dayIndex, carRate);
                expenses.Add(transportExpense);
                transportExpenseModes[transportExpense] = GetTransportModeForDay(seed, dayIndex);

                expenses.Add(new ExpenseRecord
                {
                    EmployeeId = seed.Employee.Id,
                    DepartmentId = departmentId,
                    DailyTripId = trip.Id,
                    Date = trip.Date,
                    ExpenseCategoryId = 3,
                    ExpenseItemId = 6,
                    Amount = (int)mealRate,
                    Description = "依當期膳雜費率自動帶入",
                    IsRoundTrip = true,
                    CreatedAt = ToUtc(trip.Date, 12, 10),
                    UpdatedAt = ToUtc(trip.Date, 12, 10)
                });

                if (seed.DayCount > 1 && dayIndex < seed.DayCount - 1)
                {
                    expenses.Add(new ExpenseRecord
                    {
                        EmployeeId = seed.Employee.Id,
                        DepartmentId = departmentId,
                        DailyTripId = trip.Id,
                        Date = trip.Date,
                        ExpenseCategoryId = 2,
                        ExpenseItemId = 5,
                        Amount = 2200 + (((trip.Date.Day + dayIndex) % 4) * 250),
                        Description = $"{seed.Destination.City}出差住宿",
                        IsRoundTrip = true,
                        CreatedAt = ToUtc(trip.Date, 18, 30),
                        UpdatedAt = ToUtc(trip.Date, 18, 30)
                    });
                }

                if (((trip.Date.Day + seed.Employee.Name.Length + dayIndex) % 3) == 0)
                {
                    expenses.Add(new ExpenseRecord
                    {
                        EmployeeId = seed.Employee.Id,
                        DepartmentId = departmentId,
                        DailyTripId = trip.Id,
                        Date = trip.Date,
                        ExpenseCategoryId = 4,
                        ExpenseItemId = 7,
                        Amount = 180 + (((trip.Date.Month + dayIndex) % 6) * 60),
                        Description = ((trip.Date.Day + dayIndex) % 2 == 0) ? "停車與過路費" : "文件寄送與雜支",
                        IsRoundTrip = true,
                        CreatedAt = ToUtc(trip.Date, 19, 0),
                        UpdatedAt = ToUtc(trip.Date, 19, 0)
                    });
                }
            }

            db.ExpenseRecords.AddRange(expenses);
            await db.SaveChangesAsync();

            var carbonRecords = new List<CarbonEmissionRecord>();
            foreach (var (expense, mode) in transportExpenseModes)
            {
                var emissionPlan = ResolveEmissionPlan(emissionPlans, mode, expense.Date);
                carbonRecords.Add(new CarbonEmissionRecord
                {
                    ExpenseId = expense.Id,
                    VehicleType = GetVehicleTypeName(mode),
                    DistanceKm = expense.DistanceKm ?? 0m,
                    Co2PerKm = emissionPlan.Co2PerKm,
                    TotalCo2 = decimal.Round((expense.DistanceKm ?? 0m) * emissionPlan.Co2PerKm, 4),
                    EmissionFactorId = GetVehicleTypeId(mode)
                });
            }

            db.CarbonEmissionRecords.AddRange(carbonRecords);

            var approvalHistories = BuildApprovalHistories(seed, application.Id, seed.Employee.Id, manager.Id, submittedAt, approvedAt, rejectedAt, voidedAt);
            if (approvalHistories.Count > 0)
            {
                db.ApprovalHistories.AddRange(approvalHistories);
            }

            if (seed.Scenario is ApplicationScenario.Rejected or ApplicationScenario.VoidedRejected)
            {
                var snapshotComment = GetRejectedComment(seed);
                var carbonByExpenseId = carbonRecords.ToDictionary(c => c.ExpenseId);
                var snapshot = BuildRejectedSnapshot(application, seed.Employee, manager, dailyTrips, expenses, carbonByExpenseId, snapshotComment, rejectedAt);
                db.ReportSnapshots.Add(snapshot);
            }

            await db.SaveChangesAsync();
        }

        private static ExpenseRecord BuildTransportExpense(ApplicationSeed seed, DailyTrip trip, int dayIndex, decimal carRate)
        {
            var mode = GetTransportModeForDay(seed, dayIndex);
            var isSingleDayTrip = seed.DayCount == 1;
            var isFirstDay = dayIndex == 0;
            var isLastDay = dayIndex == seed.DayCount - 1;

            string origin;
            string destination;
            bool isRoundTrip;
            decimal distanceKm;
            string description;

            if (isSingleDayTrip)
            {
                origin = "台北";
                destination = seed.Destination.City;
                isRoundTrip = true;
                distanceKm = seed.Destination.BaseDistanceKm * 2m;
                description = $"{GetExpenseItemNameByMode(mode)}往返 {seed.Destination.City}";
            }
            else if (mode == TransportMode.Taxi)
            {
                origin = seed.Destination.City;
                destination = $"{seed.Destination.City}市區";
                isRoundTrip = true;
                distanceKm = 10m + ((dayIndex + seed.Destination.City.Length) % 4) * 3m;
                description = $"{seed.Destination.City}市區移動";
            }
            else if (isFirstDay)
            {
                origin = "台北";
                destination = seed.Destination.City;
                isRoundTrip = false;
                distanceKm = seed.Destination.BaseDistanceKm;
                description = $"{GetExpenseItemNameByMode(mode)}前往 {seed.Destination.City}";
            }
            else if (isLastDay)
            {
                origin = seed.Destination.City;
                destination = "台北";
                isRoundTrip = false;
                distanceKm = seed.Destination.BaseDistanceKm;
                description = $"{GetExpenseItemNameByMode(mode)}返回台北";
            }
            else
            {
                origin = seed.Destination.City;
                destination = $"{seed.Destination.City}市區";
                isRoundTrip = true;
                distanceKm = 12m;
                description = $"{seed.Destination.City}市區移動";
            }

            return new ExpenseRecord
            {
                EmployeeId = seed.Employee.Id,
                DepartmentId = seed.Employee.DepartmentId,
                DailyTripId = trip.Id,
                Date = trip.Date,
                ExpenseCategoryId = 1,
                ExpenseItemId = GetExpenseItemId(mode),
                Amount = CalculateTransportAmount(mode, seed.Destination, distanceKm, isRoundTrip, carRate),
                DistanceKm = distanceKm,
                Description = description,
                Origin = origin,
                Destination = destination,
                IsRoundTrip = isRoundTrip,
                CreatedAt = ToUtc(trip.Date, 9, 0),
                UpdatedAt = ToUtc(trip.Date, 9, 0)
            };
        }

        private static List<ApprovalHistory> BuildApprovalHistories(
            ApplicationSeed seed,
            int applicationId,
            string employeeId,
            string managerId,
            DateTime submittedAt,
            DateTime approvedAt,
            DateTime rejectedAt,
            DateTime voidedAt)
        {
            var histories = new List<ApprovalHistory>();

            if (seed.Scenario is ApplicationScenario.Approved or ApplicationScenario.Rejected or ApplicationScenario.VoidedRejected or ApplicationScenario.Submitted)
            {
                histories.Add(new ApprovalHistory
                {
                    ApplicationId = applicationId,
                    Action = ApprovalAction.Submit,
                    ActorId = employeeId,
                    CreatedAt = submittedAt
                });
            }

            if (seed.Scenario == ApplicationScenario.Approved)
            {
                histories.Add(new ApprovalHistory
                {
                    ApplicationId = applicationId,
                    Action = ApprovalAction.Approve,
                    ActorId = managerId,
                    CreatedAt = approvedAt
                });
            }

            if (seed.Scenario is ApplicationScenario.Rejected or ApplicationScenario.VoidedRejected)
            {
                histories.Add(new ApprovalHistory
                {
                    ApplicationId = applicationId,
                    Action = ApprovalAction.Reject,
                    ActorId = managerId,
                    Comment = GetRejectedComment(seed),
                    CreatedAt = rejectedAt
                });
            }

            if (seed.Scenario == ApplicationScenario.VoidedDraft)
            {
                histories.Add(new ApprovalHistory
                {
                    ApplicationId = applicationId,
                    Action = ApprovalAction.Void,
                    ActorId = employeeId,
                    Comment = "行程取消，申請單作廢",
                    CreatedAt = voidedAt
                });
            }

            if (seed.Scenario == ApplicationScenario.VoidedRejected)
            {
                histories.Add(new ApprovalHistory
                {
                    ApplicationId = applicationId,
                    Action = ApprovalAction.Void,
                    ActorId = employeeId,
                    Comment = "駁回後決定不再重送，申請單作廢",
                    CreatedAt = voidedAt
                });
            }

            return histories;
        }

        private static ReportSnapshot BuildRejectedSnapshot(
            Application application,
            ApplicationUser employee,
            ApplicationUser manager,
            IReadOnlyList<DailyTrip> dailyTrips,
            IReadOnlyList<ExpenseRecord> expenses,
            IReadOnlyDictionary<int, CarbonEmissionRecord> carbonByExpenseId,
            string comment,
            DateTime createdAt)
        {
            var daySnapshots = dailyTrips
                .OrderBy(d => d.Date)
                .Select(day =>
                {
                    var dayExpenses = expenses
                        .Where(e => e.DailyTripId == day.Id)
                        .OrderBy(e => e.CreatedAt)
                        .ToList();

                    return new ReportSnapshotDayData
                    {
                        DailyTripId = day.Id,
                        Date = day.Date,
                        TripReason = day.TripReason,
                        TransportTotal = dayExpenses.Where(e => e.ExpenseCategoryId == 1).Sum(e => e.Amount),
                        MealTotal = dayExpenses.Where(e => e.ExpenseCategoryId == 3).Sum(e => e.Amount),
                        LodgingTotal = dayExpenses.Where(e => e.ExpenseCategoryId == 2).Sum(e => e.Amount),
                        OtherTotal = dayExpenses.Where(e => e.ExpenseCategoryId == 4).Sum(e => e.Amount),
                        Expenses = dayExpenses.Select(e => new ReportSnapshotExpenseData
                        {
                            CategoryName = GetExpenseCategoryName(e.ExpenseCategoryId),
                            ItemName = GetExpenseItemName(e.ExpenseItemId),
                            Amount = e.Amount,
                            DistanceKm = e.DistanceKm,
                            EstimatedCo2 = carbonByExpenseId.TryGetValue(e.Id, out var carbon) ? carbon.TotalCo2 : null,
                            Origin = e.Origin,
                            Destination = e.Destination,
                            IsRoundTrip = e.IsRoundTrip,
                            Description = e.Description
                        }).ToList()
                    };
                })
                .ToList();

            var snapshotData = new ReportSnapshotData
            {
                ReportId = application.Id,
                YearMonth = daySnapshots.First().Date.ToString("yyyy-MM"),
                EmployeeName = employee.Name,
                SubmittedAt = application.SubmittedAt,
                ApprovedAt = application.ApprovedAt,
                ApproverName = manager.Name,
                StartDate = daySnapshots.First().Date,
                EndDate = daySnapshots.Last().Date,
                TotalAmount = daySnapshots.Sum(d => d.TransportTotal + d.MealTotal + d.LodgingTotal + d.OtherTotal),
                TotalCo2 = carbonByExpenseId.Values.Sum(c => c.TotalCo2),
                DailyTrips = daySnapshots
            };

            return new ReportSnapshot
            {
                ApplicationId = application.Id,
                SnapshotType = RejectedSnapshotType,
                SnapshotJson = JsonSerializer.Serialize(snapshotData),
                Comment = comment,
                CreatedById = manager.Id,
                CreatedAt = createdAt
            };
        }

        private static async Task UpsertSystemConfigAsync(
            ApplicationDbContext db,
            string key,
            string value,
            string description,
            string updatedById)
        {
            var config = await db.SystemConfigs.FirstOrDefaultAsync(c => c.Key == key);
            if (config == null)
            {
                config = new SystemConfig
                {
                    Key = key,
                    Description = description
                };
                db.SystemConfigs.Add(config);
            }

            config.Value = value;
            config.Description = description;
            config.UpdatedAt = DateTime.UtcNow;
            config.UpdatedById = updatedById;
        }

        private static List<DateTime> BuildPlanDates(Random random, DateTime startDate, DateTime endDate, bool includeStartDate)
        {
            var dates = new List<DateTime>();
            if (includeStartDate)
            {
                dates.Add(startDate.Date);
            }

            var firstChange = startDate.AddDays(random.Next(150, 240)).Date;
            var secondChange = startDate.AddDays(random.Next(360, 430)).Date;

            dates.Add(firstChange);
            dates.Add(secondChange > endDate ? endDate.Date.AddDays(-10) : secondChange);

            return dates
                .Distinct()
                .OrderBy(d => d)
                .Take(3)
                .ToList();
        }

        private static List<DestinationProfile> BuildDestinationProfiles()
        {
            return new List<DestinationProfile>
            {
                new("新竹", 75, 290, 180),
                new("台中", 170, 700, 375),
                new("嘉義", 250, 1080, 590),
                new("台南", 320, 1350, 740),
                new("高雄", 350, 1490, 845)
            };
        }

        private static DateTime BuildStartDate(int year, int month, int employeeOffset, int occurrenceIndex)
        {
            var day = 4 + ((employeeOffset * 2 + occurrenceIndex * 3) % 12);
            var date = new DateTime(year, month, day);
            return date > BulkSeedEndDate ? BulkSeedEndDate.AddDays(-1) : date;
        }

        private static int GetFinalStatusId(ApplicationScenario scenario, IReadOnlyDictionary<string, int> statusIds)
        {
            return scenario switch
            {
                ApplicationScenario.Approved => statusIds["Approved"],
                ApplicationScenario.Rejected => statusIds["Rejected"],
                ApplicationScenario.VoidedDraft => statusIds["Voided"],
                ApplicationScenario.VoidedRejected => statusIds["Voided"],
                ApplicationScenario.Submitted => statusIds["Submitted"],
                _ => statusIds["Draft"]
            };
        }

        private static TransportMode GetTransportModeForDay(ApplicationSeed seed, int dayIndex)
        {
            if (seed.DayCount == 1)
            {
                return seed.LongDistanceMode;
            }

            if (dayIndex == 0 || dayIndex == seed.DayCount - 1)
            {
                return seed.LongDistanceMode;
            }

            return TransportMode.Taxi;
        }

        private static decimal ResolveRate(IReadOnlyList<RatePlan> plans, DateTime date)
        {
            return plans
                .Where(p => p.EffectiveFrom <= date.Date)
                .OrderByDescending(p => p.EffectiveFrom)
                .Select(p => p.Rate)
                .First();
        }

        private static EmissionPlan ResolveEmissionPlan(
            IReadOnlyDictionary<TransportMode, IReadOnlyList<EmissionPlan>> plans,
            TransportMode mode,
            DateTime date)
        {
            return plans[mode]
                .Where(p => p.EffectiveFrom <= date.Date)
                .OrderByDescending(p => p.EffectiveFrom)
                .First();
        }

        private static int CalculateTransportAmount(
            TransportMode mode,
            DestinationProfile destination,
            decimal distanceKm,
            bool isRoundTrip,
            decimal carRate)
        {
            return mode switch
            {
                TransportMode.PersonalCar => (int)Math.Ceiling(distanceKm * carRate),
                TransportMode.HSR => isRoundTrip ? destination.HsrFare * 2 : destination.HsrFare,
                TransportMode.Train => isRoundTrip ? destination.TrainFare * 2 : destination.TrainFare,
                TransportMode.Taxi => Math.Max(120, (int)Math.Ceiling(distanceKm * 25m)),
                _ => 0
            };
        }

        private static int GetVehicleTypeId(TransportMode mode)
        {
            return mode switch
            {
                TransportMode.PersonalCar => 1,
                TransportMode.HSR => 2,
                TransportMode.Train => 3,
                TransportMode.Taxi => 4,
                _ => throw new InvalidOperationException($"未知交通工具: {mode}")
            };
        }

        private static int GetExpenseItemId(TransportMode mode)
        {
            return mode switch
            {
                TransportMode.PersonalCar => 1,
                TransportMode.HSR => 2,
                TransportMode.Train => 3,
                TransportMode.Taxi => 4,
                _ => throw new InvalidOperationException($"未知交通工具: {mode}")
            };
        }

        private static string GetVehicleTypeName(TransportMode mode)
        {
            return mode switch
            {
                TransportMode.PersonalCar => "自用車",
                TransportMode.HSR => "高鐵",
                TransportMode.Train => "火車",
                TransportMode.Taxi => "計程車",
                _ => throw new InvalidOperationException($"未知交通工具: {mode}")
            };
        }

        private static string GetExpenseItemNameByMode(TransportMode mode)
        {
            return GetVehicleTypeName(mode);
        }

        private static string GetExpenseCategoryName(int expenseCategoryId)
        {
            return expenseCategoryId switch
            {
                1 => "國內交通費",
                2 => "住宿費",
                3 => "膳雜費",
                4 => "其他費用",
                _ => "未知分類"
            };
        }

        private static string GetExpenseItemName(int expenseItemId)
        {
            return expenseItemId switch
            {
                1 => "自用車",
                2 => "高鐵",
                3 => "火車",
                4 => "計程車",
                5 => "住宿費",
                6 => "膳雜費",
                7 => "其他費用",
                _ => "未知項目"
            };
        }

        private static string GetRejectedComment(ApplicationSeed seed)
        {
            var comments = new[]
            {
                "缺少憑證附件，請補齊後重新送出。",
                "交通費說明不足，請補充起訖與用途。",
                "住宿金額偏高，請補上核准說明。"
            };

            var index = (seed.StartDate.Month + seed.StartDate.Day + seed.Employee.Name.Length) % comments.Length;
            return comments[index];
        }

        private static DateTime ToUtc(DateTime date, int hour, int minute)
        {
            return new DateTime(date.Year, date.Month, date.Day, hour, minute, 0, DateTimeKind.Utc);
        }
    }
}

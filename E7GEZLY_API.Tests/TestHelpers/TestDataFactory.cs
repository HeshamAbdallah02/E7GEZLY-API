using E7GEZLY_API.Data;
using E7GEZLY_API.Models;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Tests.TestHelpers
{
    public static class TestDataFactory
    {
        public static AppDbContext CreateTestDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            var context = new AppDbContext(options);
            SeedBasicData(context);
            return context;
        }

        private static void SeedBasicData(AppDbContext context)
        {
            // Add test governorates
            var governorates = new[]
            {
                new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة" },
                new Governorate { Id = 2, NameEn = "Giza", NameAr = "الجيزة" }
            };
            context.Governorates.AddRange(governorates);

            // Add test districts
            var districts = new[]
            {
                new District
                {
                    Id = 1,
                    NameEn = "Nasr City",
                    NameAr = "مدينة نصر",
                    GovernorateId = 1,
                    CenterLatitude = 30.0626,
                    CenterLongitude = 31.2497
                },
                new District
                {
                    Id = 2,
                    NameEn = "Maadi",
                    NameAr = "المعادي",
                    GovernorateId = 1,
                    CenterLatitude = 30.0131,
                    CenterLongitude = 31.2089
                }
            };
            context.Districts.AddRange(districts);

            context.SaveChanges();
        }
    }
}
using E7GEZLY_API.Models;
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.Data
{
    public static class LocationSeeder
    {
        public static async Task SeedLocationsAsync(AppDbContext context)
        {
            try
            {
                // Check if we already have data
                if (await context.Governorates.AnyAsync())
                {
                    return;
                }

                // Create Cairo governorate
                var cairo = new Governorate
                {
                    NameEn = "Cairo",
                    NameAr = "القاهرة"
                };
                context.Governorates.Add(cairo);

                // Create Giza governorate
                var giza = new Governorate
                {
                    NameEn = "Giza",
                    NameAr = "الجيزة"
                };
                context.Governorates.Add(giza);

                // Create Alexandria governorate
                var alexandria = new Governorate
                {
                    NameEn = "Alexandria",
                    NameAr = "الإسكندرية"
                };
                context.Governorates.Add(alexandria);

                // Create Qalyubia governorate
                var qalyubia = new Governorate
                {
                    NameEn = "Qalyubia",
                    NameAr = "القليوبية"
                };
                context.Governorates.Add(qalyubia);

                // Create Dakahlia governorate
                var dakahlia = new Governorate
                {
                    NameEn = "Dakahlia",
                    NameAr = "الدقهلية"
                };
                context.Governorates.Add(dakahlia);

                // Save governorates first to get their IDs
                await context.SaveChangesAsync();

                // Now add districts using the saved governorates
                var districts = new List<District>
                {
                    // Cairo Districts
                    new District { NameEn = "Nasr City", NameAr = "مدينة نصر", GovernorateId = cairo.Id },
                    new District { NameEn = "New Cairo", NameAr = "القاهرة الجديدة", GovernorateId = cairo.Id },
                    new District { NameEn = "Heliopolis", NameAr = "مصر الجديدة", GovernorateId = cairo.Id },
                    new District { NameEn = "Maadi", NameAr = "المعادي", GovernorateId = cairo.Id },
                    new District { NameEn = "Zamalek", NameAr = "الزمالك", GovernorateId = cairo.Id },
                    new District { NameEn = "Downtown", NameAr = "وسط البلد", GovernorateId = cairo.Id },
                    new District { NameEn = "Dokki", NameAr = "الدقي", GovernorateId = cairo.Id },
                    new District { NameEn = "Mohandeseen", NameAr = "المهندسين", GovernorateId = cairo.Id },
                    new District { NameEn = "Shubra", NameAr = "شبرا", GovernorateId = cairo.Id },
                    new District { NameEn = "Ain Shams", NameAr = "عين شمس", GovernorateId = cairo.Id },
                    
                    // Giza Districts
                    new District { NameEn = "6th of October", NameAr = "السادس من أكتوبر", GovernorateId = giza.Id },
                    new District { NameEn = "Sheikh Zayed", NameAr = "الشيخ زايد", GovernorateId = giza.Id },
                    new District { NameEn = "Haram", NameAr = "الهرم", GovernorateId = giza.Id },
                    new District { NameEn = "Faisal", NameAr = "فيصل", GovernorateId = giza.Id },
                    new District { NameEn = "Giza Square", NameAr = "ميدان الجيزة", GovernorateId = giza.Id },
                    new District { NameEn = "Imbaba", NameAr = "إمبابة", GovernorateId = giza.Id },
                    new District { NameEn = "Agouza", NameAr = "العجوزة", GovernorateId = giza.Id },
                    
                    // Alexandria Districts
                    new District { NameEn = "Montaza", NameAr = "المنتزه", GovernorateId = alexandria.Id },
                    new District { NameEn = "Smouha", NameAr = "سموحة", GovernorateId = alexandria.Id },
                    new District { NameEn = "Sidi Beshr", NameAr = "سيدي بشر", GovernorateId = alexandria.Id },
                    new District { NameEn = "San Stefano", NameAr = "سان ستيفانو", GovernorateId = alexandria.Id },
                    new District { NameEn = "Raml Station", NameAr = "محطة الرمل", GovernorateId = alexandria.Id },
                    new District { NameEn = "Miami", NameAr = "ميامي", GovernorateId = alexandria.Id },
                    new District { NameEn = "Sidi Gaber", NameAr = "سيدي جابر", GovernorateId = alexandria.Id },
                    
                    // Qalyubia Districts
                    new District { NameEn = "Banha", NameAr = "بنها", GovernorateId = qalyubia.Id },
                    new District { NameEn = "Qalyub", NameAr = "قليوب", GovernorateId = qalyubia.Id },
                    new District { NameEn = "Shubra El Kheima", NameAr = "شبرا الخيمة", GovernorateId = qalyubia.Id },
                    new District { NameEn = "Khanka", NameAr = "الخانكة", GovernorateId = qalyubia.Id },
                    new District { NameEn = "Shibin El Qanater", NameAr = "شبين القناطر", GovernorateId = qalyubia.Id },
                    
                    // Dakahlia Districts
                    new District { NameEn = "Mansoura", NameAr = "المنصورة", GovernorateId = dakahlia.Id },
                    new District { NameEn = "Talkha", NameAr = "طلخا", GovernorateId = dakahlia.Id },
                    new District { NameEn = "Mit Ghamr", NameAr = "ميت غمر", GovernorateId = dakahlia.Id },
                    new District { NameEn = "Dekernes", NameAr = "دكرنس", GovernorateId = dakahlia.Id },
                    new District { NameEn = "Belqas", NameAr = "بلقاس", GovernorateId = dakahlia.Id }
                };

                await context.Districts.AddRangeAsync(districts);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error seeding locations: {ex.Message}", ex);
            }
        }
    }
}
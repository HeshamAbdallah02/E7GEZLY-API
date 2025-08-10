using E7GEZLY_API.Domain.Entities;
using E7GEZLY_API.Domain.Enums;

namespace E7GEZLY_API.Domain.Services;

/// <summary>
/// Domain service implementation for venue profile completion logic
/// Contains the complex business rules for determining profile completeness
/// </summary>
public sealed class VenueProfileCompletionService : IVenueProfileCompletionService
{
    public async Task<VenueProfileCompletionResult> EvaluateCompletionAsync(Venue venue)
    {
        var completionPercentage = await GetCompletionPercentageAsync(venue);
        var missingRequirements = await GetMissingRequirementsAsync(venue);
        var suggestions = await GetImprovementSuggestionsAsync(venue);
        
        var isComplete = completionPercentage >= 100 && !missingRequirements.Any();
        
        return VenueProfileCompletionResult.Create(isComplete, completionPercentage, missingRequirements, suggestions);
    }

    public async Task<int> GetCompletionPercentageAsync(Venue venue)
    {
        var completionItems = new List<(string Name, bool IsComplete, int Weight)>
        {
            // Basic Information (30%)
            ("Venue Name", !string.IsNullOrWhiteSpace(venue.Name.Name), 10),
            ("Venue Type", venue.VenueType != default, 10),
            ("Address Information", !venue.Address.IsEmpty, 10),
            
            // Location (10%)
            ("District", venue.DistrictSystemId.HasValue, 5),
            ("Coordinates", venue.Address.Coordinates != null, 5),
            
            // Working Hours (20%)
            ("Working Hours", venue.WorkingHours.Any(), 20),
            
            // Pricing (25%)
            ("Pricing Information", venue.Pricing.Any(), 25),
            
            // Images (10%)
            ("Venue Images", venue.Images.Any(), 5),
            ("Primary Image", venue.Images.Any(i => i.IsPrimary), 5),
            
            // Type-specific requirements (5%)
            ("Type-specific Details", await IsTypeSpecificCompleteAsync(venue), 5)
        };

        var totalWeight = completionItems.Sum(item => item.Weight);
        var completedWeight = completionItems.Where(item => item.IsComplete).Sum(item => item.Weight);
        
        return totalWeight > 0 ? (int)Math.Round((double)completedWeight / totalWeight * 100) : 0;
    }

    public async Task<IEnumerable<string>> GetMissingRequirementsAsync(Venue venue)
    {
        var missingRequirements = new List<string>();

        // Basic Information
        if (string.IsNullOrWhiteSpace(venue.Name.Name))
            missingRequirements.Add("Venue name is required");

        if (venue.VenueType == default)
            missingRequirements.Add("Venue type must be selected");

        if (venue.Address.IsEmpty)
            missingRequirements.Add("Address information is required");

        // Location
        if (!venue.DistrictSystemId.HasValue)
            missingRequirements.Add("District must be selected");

        if (venue.Address.Coordinates == null)
            missingRequirements.Add("Location coordinates are required");

        // Working Hours
        if (!venue.WorkingHours.Any())
            missingRequirements.Add("At least one day of working hours must be set");

        // Pricing
        if (!venue.Pricing.Any())
            missingRequirements.Add("At least one pricing option must be configured");

        // Images
        if (!venue.Images.Any())
            missingRequirements.Add("At least one venue image is required");

        if (!venue.Images.Any(i => i.IsPrimary))
            missingRequirements.Add("A primary image must be selected");

        // Type-specific requirements
        await AddTypeSpecificRequirementsAsync(venue, missingRequirements);

        return missingRequirements;
    }

    public async Task<IEnumerable<string>> GetImprovementSuggestionsAsync(Venue venue)
    {
        var suggestions = new List<string>();

        // Working hours suggestions
        var daysCovered = venue.WorkingHours.Select(wh => wh.DayOfWeek).ToHashSet();
        var missingDays = Enum.GetValues<DayOfWeek>().Except(daysCovered).ToList();
        
        if (missingDays.Any())
        {
            suggestions.Add($"Consider adding working hours for: {string.Join(", ", missingDays)}");
        }

        // Image suggestions
        if (venue.Images.Count < 3)
            suggestions.Add("Add more images to showcase your venue (recommended: 3-5 images)");

        if (venue.Images.Count > 0 && venue.Images.All(i => string.IsNullOrEmpty(i.Caption)))
            suggestions.Add("Add captions to your images to provide more context");

        // Pricing suggestions
        await AddPricingSuggestionsAsync(venue, suggestions);

        // Feature suggestions
        if (venue.Features == VenueFeatures.None)
            suggestions.Add("Add venue features to help customers understand what you offer");

        // Type-specific suggestions
        await AddTypeSpecificSuggestionsAsync(venue, suggestions);

        return suggestions;
    }

    public async Task<VenueProfileCompletionResult> CheckVenueProfileCompletionAsync(Venue venue)
    {
        return await EvaluateCompletionAsync(venue);
    }

    private async Task<bool> IsTypeSpecificCompleteAsync(Venue venue)
    {
        switch (venue.VenueType)
        {
            case VenueType.PlayStationVenue:
                return venue.PlayStationDetails != null;
                
            case VenueType.FootballCourt:
            case VenueType.PadelCourt:
                // Court-specific requirements (could be extended)
                return venue.WorkingHours.Any(wh => wh.HasTimeSlots);
                
            case VenueType.MultiPurpose:
                // Multi-purpose venues might have specific requirements
                return true;
                
            default:
                return true;
        }
    }

    private async Task AddTypeSpecificRequirementsAsync(Venue venue, List<string> missingRequirements)
    {
        switch (venue.VenueType)
        {
            case VenueType.PlayStationVenue:
                if (venue.PlayStationDetails == null)
                    missingRequirements.Add("PlayStation venue details must be configured");
                break;
                
            case VenueType.FootballCourt:
            case VenueType.PadelCourt:
                if (!venue.WorkingHours.Any(wh => wh.HasTimeSlots))
                    missingRequirements.Add("Court venues should have morning and evening time slots configured");
                break;
        }
    }

    private async Task AddPricingSuggestionsAsync(Venue venue, List<string> suggestions)
    {
        switch (venue.VenueType)
        {
            case VenueType.PlayStationVenue:
                var hasPS4Pricing = venue.Pricing.Any(p => p.PlayStationModel == PlayStationModel.PS4);
                var hasPS5Pricing = venue.Pricing.Any(p => p.PlayStationModel == PlayStationModel.PS5);
                
                if (venue.PlayStationDetails?.HasPS4 == true && !hasPS4Pricing)
                    suggestions.Add("Add PS4 pricing since you offer PS4 gaming");
                    
                if (venue.PlayStationDetails?.HasPS5 == true && !hasPS5Pricing)
                    suggestions.Add("Add PS5 pricing since you offer PS5 gaming");
                    
                if (venue.PlayStationDetails?.HasVIPRooms == true && !venue.Pricing.Any(p => p.RoomType == RoomType.VIP))
                    suggestions.Add("Add VIP room pricing since you have VIP rooms");
                break;
                
            case VenueType.FootballCourt:
            case VenueType.PadelCourt:
                var hasMorningPricing = venue.Pricing.Any(p => p.TimeSlotType == TimeSlotType.Morning);
                var hasEveningPricing = venue.Pricing.Any(p => p.TimeSlotType == TimeSlotType.Evening);
                
                if (!hasMorningPricing)
                    suggestions.Add("Add morning time slot pricing");
                    
                if (!hasEveningPricing)
                    suggestions.Add("Add evening time slot pricing");
                break;
        }

        // General pricing suggestions
        if (venue.Pricing.All(p => !p.DepositPercentage.HasValue))
            suggestions.Add("Consider setting deposit requirements for bookings");
    }

    private async Task AddTypeSpecificSuggestionsAsync(Venue venue, List<string> suggestions)
    {
        switch (venue.VenueType)
        {
            case VenueType.PlayStationVenue:
                if (venue.PlayStationDetails != null)
                {
                    if (!venue.PlayStationDetails.HasWiFi)
                        suggestions.Add("Consider offering Wi-Fi to enhance customer experience");
                        
                    if (!venue.PlayStationDetails.HasCafe)
                        suggestions.Add("Consider adding café services for longer gaming sessions");
                        
                    if (!venue.PlayStationDetails.ShowsMatches)
                        suggestions.Add("Consider showing sports matches to attract more customers");
                }
                break;
                
            case VenueType.FootballCourt:
            case VenueType.PadelCourt:
                if (!venue.HasFeature(VenueFeatures.LightingSystem))
                    suggestions.Add("Consider adding lighting system for evening play");
                    
                if (!venue.HasFeature(VenueFeatures.Equipment))
                    suggestions.Add("Consider providing equipment rental services");
                break;
        }
    }

    public bool CanCompletePlayStationProfile(VenueType venueType)
    {
        return venueType == VenueType.PlayStationVenue;
    }

    public bool CanCompleteCourtProfile(VenueType venueType)
    {
        return venueType == VenueType.FootballCourt || venueType == VenueType.PadelCourt;
    }
}
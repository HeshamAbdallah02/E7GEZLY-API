using E7GEZLY_API.Domain.Entities;

namespace E7GEZLY_API.Domain.Services;

/// <summary>
/// Domain service for managing venue profile completion logic
/// Encapsulates complex business rules around what constitutes a complete venue profile
/// </summary>
public interface IVenueProfileCompletionService
{
    /// <summary>
    /// Evaluates whether a venue profile is complete based on business rules
    /// </summary>
    Task<VenueProfileCompletionResult> EvaluateCompletionAsync(Venue venue);
    
    /// <summary>
    /// Gets the completion percentage of a venue profile
    /// </summary>
    Task<int> GetCompletionPercentageAsync(Venue venue);
    
    /// <summary>
    /// Gets missing requirements for completing the venue profile
    /// </summary>
    Task<IEnumerable<string>> GetMissingRequirementsAsync(Venue venue);
    
    /// <summary>
    /// Gets suggestions for improving the venue profile
    /// </summary>
    Task<IEnumerable<string>> GetImprovementSuggestionsAsync(Venue venue);
    
    /// <summary>
    /// Checks if the venue profile is complete and returns validation results
    /// </summary>
    Task<VenueProfileCompletionResult> CheckVenueProfileCompletionAsync(Venue venue);
    
    /// <summary>
    /// Checks if a venue type can complete PlayStation profile
    /// </summary>
    bool CanCompletePlayStationProfile(Domain.Enums.VenueType venueType);
    
    /// <summary>
    /// Checks if a venue type can complete court profile
    /// </summary>
    bool CanCompleteCourtProfile(Domain.Enums.VenueType venueType);
}

/// <summary>
/// Result of venue profile completion evaluation
/// </summary>
public sealed class VenueProfileCompletionResult
{
    private VenueProfileCompletionResult(bool isComplete, int completionPercentage, 
        IEnumerable<string> missingRequirements, IEnumerable<string> suggestions)
    {
        IsComplete = isComplete;
        CompletionPercentage = completionPercentage;
        MissingRequirements = missingRequirements.ToList().AsReadOnly();
        Suggestions = suggestions.ToList().AsReadOnly();
    }

    public static VenueProfileCompletionResult Create(bool isComplete, int completionPercentage, 
        IEnumerable<string> missingRequirements, IEnumerable<string> suggestions)
    {
        if (completionPercentage < 0 || completionPercentage > 100)
            throw new ArgumentOutOfRangeException(nameof(completionPercentage), "Completion percentage must be between 0 and 100");

        return new VenueProfileCompletionResult(isComplete, completionPercentage, missingRequirements, suggestions);
    }

    public bool IsComplete { get; }
    public int CompletionPercentage { get; }
    public IReadOnlyCollection<string> MissingRequirements { get; }
    public IReadOnlyCollection<string> Suggestions { get; }
    
    public bool CanMarkAsComplete => IsComplete && CompletionPercentage >= 100;
}
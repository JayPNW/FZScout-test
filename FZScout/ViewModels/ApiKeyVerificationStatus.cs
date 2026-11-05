namespace FZScout.ViewModels;

/// <summary>When checking with the site or saving fails, the log has the details.</summary>
enum ApiKeyVerificationStatus
{
    NotVerified,
    Verifying,
    VerifiedAndSaved,
    RejectedBySite,
    CouldNotCheckWithSite,
    CouldNotSave,
}

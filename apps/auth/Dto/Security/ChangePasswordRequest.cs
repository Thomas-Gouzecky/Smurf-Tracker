public sealed class ChangePasswordRequest
{
    /// <summary>
    /// The current password of the user.
    /// </summary>
    public required string CurrentPassword { get; init; }

    /// <summary>
    /// The new password to set for the user.
    /// </summary>
    public required string NewPassword { get; init; }
}
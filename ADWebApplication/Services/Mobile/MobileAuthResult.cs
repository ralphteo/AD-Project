namespace ADWebApplication.Services;

public enum MobileAuthError
{
    None,
    EmailAlreadyRegistered,
    InvalidCredentials,
    AccountInactive,
    NotFound,
    Forbidden,
    InvalidRegion
}

public sealed record MobileAuthResult<T>(T? Data, MobileAuthError Error, string? Message = null)
{
    public bool Success => Error == MobileAuthError.None;

    public static MobileAuthResult<T> Ok(T data, string? message = null) =>
        new(data, MobileAuthError.None, message);

    public static MobileAuthResult<T> Fail(MobileAuthError error, string? message = null) =>
        new(default, error, message);
}

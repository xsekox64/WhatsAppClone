namespace WhatsAppClone.Authentincation
{
    public sealed record AuthLoginDto(
    string Token,
    string RefreshToken,
    DateTime RefreshTokenExpires);
}

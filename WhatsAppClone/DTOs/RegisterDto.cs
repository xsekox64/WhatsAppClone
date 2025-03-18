namespace WhatsAppClone.DTOs
{
    public sealed record RegisterDto(
     string NameLastName,
     string UserName,
     string Email,
     string Password);
}

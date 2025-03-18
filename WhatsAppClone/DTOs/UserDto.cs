namespace WhatsAppClone.DTOs
{
    public class UserDto
    {
        public Guid UserId { get; set; }
        public bool IsAdmin { get; set; }  // Admin yetkisi
    }
}

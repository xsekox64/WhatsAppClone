namespace WhatsAppClone.DTOs
{
    public class AcceptInviteDto
    {
        public Guid GroupId { get; set; }
        public Guid InviteeId { get; set; }
        public int Accetp { get; set; }
    }
}

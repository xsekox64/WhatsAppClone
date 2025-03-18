namespace WhatsAppClone.DTOs
{
    public class GroupInviteDto
    {
        public Guid GroupId { get; set; }

        public Guid InviterId { get; set; }

        public Guid InviteeId { get; set; }

        public DateTime? SentAt { get; set; }
    }
}

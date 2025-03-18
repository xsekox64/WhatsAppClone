namespace WhatsAppClone.DTOs
{
    public class UpdateGroupMessageStatusDto
    {
        public Guid MessageId { get; set; }
        public Guid ReceiverId { get; set; }
        public string Status { get; set; } // "Wait", "Sent", "Read" vb.
    }
}

namespace WhatsAppClone.DTOs
{
    public class UpdateMessageStatusDto
    {
        public Guid MessageId { get; set; }
        public Guid ReceiverId { get; set; }
        public string Status { get; set; } // "Sent", "Seen", vb.
    }
}

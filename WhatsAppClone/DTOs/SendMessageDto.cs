namespace WhatsAppClone.DTOs
{
    public class SendMessageDto
    {
        public Guid SenderId { get; set; }
        public Guid ReceiverId { get; set; }
        public string Content { get; set; }
        public string MessageType { get; set; } // "Text", "Image", "Video", vb.
        public string MediaUrl { get; set; }
    }
}

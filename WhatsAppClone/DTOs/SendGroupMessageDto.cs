namespace WhatsAppClone.DTOs
{
    public class SendGroupMessageDto
    {
        public Guid GroupId { get; set; }
        public string GroupName { get; set; }
        public Guid SenderId { get; set; }
        public string Content { get; set; }
        public string MessageType { get; set; } // "Text", "Image", "Video"
        public string MediaUrl { get; set; } // Opsiyonel (Resim, video vs.)
    }

}

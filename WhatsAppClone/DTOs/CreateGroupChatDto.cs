namespace WhatsAppClone.DTOs
{
    public class CreateGroupChatDto
    {
        public string GroupName { get; set; }
        public string GroupImage { get; set; }
        public List<Guid> UserIds { get; set; }
    }
}

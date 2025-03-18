namespace WhatsAppClone.DTOs
{    public class GroupSaveDto
    {
        public string GroupName { get; set; }
        public string GroupImage { get; set; }
        public Guid CreatedBy { get; set; }       
        public List<UserDto> Members { get; set; } = new List<UserDto>(); // Grup üyeleri
    }
}

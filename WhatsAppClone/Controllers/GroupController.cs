using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System;
using System.Text.RegularExpressions;
using WhatsAppClone.DTOs;
using WhatsAppClone.Models;

namespace WhatsAppClone.Controllers
{
    [Route("api/[controller]/[action]")]
    //[Authorize(AuthenticationSchemes ="Bearer")]
    [ApiController]
    public class GroupController : ControllerBase
    {
        private readonly EkikWhatsappContext _context;

        public GroupController(EkikWhatsappContext context)
        {
            _context = context;
        }

        [HttpPost("GroupCreate")]
        public async Task<IActionResult> GroupSave([FromBody] GroupSaveDto request)
        {
            try
            {
                // Aynı isimde bir grup var mı kontrol et
                var existingGroup = _context.Groups
                    .FirstOrDefault(p => p.GroupName.ToLower() == request.GroupName.ToLower());

                if (existingGroup != null)
                    return BadRequest("Bu grup daha önce kaydedilmiş.");

                // Yeni grup oluştur
                var newGroup = new Models.Group
                {
                    Id = Guid.NewGuid(),
                    GroupName = request.GroupName,
                    GroupImage = request.GroupImage,
                    CreatedBy = request.CreatedBy,
                    CreatedAt = DateTime.Now
                };

                _context.Groups.Add(newGroup);
                await _context.SaveChangesAsync();

                // Kullanıcı listesini oluştur ve admini ekleyerek gruba dahil et
                var groupMembers = new List<GroupMember>();

                // 1️⃣ **Admin olarak eklenmesi gereken kişi**:
                groupMembers.Add(new GroupMember
                {
                    Id = Guid.NewGuid(),
                    GroupId = newGroup.Id,
                    UserId = request.CreatedBy,
                    IsAdmin = true, // Admin olarak atanıyor
                    JoinedAt = DateTime.Now
                });

                // 2️⃣ **Diğer üyeleri ekle** (Eğer varsa)
                foreach (var member in request.Members)
                {
                    // member.UserId ile karşılaştırıyoruz, çünkü request.Members'de her öğe UserDto
                    if (member.UserId != request.CreatedBy)
                    {
                        groupMembers.Add(new GroupMember
                        {
                            Id = Guid.NewGuid(),
                            GroupId = newGroup.Id,
                            UserId = member.UserId, // UserDto'dan UserId alıyoruz
                            IsAdmin = false, // Normal üye
                            JoinedAt = DateTime.Now
                        });
                    }
                }

                // **Bütün üyeleri tek seferde ekleyelim**
                if (groupMembers.Count > 0)
                {
                    _context.GroupMembers.AddRange(groupMembers);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { Message = "Grup başarıyla oluşturuldu.", GroupId = newGroup.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Bir hata oluştu: {ex.Message}");
            }
        }



        [HttpPost("GroupInvites")]
        public async Task<IActionResult> GroupInvites([FromBody] GroupInviteDto request)
        {
            try
            {
                // Davetin zaten var olup olmadığını kontrol et
                var existingInvite = await _context.GroupInvites
                    .FirstOrDefaultAsync(i => i.GroupId == request.GroupId &&
                                              i.InviteeId == request.InviteeId);

                if (existingInvite != null)
                    return BadRequest("Bu kullanıcıya zaten bir davet gönderilmiş.");

                // Yeni daveti oluştur
                var invite = new GroupInvite
                {
                    Id = Guid.NewGuid(),
                    GroupId = request.GroupId,
                    InviterId = request.InviterId,
                    InviteeId = request.InviteeId,
                    Status = "Pending", // Varsayılan olarak bekleyen durumunda başlatıyoruz.
                    SentAt = DateTime.Now
                };

                _context.GroupInvites.Add(invite);
                await _context.SaveChangesAsync();

                return Ok("Davet başarıyla gönderildi.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Bir hata oluştu: {ex.Message}");
            }
        }

        [HttpGet("GetUserGroups/{userId}")]
        public async Task<IActionResult> GetUserGroups(Guid userId)
        {
            try
            {
                var groups = await _context.Groups
                    .Where(g => _context.GroupMembers
                        .Any(m => m.GroupId == g.Id && m.UserId == userId)) // Kullanıcının üye olduğu gruplar
                    .Select(g => new
                    {
                        g.Id,
                        g.GroupName,
                        g.GroupImage,
                        g.CreatedAt,
                        g.CreatedBy,
                        Members = _context.GroupMembers
                            .Where(m => m.GroupId == g.Id)
                            .Join(_context.Users,
                                m => m.UserId,
                                u => u.Id,
                                (m, u) => new
                                {
                                    u.Id,
                                    u.Name,
                                    u.SurName,
                                    u.Tcno,
                                    u.Email,
                                    m.IsAdmin,
                                    m.JoinedAt,
                                    u.ProfileImage // Varsayılan profil resmi
                                })
                            .ToList() // Tüm üyeleri alıyoruz
                    })
                    .ToListAsync();

                if (groups == null || !groups.Any())
                    return NotFound("Grup bulunamadı.");

                return Ok(groups);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Bir hata oluştu: {ex.Message}");
            }
        }



        [HttpPost("AcceptInvite")]
        public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteDto request)
        {
            try
            {
                // Davetin var olup olmadığını kontrol et
                var invite = await _context.GroupInvites
                    .FirstOrDefaultAsync(i => i.GroupId == request.GroupId &&
                                              i.InviteeId == request.InviteeId &&
                                              i.Status == "Pending");

                if (invite == null)
                    return BadRequest("Geçerli bir davet bulunamadı veya zaten kabul edilmiş.");

                if (request.Accetp == 1)
                {
                    // Kullanıcıyı GroupMembers tablosuna ekle
                    var groupMember = new GroupMember
                    {
                        Id = Guid.NewGuid(),
                        GroupId = request.GroupId,
                        UserId = request.InviteeId,
                        IsAdmin = false,  // Normal üye olarak ekleniyor
                        JoinedAt = DateTime.Now
                    };

                    _context.GroupMembers.Add(groupMember);

                    // Davetin durumunu "Accepted" olarak güncelle
                    invite.Status = "Accepted";

                    await _context.SaveChangesAsync(); // Değişiklikleri kaydet

                    return Ok("Davet kabul edildi, kullanıcı gruba eklendi.");
                }
                else
                {
                    // Davetin durumunu "Rejected" olarak güncelle
                    invite.Status = "Rejected";

                    await _context.SaveChangesAsync(); // Değişiklikleri kaydet

                    return Ok("Davet reddedildi.");
                }

                
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Bir hata oluştu: {ex.Message}");
            }
        }
        //Yeni bir grup oluşturulunca, Chats tablosuna bir kayıt eklenmeli.
        [HttpPost("CreateGroupChat")]
        public async Task<IActionResult> CreateGroupChat([FromBody] CreateGroupChatDto request)
        {
            try
            {
                // Yeni sohbet oluştur (grup sohbeti)
                var chat = new Chat
                {
                    Id = Guid.NewGuid(),
                    IsGroup = true,
                    GroupName = request.GroupName,
                    GroupImage = request.GroupImage,
                    CreatedAt = DateTime.Now
                };

                _context.Chats.Add(chat);
                await _context.SaveChangesAsync();

                // Gruba katılan kullanıcıları ChatParticipants tablosuna ekle
                var participants = request.UserIds.Select(userId => new ChatParticipant
                {
                    ChatId = chat.Id,
                    UserId = userId
                }).ToList();

                _context.ChatParticipants.AddRange(participants);
                await _context.SaveChangesAsync();

                return Ok(new { chatId = chat.Id, message = "Grup sohbeti oluşturuldu" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Bir hata oluştu: {ex.Message}");
            }
        }
        //Grup Mesajı Gönderme        
        [HttpPost("SendGroupMessage")]
        public async Task<IActionResult> SendGroupMessage([FromBody] SendGroupMessageDto request)
        {
            try
            {
                // 1. Grup Sohbetini (Chat) Kontrol Et, Yoksa Oluştur
                var chat = await _context.Chats
                    .Where(c => c.IsGroup == true && c.GroupName == request.GroupName)
                    .FirstOrDefaultAsync();

                if (chat == null)
                {
                    chat = new Chat
                    {
                        Id = Guid.NewGuid(),
                        IsGroup = true,
                        GroupName = request.GroupName, // Grup ismini kaydediyoruz
                        CreatedAt = DateTime.Now
                    };
                    _context.Chats.Add(chat);
                    await _context.SaveChangesAsync();

                    // 2. Grup Katılımcılarını ChatParticipants Tablosuna Ekle
                    var groupMembers = await _context.Groups
                        .Where(g => g.Id == request.GroupId)
                        .SelectMany(g => g.GroupMembers)
                        .Select(gp => gp.UserId)
                        .ToListAsync();

                    var chatParticipants = groupMembers.Select(userId => new ChatParticipant
                    {
                        ChatId = chat.Id,
                        UserId = userId
                    }).ToList();

                    _context.ChatParticipants.AddRange(chatParticipants);
                    await _context.SaveChangesAsync();
                }

                // 3. Mesajı GroupMessages Tablosuna Kaydet
                var message = new GroupMessage
                {
                    Id = Guid.NewGuid(),
                    GroupId = request.GroupId,
                    SenderId = request.SenderId,
                    Content = request.Content,
                    MessageType = request.MessageType,
                    MediaUrl = request.MediaUrl,
                    Timestamp = DateTime.Now
                };

                _context.GroupMessages.Add(message);
                await _context.SaveChangesAsync();

                // 4. Mesaj Durumu (GroupMessageStatuses) Ekle
                var userIds = await _context.ChatParticipants
                    .Where(cp => cp.ChatId == chat.Id && cp.UserId != request.SenderId)
                    .Select(cp => cp.UserId)
                    .ToListAsync();

                var messageStatuses = userIds.Select(receiverId => new GroupMessageStatus
                {
                    Id = Guid.NewGuid(),
                    MessageId = message.Id,
                    ReceiverId = receiverId,
                    Status = "Sent",
                    Timestamp = DateTime.Now
                }).ToList();

                _context.GroupMessageStatuses.AddRange(messageStatuses);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Grup mesajı başarıyla gönderildi.",
                    messageId = message.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Bir hata oluştu: {ex.Message}");
            }
        }


        [HttpPost("UpdateGroupMessageStatus")]
        public async Task<IActionResult> UpdateGroupMessageStatus([FromBody] UpdateGroupMessageStatusDto request)
        {
            try
            {
                var messageStatus = await _context.GroupMessageStatuses
                    .FirstOrDefaultAsync(gms => gms.MessageId == request.MessageId && gms.ReceiverId == request.ReceiverId);

                if (messageStatus == null)
                {
                    return NotFound("Mesaj durumu bulunamadı.");
                }

                // Durum güncelleniyor
                messageStatus.Status = request.Status;  // "Sent", "Read" vs.
                messageStatus.Timestamp = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok("Mesaj durumu başarıyla güncellendi.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Bir hata oluştu: {ex.Message}");
            }
        }
        //Kullanıcının Grup Sohbetlerini Listeleme
        [HttpGet("GetUserGroupChats/{userId}")]
        public async Task<IActionResult> GetUserGroupChats(Guid userId)
        {
            try
            {
                var groups = await _context.ChatParticipants
                    .Where(cp => cp.UserId == userId)
                    .Select(cp => new
                    {
                        cp.Chat.Id,
                        cp.Chat.GroupName,
                        cp.Chat.GroupImage,
                        cp.Chat.CreatedAt
                    })
                    .ToListAsync();

                return Ok(groups);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Bir hata oluştu: {ex.Message}");
            }
        }
        //Bir Grup Sohbetinin Mesajlarını Listeleme
        [HttpGet("GetGroupMessages/{groupId}")]
        public async Task<IActionResult> GetGroupMessages(Guid groupId)
        {
            try
            {
                var messages = await _context.GroupMessages
                    .Where(m => m.GroupId == groupId)
                    .OrderBy(m => m.Timestamp)
                    .Select(m => new
                    {
                        m.Id,
                        m.SenderId,
                        m.Content,
                        m.MessageType,
                        m.MediaUrl,
                        m.Timestamp
                    })
                    .ToListAsync();

                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Bir hata oluştu: {ex.Message}");
            }
        }
        //Kullanıcının Görmediği Grup Mesajlarını Getirme
        [HttpGet("GetUnreadGroupMessages/{userId}")]
        public async Task<IActionResult> GetUnreadGroupMessages(Guid userId)
        {
            try
            {
                var unreadMessages = await _context.GroupMessageStatuses
                    .Where(ms => ms.ReceiverId == userId && ms.Status == "Sent")
                    .Select(ms => new
                    {
                        ms.MessageId,
                        ms.Message.GroupId,
                        ms.Message.SenderId,
                        ms.Message.Content,
                        ms.Message.MessageType,
                        ms.Message.MediaUrl,
                        ms.Message.Timestamp
                    })
                    .ToListAsync();

                return Ok(unreadMessages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Bir hata oluştu: {ex.Message}");
            }
        }

    }
}

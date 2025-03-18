using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WhatsAppClone.DTOs;
using WhatsAppClone.Models;

namespace WhatsAppClone.Controllers
{
    [Route("api/[controller]/[action]")]
    //[Authorize(AuthenticationSchemes ="Bearer")]
    [ApiController]
    public class UserMessageController : ControllerBase
    {
        private readonly EkikWhatsappContext _context;

        public UserMessageController(EkikWhatsappContext context)
        {
            _context = context;
        }

        // Kullanıcılar arasında mesaj gönderme
        //[HttpPost("SendMessage")]
        //public async Task<IActionResult> SendMessage([FromBody] SendMessageDto request)
        //{
        //    try
        //    {
        //        // 1. Yeni bir sohbet oluştur (Eğer yoksa)
        //        var chat = await _context.Chats
        //            .Where(c => c.IsGroup == false)
        //            .FirstOrDefaultAsync(c =>
        //                (c.ChatParticipants.Any(u => u.UserId == request.SenderId) &&
        //                c.ChatParticipants.Any(u => u.UserId == request.ReceiverId)));

        //        if (chat == null)
        //        {
        //            chat = new Chat
        //            {
        //                Id = Guid.NewGuid(),
        //                IsGroup = false,
        //                CreatedAt = DateTime.Now
        //            };
        //            _context.Chats.Add(chat);
        //            await _context.SaveChangesAsync();

        //            // ChatParticipants ekle
        //            _context.ChatParticipants.AddRange(new List<ChatParticipant>
        //        {
        //            new ChatParticipant { ChatId = chat.Id, UserId = request.SenderId },
        //            new ChatParticipant { ChatId = chat.Id, UserId = request.ReceiverId }
        //        });
        //            await _context.SaveChangesAsync();
        //        }

        //        // 2. Yeni mesajı Messages tablosuna ekle
        //        var message = new Message
        //        {
        //            Id = Guid.NewGuid(),
        //            ChatId = chat.Id,
        //            SenderId = request.SenderId,
        //            Content = request.Content,
        //            MessageType = request.MessageType,
        //            MediaUrl = request.MediaUrl,
        //            Timestamp = DateTime.Now
        //        };

        //        _context.Messages.Add(message);
        //        await _context.SaveChangesAsync();

        //        // 3. Mesaj durumu ekle (Alıcıya mesaj durumu gönder)
        //        var messageStatus = new MessageStatus
        //        {
        //            Id = Guid.NewGuid(),
        //            MessageId = message.Id,
        //            ReceiverId = request.ReceiverId,
        //            Status = "Sent",
        //            Timestamp = DateTime.Now
        //        };

        //        _context.MessageStatuses.Add(messageStatus);
        //        await _context.SaveChangesAsync();

        //        // 🔥 JSON formatında yanıt dön
        //        return Ok(new
        //        {
        //            message = "Mesaj başarıyla gönderildi.",
        //            messageId = message.Id
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Bir hata oluştu: {ex.Message}");
        //    }
        //}
        [HttpPost("SendMessage")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto request)
        {
            try
            {
                var chat = await _context.Chats
                    .Where(c => c.IsGroup == false)
                    .FirstOrDefaultAsync(c =>
                        c.ChatParticipants.Any(u => u.UserId == request.SenderId) &&
                        c.ChatParticipants.Any(u => u.UserId == request.ReceiverId));

                if (chat == null)
                {
                    chat = new Chat
                    {
                        Id = Guid.NewGuid(),
                        IsGroup = false,
                        CreatedAt = DateTime.Now
                    };
                    _context.Chats.Add(chat);
                    await _context.SaveChangesAsync();

                    _context.ChatParticipants.AddRange(new List<ChatParticipant>
            {
                new ChatParticipant { ChatId = chat.Id, UserId = request.SenderId },
                new ChatParticipant { ChatId = chat.Id, UserId = request.ReceiverId }
            });
                    await _context.SaveChangesAsync();
                }

                var message = new Message
                {
                    Id = Guid.NewGuid(),
                    ChatId = chat.Id,
                    SenderId = request.SenderId,
                    Content = request.Content,
                    MessageType = request.MessageType,
                    MediaUrl = request.MediaUrl, // 📌 Resim URL'si burada saklanıyor
                    Timestamp = DateTime.Now
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                var messageStatus = new MessageStatus
                {
                    Id = Guid.NewGuid(),
                    MessageId = message.Id,
                    ReceiverId = request.ReceiverId,
                    Status = "Sent",
                    Timestamp = DateTime.Now
                };

                _context.MessageStatuses.Add(messageStatus);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Mesaj başarıyla gönderildi.",
                    messageId = message.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Bir hata oluştu: {ex.Message}");
            }
        }


        [HttpPost("UploadImage")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Dosya Bulunamadı.");
            }
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            string filePath = Path.Combine(uploadsFolder, fileName);

            using(var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            string fileUrl = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";
            return Ok(new {Url = fileUrl});

        }

        // Kullanıcılar arasındaki sohbetleri al      
        [HttpGet("GetChats/{userId}")]
        public async Task<IActionResult> GetChats(Guid userId)
        {
            try
            {
                var messages = await _context.ChatParticipants
                    .Where(cp => cp.UserId == userId) // Kullanıcının dahil olduğu sohbetleri al
                    .Select(cp => cp.ChatId)
                    .Distinct()
                    .Select(chatId =>
                        _context.Messages
                            .Where(m => m.ChatId == chatId) // Sohbetteki mesajları al
                            .OrderByDescending(m => m.Timestamp) // En son mesajı al
                            .Select(m => new
                            {
                                m.Id,
                                m.SenderId,
                                m.Content,
                                m.MessageType,
                                m.MediaUrl,
                                m.Timestamp,
                                // Sohbetteki karşı tarafın bilgileri
                                Name = _context.ChatParticipants
                                    .Where(cp => cp.ChatId == chatId && cp.UserId != userId)
                                    .Select(cp => cp.User.Name)
                                    .FirstOrDefault(),
                                SurName = _context.ChatParticipants
                                    .Where(cp => cp.ChatId == chatId && cp.UserId != userId)
                                    .Select(cp => cp.User.SurName)
                                    .FirstOrDefault(),
                                LastSeen = _context.ChatParticipants
                                    .Where(cp => cp.ChatId == chatId && cp.UserId != userId)
                                    .Select(cp => cp.User.LastSeen)
                                    .FirstOrDefault(),
                                Tcno = _context.ChatParticipants
                                    .Where(cp => cp.ChatId == chatId && cp.UserId != userId)
                                    .Select(cp => cp.User.Tcno)
                                    .FirstOrDefault(),
                                SicilNo = _context.ChatParticipants
                                    .Where(cp => cp.ChatId == chatId && cp.UserId != userId)
                                    .Select(cp => cp.User.SicilNo)
                                    .FirstOrDefault(),
                                ProfileImage = _context.ChatParticipants
                                    .Where(cp => cp.ChatId == chatId && cp.UserId != userId)
                                    .Select(cp => cp.User.ProfileImage)
                                    .FirstOrDefault(),
                                ReceiveId = _context.ChatParticipants
                                    .Where(cp => cp.ChatId == chatId && cp.UserId != m.SenderId) // Gönderen hariç diğer kişi
                                    .Select(cp => cp.User.Id)
                                    .FirstOrDefault(),
                            })
                            .FirstOrDefault() // Sadece en son mesajı getir
                    )
                    .Where(m => m != null) // Null olanları filtrele
                    .ToListAsync(); // Liste olarak dön

                if (messages == null || !messages.Any())
                    return NotFound("Mesaj bulunamadı.");

                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Bir hata oluştu: {ex.Message}");
            }
        }





        [HttpGet("GetMessages/{senderId}/{receiveId}")]
        public async Task<IActionResult> GetMessages(Guid senderId, Guid receiveId)
        {
            try
            {
                var chatIds = await _context.ChatParticipants
                    .Where(cp => cp.UserId == senderId || cp.UserId == receiveId) // Kullanıcıların ortak sohbetlerini al
                    .GroupBy(cp => cp.ChatId)
                    .Where(g => g.Count() > 1) // İki kişinin de dahil olduğu sohbetleri filtrele
                    .Select(g => g.Key)
                    .ToListAsync();

                var messages = await _context.Messages
                    .Where(m => chatIds.Contains(m.ChatId)) // Sadece ortak sohbetlerdeki mesajları al
                    .OrderBy(m => m.Timestamp) // Tarihe göre sırala (eski mesajdan yeni mesaja doğru)
                    .Select(m => new
                    {
                        m.Id,
                        m.SenderId,
                        m.Content,
                        m.MessageType,
                        m.MediaUrl,
                        m.Timestamp,
                        ChatId = m.ChatId,
                        // Mesajın alıcısını belirle (Gönderenin zıttı olan kişi)
                        ReceiveId = (m.SenderId == senderId) ? receiveId : senderId,
                        Name = _context.Users
                            .Where(u => u.Id == ((m.SenderId == senderId) ? receiveId : senderId))
                            .Select(u => u.Name)
                            .FirstOrDefault(),
                        SurName = _context.Users
                            .Where(u => u.Id == ((m.SenderId == senderId) ? receiveId : senderId))
                            .Select(u => u.SurName)
                            .FirstOrDefault(),
                        LastSeen = _context.Users
                            .Where(u => u.Id == ((m.SenderId == senderId) ? receiveId : senderId))
                            .Select(u => u.LastSeen)
                            .FirstOrDefault(),
                        Tcno = _context.Users
                            .Where(u => u.Id == ((m.SenderId == senderId) ? receiveId : senderId))
                            .Select(u => u.Tcno)
                            .FirstOrDefault(),
                        SicilNo = _context.Users
                            .Where(u => u.Id == ((m.SenderId == senderId) ? receiveId : senderId))
                            .Select(u => u.SicilNo)
                            .FirstOrDefault(),
                        ProfileImage = _context.Users
                            .Where(u => u.Id == ((m.SenderId == senderId) ? receiveId : senderId))
                            .Select(u => u.ProfileImage)
                            .FirstOrDefault(),
                    })
                    .ToListAsync();

                if (messages == null || !messages.Any())
                    return NotFound("Mesaj bulunamadı.");

                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Bir hata oluştu: {ex.Message}");
            }
        }



        // Mesaj durumunu güncelle
        [HttpPost("UpdateMessageStatus")]
        public async Task<IActionResult> UpdateMessageStatus([FromBody] UpdateMessageStatusDto request)
        {
            try
            {
                var messageStatus = await _context.MessageStatuses
                    .FirstOrDefaultAsync(ms => ms.MessageId == request.MessageId && ms.ReceiverId == request.ReceiverId);

                if (messageStatus == null)
                {
                    return NotFound("Mesaj durumu bulunamadı.");
                }

                messageStatus.Status = request.Status;
                await _context.SaveChangesAsync();

                return Ok("Mesaj durumu güncellendi.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Bir hata oluştu: {ex.Message}");
            }
        }
        [HttpGet("GetUsers")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _context.Users.ToListAsync();
                return Ok(users);   
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Bir hata oluştu: {ex.Message}");
            }
        }


    }
}

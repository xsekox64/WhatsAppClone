using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using WhatsAppClone.Authentincation;
using WhatsAppClone.DTOs;
using WhatsAppClone.Models;


namespace WhatsAppClone.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IValidator<RegisterDto> _validator;
        private readonly IValidator<LoginDto> _loginvalidator;
        private readonly EkikWhatsappContext _context;

        public AuthController(IValidator<RegisterDto> validator, IValidator<LoginDto> loginvalidator, EkikWhatsappContext context)
        {
            _validator = validator;
            _loginvalidator = loginvalidator;
            _context = context;
        }

        [HttpPost]
        public IActionResult Login(LoginDto request)
        {
            var validationResult = _loginvalidator.Validate(request);

            if (!validationResult.IsValid)
            {
                return StatusCode(403, validationResult.Errors);
            }

            User user = _context.Users.FirstOrDefault(p => p.Email == request.Email && p.Password == request.Password);
            if (user == null) throw new Exception("Kullanıcı bulunamadı");

            if (user.Password != request.Password) throw new Exception("Şifre doğru değil");

            List<string> roles = ["Admin", "SuperAdmin"];

            var token = JwtProvider.CreateToken(user, roles);
            var response = new
            {
                user.Id,
                token.Token,
                token.RefreshToken,
                token.RefreshTokenExpires,
                user.PhoneNumber,
                user.Tcno,
                user.SicilNo,
                user.Name,
                user.SurName,
                user.ProfileImage,
                user.Status,
                user.LastSeen,
                user.IsOnline,
                user.Email  ,
                user.isAddToGroup
            };

            return Ok(response);
        }


        [HttpPost("{refreshToken}")]
        public IActionResult LoginWithRefreshToken(string refreshToken)
        {
            User user = _context.Users.FirstOrDefault(p => p.RefreshToken == refreshToken);
            if (user == null) throw new Exception("Kullanıcı bulunamadı");
            if (user.RefreshTokenExpires < DateTime.Now) throw new Exception("Refresh token süresi dolmuş!");

            List<string> roles = ["Admin", "SuperAdmin"];

            var response = JwtProvider.CreateToken(user, roles);
            return Ok(response);
        }
    }
}

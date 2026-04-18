using E_Commerce.Application.DTOs.Auth;
using E_Commerce.Application.interfaces;
using E_Commerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Commerce.Infrastructure.Services
{
     
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ArgumentException("Email is required.");

            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("Password is required.");

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email == request.Email);

            if (user is null)
                return null;

            // مؤقتًا: مقارنة مباشرة لأننا لم نطبق hashing الحقيقي بعد
            if (user.PasswordHash != request.Password)
                return null;

            return new LoginResponse
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Message = "Login successful."
            };
        }
    }
}

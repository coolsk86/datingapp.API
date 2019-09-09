using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DatingApp.API.Models;
using DatingApp.API.Repository.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _repository;
        private readonly IConfiguration _config;

        public AuthController(IAuthRepository repository, IConfiguration config)
        {
            _repository = repository;
            _config = config;
        }

        [HttpPost]
        [Route("register")]
        public async Task<ActionResult> Register(UserDto user)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            user.UserName = user.UserName.ToLower();

            if (await _repository.UserExists(user.UserName))
                return BadRequest("User already exists!!");

            var userToCreate = new User()
            {
                UserName = user.UserName
            };

            var createdUser = await _repository.Register(userToCreate, user.Password);
            return StatusCode(201);
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login(UserLoginDto login)
        {
            var userFromResponse = await _repository.Login(login.UserName.ToLower(), login.Password);

            if (userFromResponse == null)
                return Unauthorized("Invalid Credentials");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userFromResponse.Id.ToString()),
                new Claim(ClaimTypes.Name, userFromResponse.UserName.ToString())
            };

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Token").Value));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var tokenDescription = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddMinutes(60),
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescription);

            return Ok(new
            {
                token = tokenHandler.WriteToken(token)
            });
        }
    }
}
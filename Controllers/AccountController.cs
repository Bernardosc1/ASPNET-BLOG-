using Blog.Data;
using Blog.Extensions;
using Blog.Models;
using Blog.Services;
using Blog.ViewModels;
using Blog.ViewModels.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureIdentity.Password;
using System.Runtime.Intrinsics;
using System.Text.RegularExpressions;

namespace Blog.Controllers
{

    [ApiController]
    public class AccountController : ControllerBase
    {
        [HttpPost("v1/accounts")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model,
            [FromServices] EmailService emailService,
            [FromServices] BlogDataContext context)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ResultViewModel<string>(ModelState.GetErrors()));

            var user = new User
            {
                Name = model.Name,
                Email = model.Email,
                Slug = model.Email.Replace("@", "-").Replace(".", "-"),
            };

            var password = PasswordGenerator.Generate(25); //Gera uma senha aleatória
            user.PasswordHash = PasswordHasher.Hash(password); //Gera um hash da senha

            try
            {
                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();

                emailService.Send(
                    user.Name, 
                    user.Email, 
                    "Bem-vindo ao Blog", 
                    $"<h1>Olá {user.Name}, seja bem-vindo ao Blog!</h1><p>Seu usuário é: {user.Email}</p><p>Sua senha é: {password}</p>");

                return Ok(new ResultViewModel<dynamic>(new
                {
                    user = user.Email,
                    password
                }));
            }
            catch (DbUpdateException)
            {
                return StatusCode(400, new ResultViewModel<string>("05X99 - Esse E-mail já está cadastrado!"));
            }
            catch
            {
                return StatusCode(500, new ResultViewModel<string>("05X04 - Falha interna no sevidor!"));
            }
        }

        [HttpPost("v1/accounts/login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model,
            [FromServices] BlogDataContext context,
            [FromServices] TokenService tokenService)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ResultViewModel<string>(ModelState.GetErrors()));

            var user = await context
                .Users.AsNoTracking()
                .Include(x => x.Roles)
                .FirstOrDefaultAsync(x => x.Email == model.Email);

            if (user == null)
                return StatusCode(401, new ResultViewModel<string>("Usuário ou senha inválido!"));

            if (!PasswordHasher.Verify(user.PasswordHash, model.Password))
                return StatusCode(401, new ResultViewModel<string>("Usuário ou senha inválido!"));

            try
            {
                var token = tokenService.GenerateToken(user);
                return Ok(new ResultViewModel<string>(token, null));
            }
            catch
            {
                return StatusCode(500, new ResultViewModel<string>("05X04 - Falha interna no sevidor!"));
            }

        }


        [Authorize]
        [HttpPost("v1/accounts/upload-image")]
        public async Task<IActionResult> UploadImage([FromBody] UploadImageViewModel model,
            [FromServices] BlogDataContext context)
        {
            var fileName = $"{Guid.NewGuid().ToString()}.jpg";
            var data = new Regex(@"data:image;base64,")
                 .Replace(model.Base64Image, "");
            var bytes = Convert.FromBase64String(data);

            try
            {
                await System.IO.File.WriteAllBytesAsync($"wwwroot/images/{fileName}", bytes);
            }
            catch
            {
                return StatusCode(500, new ResultViewModel<string>("05X04 - Falha interna no sevidor!"));
            }

            var user = await context.Users.FirstOrDefaultAsync(x => x.Email == User.Identity.Name);

            if (user == null)
                return NotFound(new ResultViewModel<User>("Usuário não encontrado"));

            user.Image = $"https://localhost:0000/images{fileName}";

            try
            {
                context.Users.Update(user);
                await context.SaveChangesAsync();
            }
            catch
            {
                return StatusCode(500, new ResultViewModel<string>("05X04 - Falha interna no sevidor!"));
            }

            return Ok(new ResultViewModel<string>("Imagem atualizada com sucesso!"));
        }
        // EU QUE FIZ AQUI EM BAIXO --------------------------------------------------------------------------------
        [HttpGet("v1/accounts/get")]
        public async Task<IActionResult> GetAccouts(
           [FromServices] BlogDataContext context)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ResultViewModel<string>(ModelState.GetErrors()));

            try
            {
                var users = await context
                .Users
                .AsNoTracking()
                .ToListAsync();

            if (users == null)
                return StatusCode(401, new ResultViewModel<string>("Sem usuarios encontrados!"));

            return Ok(new ResultViewModel<List<User>>(users));
           
            }
            catch
            {
                return StatusCode(500, new ResultViewModel<string>("05X04 - Falha interna no sevidor!"));
            }

        }
    }
}

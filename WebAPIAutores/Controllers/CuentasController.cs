using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebAPIGSC.DTOs;

namespace WebAPIGSC.Controllers.V1
{
    [ApiController]
    [Route("api/[controller]")]
    public class CuentasController: ControllerBase
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly IConfiguration configuration;
        private readonly SignInManager<IdentityUser> signInManager;
    
        public CuentasController(UserManager<IdentityUser> userManager,
            IConfiguration configuration,
            SignInManager<IdentityUser> signInManager,
            IDataProtectionProvider dataProtectionProvider
            )
        {
            this.userManager = userManager;
            this.configuration = configuration;
            this.signInManager = signInManager;
        }

        [HttpPost("registrar")] // api/cuentas/registrar
        public async Task<ActionResult<RespuestaAutenticacion>> Registrar(string email,
            string password)
        {
            var usuario = new IdentityUser { UserName = email, 
                Email = email};
            var resultado = await userManager.CreateAsync(usuario, password);

            if (resultado.Succeeded)
            {
                return await ConstruirToken(email);
            }
            else
            {
                return BadRequest(resultado.Errors);
            }            
        }

        [HttpPost("login")]
        public async Task<ActionResult<RespuestaAutenticacion>> Login(string email,
            string password)
        {
            var resultado = await signInManager.PasswordSignInAsync(email,
                password, isPersistent: false, lockoutOnFailure: false);

            if (resultado.Succeeded)
            {
                
                return await ConstruirToken(email);
            }
            else
            {
                return BadRequest("Login incorrecto");
            }            
        }

        private async Task<RespuestaAutenticacion> ConstruirToken(string email)
        {
            var claims = new List<Claim>()
            {
                new Claim("email", email),
            };

            var usuario = await userManager.FindByEmailAsync(email);
            var claimsDB = await userManager.GetClaimsAsync(usuario);

            claims.AddRange(claimsDB);

            var llave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["llavejwt"]));
            var creds = new SigningCredentials(llave, SecurityAlgorithms.HmacSha256);

            var expiracion = DateTime.UtcNow.AddYears(1);

            var securityToken = new JwtSecurityToken(
                issuer: null, 
                audience: null, 
                claims: claims,
                expires: expiracion, 
                signingCredentials: creds);

            return new RespuestaAutenticacion()
            {
                Token = new JwtSecurityTokenHandler().WriteToken(securityToken),
                Expiracion = expiracion
            };
        }

        [HttpPost("HacerAdmin")]
        public async Task<ActionResult> HacerAdmin(string email)
        {
            var usuario = await userManager.FindByEmailAsync(email);
            await userManager.AddClaimAsync(usuario, new Claim("admin", "1"));
            return NoContent();
        }


        [HttpPost("RolCalleLarga")]
        public async Task<ActionResult> RolAdmin(string email)
        {
            var usuario = await userManager.FindByEmailAsync(email);
            await userManager.AddClaimAsync(usuario, new Claim("Callelarga", "1"));
            return NoContent();
        }
    }
}

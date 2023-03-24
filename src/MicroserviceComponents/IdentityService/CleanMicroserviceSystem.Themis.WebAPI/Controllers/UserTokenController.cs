﻿using System.Security.Claims;
using CleanMicroserviceSystem.Authentication.Application;
using CleanMicroserviceSystem.Oceanus.Domain.Abstraction.Models;
using CleanMicroserviceSystem.Themis.Contract.Users;
using CleanMicroserviceSystem.Themis.Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CleanMicroserviceSystem.Themis.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserTokenController : ControllerBase
{
    private readonly ILogger<UserTokenController> logger;
    private readonly IJwtBearerTokenGenerator jwtBearerTokenGenerator;
    private readonly UserManager<OceanusUser> userManager;
    private readonly RoleManager<OceanusRole> roleManager;
    private readonly SignInManager<OceanusUser> signInManager;

    public UserTokenController(
        ILogger<UserTokenController> logger,
        IJwtBearerTokenGenerator jwtBearerTokenGenerator,
        UserManager<OceanusUser> userManager,
        RoleManager<OceanusRole> roleManager,
        SignInManager<OceanusUser> signInManager)
    {
        this.logger = logger;
        this.jwtBearerTokenGenerator = jwtBearerTokenGenerator;
        this.userManager = userManager;
        this.roleManager = roleManager;
        this.signInManager = signInManager;
    }

    private async Task<IEnumerable<Claim>> GetClaimsAsync(OceanusUser user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName)
        };

        if (!string.IsNullOrEmpty(user.Email))
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        if (!string.IsNullOrEmpty(user.PhoneNumber))
            claims.Add(new Claim(ClaimTypes.HomePhone, user.PhoneNumber));

        var userClaims = await this.userManager.GetClaimsAsync(user);
        claims.AddRange(userClaims);

        var roleNames = await this.userManager.GetRolesAsync(user);
        claims.AddRange(roleNames.Select(role => new Claim(ClaimTypes.Role, role)));
        var roleClaims = roleNames
            .Select(roleName => this.roleManager.FindByNameAsync(roleName).Result)
            .OfType<OceanusRole>()
            .SelectMany(role => this.roleManager.GetClaimsAsync(role).Result)
            .DistinctBy(claim => (claim.Type, claim.Value));
        claims.AddRange(roleClaims);

        return claims;
    }

    /// <summary>
    /// Login user
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<string>> Post([FromBody] UserTokenLoginRequest request)
    {
        this.logger.LogInformation($"Sign in User: {request.UserName}");
        var result = await this.signInManager.PasswordSignInAsync(request.UserName, request.Password, true, false);
        if (!result.Succeeded)
        {
            var commonResult = new CommonResult<string>();
            if (result.IsNotAllowed) commonResult.Errors.Add(new(nameof(result.IsNotAllowed)));
            if (result.IsLockedOut) commonResult.Errors.Add(new(nameof(result.IsLockedOut)));
            if (result.RequiresTwoFactor) commonResult.Errors.Add(new(nameof(result.RequiresTwoFactor)));
            return this.BadRequest(commonResult);
        }

        var user = await this.userManager.FindByNameAsync(request.UserName);
        var claims = await this.GetClaimsAsync(user!);
        var token = this.jwtBearerTokenGenerator.GenerateUserSecurityToken(claims);
        return this.Ok(token);
    }

    /// <summary>
    /// Refresh user token
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<string>> Put()
    {
        var userName = this.HttpContext.User?.Identity?.Name;
        this.logger.LogInformation($"Refresh User token: {userName}");
        if (string.IsNullOrEmpty(userName))
            return this.BadRequest();

        var user = await this.userManager.FindByNameAsync(userName);
        var claims = await this.GetClaimsAsync(user!);
        var token = this.jwtBearerTokenGenerator.GenerateUserSecurityToken(claims);
        return this.Ok(token);
    }

    /// <summary>
    /// Logout user
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Delete()
    {
        var userName = this.HttpContext.User?.Identity?.Name;
        this.logger.LogInformation($"Sign out User: {userName}");
        if (string.IsNullOrEmpty(userName))
            return this.BadRequest();
        await this.signInManager.SignOutAsync();
        return this.Ok();
    }
}

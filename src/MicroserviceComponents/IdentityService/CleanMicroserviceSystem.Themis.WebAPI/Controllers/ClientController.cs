﻿using System.Security.Claims;
using CleanMicroserviceSystem.Authentication.Domain;
using CleanMicroserviceSystem.DataStructure;
using CleanMicroserviceSystem.Oceanus.Domain.Abstraction.Models;
using CleanMicroserviceSystem.Themis.Application.Services;
using CleanMicroserviceSystem.Themis.Contract.Claims;
using CleanMicroserviceSystem.Themis.Contract.Clients;
using CleanMicroserviceSystem.Themis.Domain.Entities.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanMicroserviceSystem.Themis.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientController : ControllerBase
{
    private readonly ILogger<ClientController> logger;
    private readonly IApiResourceManager apiResourceManager;
    private readonly IClientManager clientManager;

    public ClientController(
        ILogger<ClientController> logger,
        IApiResourceManager apiResourceManager,
        IClientManager clientManager)
    {
        this.logger = logger;
        this.apiResourceManager = apiResourceManager;
        this.clientManager = clientManager;
    }

    #region Clients

    /// <summary>
    /// Get client information
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}")]
    [Authorize(Policy = IdentityContract.ThemisAPIReadPolicyName)]
    public async Task<ActionResult<ClientInformationResponse>> Get(int id)
    {
        this.logger.LogInformation($"Get Client: {id}");
        var client = await this.clientManager.FindByIdAsync(id);
        return client is null
            ? this.NotFound()
            : this.Ok(new ClientInformationResponse()
            {
                Id = client.Id,
                Name = client.Name,
                Enabled = client.Enabled,
                Description = client.Description,
            });
    }

    /// <summary>
    /// Search clients information
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpGet(nameof(Search))]
    [Authorize(Policy = IdentityContract.ThemisAPIReadPolicyName)]
    public async Task<ActionResult<PaginatedEnumerable<ClientInformationResponse>>> Search([FromQuery] ClientSearchRequest request)
    {
        this.logger.LogInformation($"Search Clients: {request.Id}, {request.Name}, {request.Enabled}");
        var result = await this.clientManager.SearchAsync(
            request.Id, request.Name, request.Enabled, request.Start, request.Count);
        var clients = result.Values.Select(client => new ClientInformationResponse()
        {
            Id = client.Id,
            Name = client.Name,
            Enabled = client.Enabled,
            Description = client.Description,
        });
        var paginatedClients = new PaginatedEnumerable<ClientInformationResponse>(
            clients, result.StartItemIndex, result.PageSize, result.OriginItemCount);
        return this.Ok(paginatedClients);
    }

    /// <summary>
    /// Create client information
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [Authorize(Policy = IdentityContract.ThemisAPIWritePolicyName)]
    public async Task<ActionResult<ClientInformationResponse>> Post([FromBody] ClientCreateRequest request)
    {
        this.logger.LogInformation($"Create Client: {request.Name}");
        var newClient = new Client()
        {
            Name = request.Name,
            Enabled = request.Enabled,
            Description = request.Description,
        };
        var result = await this.clientManager.CreateAsync(newClient);
        if (!result.Succeeded)
        {
            return this.BadRequest(result);
        }
        else
        {
            newClient = await this.clientManager.FindByIdAsync(newClient.Id);
            return this.Ok(new ClientInformationResponse()
            {
                Id = newClient.Id,
                Name = newClient.Name,
                Enabled = newClient.Enabled,
                Description = newClient.Description,
            });
        }
    }

    /// <summary>
    /// Update client information
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("{id}")]
    [Authorize(Policy = IdentityContract.ThemisAPIWritePolicyName)]
    public async Task<ActionResult<CommonResult>> Put(int id, [FromBody] ClientUpdateRequest request)
    {
        this.logger.LogInformation($"Update Client: {id}");
        var client = await this.clientManager.FindByIdAsync(id);
        if (client is null)
            return this.NotFound();

        if (request.Enabled.HasValue)
        {
            client.Enabled = request.Enabled.Value;
        }
        if (request.Description is not null)
        {
            client.Description = request.Description;
        }
        if (request.Name is not null)
        {
            client.Name = request.Name;
        }
        if (request.Secret is not null)
        {
            client.Secret = request.Secret;
        }

        var commonResult = await this.clientManager.UpdateAsync(client);
        return commonResult.Succeeded ? this.Ok(commonResult) : this.BadRequest(commonResult);
    }

    /// <summary>
    /// Delete client information
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    [Authorize(Policy = IdentityContract.ThemisAPIWritePolicyName)]
    public async Task<IActionResult> Delete(int id)
    {
        this.logger.LogInformation($"Delete Client: {id}");
        var client = await this.clientManager.FindByIdAsync(id);
        if (client is null)
            return this.NotFound();
        await this.clientManager.DeleteAsync(client);
        return this.Ok();
    }
    #endregion

    #region ClientClaims

    /// <summary>
    /// Get client claims
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}/Claims")]
    [Authorize(Policy = IdentityContract.ThemisAPIReadPolicyName)]
    public async Task<ActionResult<IEnumerable<ClaimInformationResponse>>> GetClaims(int id)
    {
        this.logger.LogInformation($"Get Client Claims: {id}");
        var client = await this.clientManager.FindByIdAsync(id);
        if (client is null)
            return this.NotFound();

        var result = await this.clientManager.GetClaimsAsync(client.Id);
        var claims = result.Select(claim => new ClaimInformationResponse()
        {
            Type = claim.ClaimType,
            Value = claim.ClaimValue
        });
        return this.Ok(claims);
    }

    /// <summary>
    /// Update client claims
    /// </summary>
    /// <param name="id"></param>
    /// <param name="requests"></param>
    /// <returns></returns>
    [HttpPut("{id}/Claims")]
    [Authorize(Policy = IdentityContract.ThemisAPIWritePolicyName)]
    public async Task<IActionResult> PutClaims(int id, [FromBody] IEnumerable<ClaimsUpdateRequest> requests)
    {
        this.logger.LogInformation($"Update Client Claims: {id}");
        var client = await this.clientManager.FindByIdAsync(id);
        if (client is null)
            return this.NotFound();

        var existingClaims = await this.clientManager.GetClaimsAsync(client.Id);
        var existingClaimSet = existingClaims.Select(claim => (claim.ClaimType, claim.ClaimValue)).ToHashSet();
        var requestClaimSet = requests.Select(claim => (claim.Type, claim.Value)).ToHashSet();

        var claimsToRemove = existingClaims
            .Where(claim => !requestClaimSet.Contains((claim.ClaimType, claim.ClaimValue)))
            .Select(claim => claim.Id)
            .ToArray();
        if (claimsToRemove.Any())
        {
            _ = await this.clientManager.RemoveClaimsAsync(claimsToRemove);
        }

        var claimsToAdd = requests
            .Where(claim => !existingClaimSet.Contains((claim.Type, claim.Value)))
            .Select(claim => new ClientClaim() { ClientId = client.Id, ClaimType = claim.Type, ClaimValue = claim.Value })
            .ToArray();
        if (claimsToAdd.Any())
        {
            _ = await this.clientManager.AddClaimsAsync(claimsToAdd);
        }

        return this.Ok();
    }

    /// <summary>
    /// Add client claims
    /// </summary>
    /// <param name="id"></param>
    /// <param name="requests"></param>
    /// <returns></returns>
    [HttpPost("{id}/Claims")]
    [Authorize(Policy = IdentityContract.ThemisAPIWritePolicyName)]
    public async Task<IActionResult> PostClaims(int id, [FromBody] IEnumerable<ClaimsUpdateRequest> requests)
    {
        this.logger.LogInformation($"Create Client Claims: {id}");
        var client = await this.clientManager.FindByIdAsync(id);
        if (client is null)
            return this.NotFound();

        var existingClaims = await this.clientManager.GetClaimsAsync(client.Id);
        var existingClaimSet = existingClaims.Select(claim => (claim.ClaimType, claim.ClaimValue)).ToHashSet();
        var claimsToAdd = requests
            .Where(claim => !existingClaimSet.Contains((claim.Type, claim.Value)))
            .Select(claim => new ClientClaim() { ClientId = client.Id, ClaimType = claim.Type, ClaimValue = claim.Value })
            .ToArray();
        if (claimsToAdd.Any())
        {
            _ = await this.clientManager.AddClaimsAsync(claimsToAdd);
            return this.Ok();
        }

        return this.NoContent();
    }

    /// <summary>
    /// Delete client claims
    /// </summary>
    /// <param name="id"></param>
    /// <param name="requests"></param>
    /// <returns></returns>
    [HttpDelete("{id}/Claims")]
    [Authorize(Policy = IdentityContract.ThemisAPIWritePolicyName)]
    public async Task<IActionResult> DeleteClaims(int id, [FromBody] IEnumerable<ClaimsUpdateRequest> requests)
    {
        this.logger.LogInformation($"Delete Client Claims: {id}");
        var client = await this.clientManager.FindByIdAsync(id);
        if (client is null)
            return this.NotFound();

        var existingClaims = await this.clientManager.GetClaimsAsync(client.Id);
        var claimsToRemoveSet = requests.Select(claim => (claim.Type, claim.Value)).ToHashSet();
        var claimsToRemove = existingClaims
            .Where(claim => claimsToRemoveSet.Contains((claim.ClaimType, claim.ClaimValue)))
            .ToArray();
        if (claimsToRemove.Any())
        {
            var claimToRemoveIds = claimsToRemove.Select(claim => claim.Id);
            _ = await this.clientManager.RemoveClaimsAsync(claimToRemoveIds);
            return this.Ok();
        }

        return this.NoContent();
    }
    #endregion
}

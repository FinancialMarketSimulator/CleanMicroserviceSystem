﻿using Microsoft.AspNetCore.Authorization;

namespace CleanMicroserviceSystem.Authentication.Domain;

public static class IdentityContract
{
    #region SchemeType

    public const string JwtAuthenticationType = $"Jwt";
    public const string UserJwtBearerScheme = $"JwtBearer_User";
    public const string ClientJwtBearerScheme = $"JwtBearer_Client";
    #endregion

    #region Users

    public const int SuperUserId = 1;
    public const string SuperUser = "Leon";
    public const int CommonUserId = 2;
    public const string CommonUser = "Mathilda";
    #endregion

    #region Roles

    public const int AdministratorRoleId = 1;
    public const string AdministratorRole = "Administrator";
    public const int OperatorRoleId = 2;
    public const string OperatorRole = "Operator";
    #endregion

    #region Headers

    public const string AuthenticationSchemeHeaderName = "authentication_scheme";
    public const string ClientAuthenticationSchemeHeaderValue = "client";
    #endregion

    #region Clients

    public const string TethysClient = "Tethys";
    #endregion

    #region Accesses

    public const string Read = "Read";
    public const string Write = "Write";
    #endregion

    #region ApiResources

    public const string ThemisAPIResource = "ThemisAPI";
    #endregion

    #region PolicyNames

    public const string ThemisAPIReadPolicyName = $"{ThemisAPIResource}_{Read}";
    public const string ThemisAPIWritePolicyName = $"{ThemisAPIResource}_{Write}";
    #endregion

    #region PolicyBuilders

    public static AuthorizationPolicy ThemisAPIReadPolicy =
        new AuthorizationPolicyBuilder().RequireClaim(ThemisAPIResource, Read).Build();
    public static AuthorizationPolicy ThemisAPIWritePolicy =
        new AuthorizationPolicyBuilder().RequireClaim(ThemisAPIResource, Write).Build();
    #endregion
}

using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Nudges.Auth.Web;

public static class ServerBearerDefaults {
    public const string AuthenticationScheme = "Server";
}

public static class SimpleBearerDefaults {
    public const string AuthenticationScheme = "Simple";
}

public static class AddServerBearerExtensions {
    public static AuthenticationBuilder AddServerBearer(this AuthenticationBuilder builder, string signingKey) {
        builder.AddJwtBearer(ServerBearerDefaults.AuthenticationScheme, options => {
            options.Authority = null;
            options.UseSecurityTokenValidators = true;
            options.RequireHttpsMetadata = false;
            //options.MapInboundClaims = false;
            options.Events = new JwtBearerEvents {
                // here for debugging purposes
                OnMessageReceived = context => Task.CompletedTask,
                OnAuthenticationFailed = context => {
                    context.NoResult();
                    return Task.CompletedTask;
                },
                OnChallenge = context => Task.CompletedTask,
                OnForbidden = context => Task.CompletedTask,
                OnTokenValidated = context => Task.CompletedTask
            };
            options.TokenValidationParameters = new TokenValidationParameters {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                ValidateAudience = false,
                ValidateIssuer = false,
                RequireExpirationTime = false,
                RequireSignedTokens = false,
                SignatureValidator = new SignatureValidator((token, validationParameters) => {
                    var auth = new SimpleAuthenticator(validationParameters.IssuerSigningKey);
                    var principal = auth.ValidateToken(token);
                    if (principal is null) {
                        return null;
                    }
                    // TODO: more validation
                    return new JwtSecurityToken(token);
                })
            };
        });
        return builder;
    }

    public static AuthenticationBuilder AddSimpleBearer(this AuthenticationBuilder builder, string signingKey) {
        builder.AddJwtBearer(SimpleBearerDefaults.AuthenticationScheme, options => {
            options.Authority = null;
            options.UseSecurityTokenValidators = true;
            options.RequireHttpsMetadata = false;
            //options.MapInboundClaims = false;
            options.Events = new JwtBearerEvents {
                // here for debugging purposes
                OnMessageReceived = context => Task.CompletedTask,
                OnAuthenticationFailed = context => {
                    context.NoResult();
                    return Task.CompletedTask;
                },
                OnChallenge = context => Task.CompletedTask,
                OnForbidden = context => Task.CompletedTask,
                OnTokenValidated = context => Task.CompletedTask
            };
            options.TokenValidationParameters = new TokenValidationParameters {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                ValidateAudience = false,
                ValidateIssuer = false,
                RequireExpirationTime = false,
                RequireSignedTokens = false,
                SignatureValidator = new SignatureValidator((token, validationParameters) => {
                    var auth = new SimpleAuthenticator(validationParameters.IssuerSigningKey);
                    var principal = auth.ValidateToken(token);
                    if (principal is null) {
                        return null;
                    }
                    // TODO: more validation
                    return new JwtSecurityToken(token);
                })
            };
        });
        return builder;
    }
}



using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;

namespace d360.web.Extensions
{
    public static class SecurityExtensions
    {
        /// <summary>
        /// Extension method that validates the identity token from the OpenID-based IdP. 
        /// Allows you to choose what type of validations to perform on the key, as well 
        /// as providing the common values of audience, issuer, etc.
        /// </summary>
        /// <param name="identityToken">This is the parameter the extension method roots itself from. It it the string value of the identity token from the JWT.</param>
        /// <param name="nameClaimType">The property name that acts as the identifier of the user on the IdP.</param>
        /// <param name="audience">The audience string value. For OKTA, this is the application's audience value.</param>
        /// <param name="shouldValidateAudience">A boolean to indicate whether the audience (aud on token) should be validated.</param>
        /// <param name="issuer">The issuer value from the IdP's definition on client application.</param>
        /// <param name="shouldValidateIssuer">A boolean to indicate whether the issuer (iss on token) should be validated.</param>
        /// <param name="keys">The web keys used for signing.</param>
        /// <param name="shouldValidateIssuerSigningKeys">A boolean to indicate whether the signed JWT should be validated against provided signing keys.</param>
        /// <param name="shouldRequireSignedTokens">A boolean to indicate whether the JWT must be signed.</param>
        /// <param name="shouldRequireExpirationTime">A boolean to indicate whether the JWT expiration should be required.</param>
        /// <param name="shouldValidateLifetime">A boolean to indicate whether the JWT lifetime should be validated.</param>
        /// <returns></returns>
        public static ClaimsPrincipal ValidateJwtIdentityToken(this string identityToken,
            string nameClaimType, 
            string audience, bool shouldValidateAudience,
            string issuer, bool shouldValidateIssuer,
            IList<IdentityModel.Jwk.JsonWebKey> keys,
            bool shouldValidateIssuerSigningKeys, bool shouldRequireSignedTokens,
            bool shouldRequireExpirationTime, bool shouldValidateLifetime)
        {
            var securityKeys = new List<SecurityKey>();
            foreach (var webKey in keys)
            {
                var e = IdentityModel.Base64Url.Decode(webKey.E);
                var n = IdentityModel.Base64Url.Decode(webKey.N);

                var key = new RsaSecurityKey(new RSAParameters { Exponent = e, Modulus = n })
                {
                    KeyId = webKey.Kid
                };

                securityKeys.Add(key);
            }

            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            handler.InboundClaimTypeMap.Clear();
            var parameters = new TokenValidationParameters
            {
                NameClaimType = nameClaimType,
                ValidateAudience = shouldValidateAudience,
                
                ValidateLifetime = shouldValidateLifetime,
                RequireExpirationTime = shouldRequireExpirationTime,

                ValidateIssuer = shouldValidateIssuer,     // Validate the JWT Issuer (iss) claim
                ValidIssuer = issuer,

                ValidateIssuerSigningKey = shouldValidateIssuerSigningKeys,
                IssuerSigningKeys = securityKeys,

                RequireSignedTokens = shouldRequireSignedTokens
            };
            if (!string.IsNullOrEmpty(audience))
            {
                parameters.ValidAudience = audience;
            }

            var user = handler.ValidateToken(identityToken, parameters, out var _);

            return user;
        }
    }
}
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Ocano.OcanoAD.SSOAdapter.Contracts.Models;
using Ocano.OcanoAD.SSOAdapter.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Ocano.OcanoAD.SSOAdapter.Core.Repositories
{
    public class UserRepository
    {
        private readonly GraphServiceClient _graphServiceClient;
        private readonly AzureAdB2CConfiguration _configuration;
        private readonly ILogger<UserRepository> _logger;
        private readonly char _userPrincipalNameInvalidCharacterReplacement;
        private readonly TemporaryPasswordService _temporaryPasswordService;
        private readonly Regex _nonLetterOrNumberPattern;
        private readonly string _specialCharacters;

        public UserRepository(
            GraphServiceClient graphServiceClient,
            AzureAdB2CConfiguration configuration,
            ILogger<UserRepository> logger,
            TemporaryPasswordService temporaryPasswordService)
        {
            _graphServiceClient = graphServiceClient;
            _configuration = configuration;
            _logger = logger;
            _temporaryPasswordService = temporaryPasswordService;
            _userPrincipalNameInvalidCharacterReplacement = '-';
            _nonLetterOrNumberPattern = new Regex("[^a-zA-Z0-9æÆøØåÅ]");
            _specialCharacters = "!#$%&'()*+,-./:;<=>?@[]^_`{|}~";
        }

        public async Task<User> Get(string email)
        {
            var userPrincipalName = HttpUtility.UrlEncode(UserPrincipalName(email));
            try
            {
                var result = await _graphServiceClient
                    .Users[userPrincipalName]
                    .Request()
                    .Select(UserProperties())
                    .GetAsync();

                return result;
            }
            catch (ServiceException serviceException)
            {
                var statusCode = serviceException.StatusCode;
                if (statusCode != HttpStatusCode.NotFound)
                    _logger.LogInformation(serviceException, $"Status code: {statusCode}. Unable to get user by user principal name: {userPrincipalName}");
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, $"Unable to get user by user principal name: {userPrincipalName}");
            }

            return null;
        }

        public async Task<User> Create(CreateUserRequest request)
        {
            if (request == null) return null;
            try
            {
                var user = User(request);
                var addedUser = await _graphServiceClient.Users
                    .Request()
                    .Select(UserProperties())
                    .AddAsync(user);
                return addedUser;
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, $"Unable to create user with email: {request?.EmailAddress}: {ex.Message}");
                return null;
            }
        }

        public async Task<User> Update(UpdateUserRequest request)
        {
            if (request == null) return null;
            var email = request.EmailAddress;
            if (string.IsNullOrWhiteSpace(email))return null;
            var user = await Get(email);
            if (user == null) return null;
            var userPrincipalName = user.UserPrincipalName;
            if (string.IsNullOrWhiteSpace(userPrincipalName)) return null;
            user = User(request);
            if (user == null) return null;
            if (await TryAssignByUserPrincipalName(userPrincipalName, user))
                return user;
            return null;
        }
     
        public async Task Delete(string email)
        {
            try
            {
                var user = await Get(email);
                if (user == null) return;
                var userPrincipalName = user.UserPrincipalName;
                if (string.IsNullOrWhiteSpace(userPrincipalName)) return;
                await _graphServiceClient.Users[userPrincipalName]
                    .Request()
                    .DeleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Unable to delete user by email");
            }

            return;
        }

        #region Private methods

        private string UserProperties()
        {
            var properties = new List<string>
            {
                Constants.User.Property.Id,
                Constants.User.Property.GivenName,
                Constants.User.Property.SurName,
                Constants.User.Property.Mail,
                Constants.User.Property.UserPrincipalName,
            };
            properties.AddRange(ExtensionProperties());
            return string.Join(Constants.Separator.UserProperty, properties);
        }

        private IEnumerable<string> ExtensionProperties()
        {
            return new List<string>
            {
                ExtensionsKey(Constants.User.AdditionalData.CompanyCVR)
            };
        }

        public string ExtensionsKey(string customUserAttributeName)
        {
            var extensionsAppId = _configuration?.ExtensionsAppId?.Replace(Constants.Separator.GuidGroup, string.Empty);
            return $"extension_{extensionsAppId}_{customUserAttributeName}";
        }

        private static string MailFilterQuery(string username)
        {
            return $"mail eq '{username}'";
        }

        private PasswordProfile PasswordProfile()
        {
            return new PasswordProfile
            {
                // Forcing password change results in 'expired' upon trying to login.
                // There's an open feature request for this here:
                // https://feedback.azure.com/forums/169401-azure-active-directory/suggestions/16861051-aadb2c-force-password-reset.
                ForceChangePasswordNextSignIn = false,
                Password = _temporaryPasswordService.TemporaryPassword(Constants.TemporaryPassword.SpecialCharacters, _configuration.TemporaryPasswordMinimumLength)
            };
        }

        private User User(CreateUserRequest request)
        {
            var email = request.EmailAddress;
            if (string.IsNullOrWhiteSpace(email)) return null;
            var passwordProfile = PasswordProfile();
            var userPrincipalName = UserPrincipalName(email);
            var mailNickname = MailNickname(request.GivenName, request.Surname);
            var displayName = DisplayName(request.GivenName, request.Surname);
            var identities = Identities(email);
            var additionalData = AdditionalData(request);
            var user = new User
            {
                AccountEnabled = true,
                AdditionalData = additionalData,
                DisplayName = displayName,
                GivenName = request.GivenName,
                Identities = identities,
                Mail = email,
                MailNickname = mailNickname,
                PasswordProfile = passwordProfile,
                Surname = request.Surname,
                UserPrincipalName = userPrincipalName,
            };
            return user;
        }

        private string MailNickname(string givenName, string surname)
        {
            var nickname = givenName + surname;
            nickname = _nonLetterOrNumberPattern.Replace(nickname, string.Empty);
            return nickname;
        }

        private string DisplayName(string givenName, string surname)
        {
            return $"{givenName} {surname}";
        }

        private IEnumerable<ObjectIdentity> Identities(string email)
        {
            var identities = new List<ObjectIdentity>
            {
                new ObjectIdentity
                {
                    SignInType = Constants.SignInType.EmailAddress,
                    Issuer = _configuration.Domain,
                    IssuerAssignedId = email
                }
            };
            return identities;
        }

        private Dictionary<string, object> AdditionalData(CreateUserRequest request)
        {
            var additionalData = AdditionalData(request.CompanyCVR);
            return additionalData;
        }

        private Dictionary<string, object> AdditionalData(string companyCvr)
        {
            var additionalData = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(companyCvr))
                additionalData[ExtensionsKey(Constants.User.AdditionalData.CompanyCVR)] = companyCvr;
            return additionalData;
        }

        private User User(UpdateUserRequest request)
        {
            // Only fields with new values will be updated, so we can just assign without scrutiny.
            // Assigning to existing user is not permitted
            if (request == null)
                return null;
            var mailNickname = MailNickname(request.GivenName, request.Surname);
            var displayName = DisplayName(request.GivenName, request.Surname);
            var updatedUser = new User
            {
                GivenName = request.GivenName,
                Surname = request.Surname,
                DisplayName = displayName,
                MailNickname = mailNickname,
                AdditionalData = AdditionalData(request.CompanyCVR)
            };
            return updatedUser;
        }

        private async Task<bool> TryUpdateByEmail(string email, User user)
        {
            try
            {
                email = HttpUtility.UrlEncode(email);
                var results = await _graphServiceClient.Users
                    .Request()
                    .Filter(MailFilterQuery(email))
                    .GetAsync();
                var result = results?.FirstOrDefault();
                if (result == null)
                    return false;
                var userPrincipalName = result.UserPrincipalName;
                return await TryAssignByUserPrincipalName(userPrincipalName, user);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Unable to delete user by username");
            }
            return false;
        }

        private async Task<bool> TryAssignByUserPrincipalName(string userPrincipalName, User user)
        {
            if (string.IsNullOrWhiteSpace(userPrincipalName))
                return false;
            if (user == null)
                return false;
            try
            {
                await _graphServiceClient.Users[userPrincipalName]
                    .Request()
                    .UpdateAsync(user);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Unable to delete user by user principal name");
            }
            return false;
        }

        private string UserPrincipalName(string email)
        {
            if (email == null) return null;
            var prefix = email;
            foreach (var specialCharacter in _specialCharacters)
                prefix = prefix.Replace(specialCharacter, _userPrincipalNameInvalidCharacterReplacement);
            return $"{prefix}@{_configuration.Domain}";
        }

        #endregion
    }
}

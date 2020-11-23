using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Ocano.OcanoAD.SSOAdapter.Contracts.Models;
using Ocano.OcanoAD.SSOAdapter.Core.Repositories;
using System.Threading.Tasks;

namespace Ocano.OcanoAD.SSOAdapter.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserRepository _userRepository;

        public UserController(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet("{email}")]
        public async Task<ActionResult<string>> Get(string email)
        {
            var user = await _userRepository.Get(email);
            if (user == null)
                return Ok(new { Message = "User does not exist" });
            return Ok(UserDto(user, "User found"));
        }

        [HttpPost]
        public async Task<ActionResult<string>> Create(CreateUserRequest request)
        {
            var user = await _userRepository.Create(request);
            if (user == null)
                return Ok(new { Message = "Could not create user" });
            return Ok(UserDto(user, "User created"));
        }

        [HttpPatch]
        public async Task<ActionResult<string>> Update(UpdateUserRequest request)
        {
            var user = await _userRepository.Update(request);
            if (user == null)
                return Ok(new { Message = "Could not update user" });
            user.Mail = request.EmailAddress;
            return Ok(UserDto(user, "User updated"));
        }

        [HttpDelete("{email}")]
        public async Task<ActionResult<string>> Delete(string email)
        {
            await _userRepository.Delete(email);
            return Ok();
        }

        private object UserDto(User user, string message)
        {
            return new
            {
                user.GivenName,
                user.Surname,
                CompanyCvr = user.AdditionalData.TryGetValue(
                    _userRepository.ExtensionsKey(Ocano.OcanoAD.SSOAdapter.Core.Constants.User.AdditionalData.CompanyCVR), out var obj) 
                    && obj is string value 
                    ? value 
                    : null,
                EmailAddress = user.Mail,
                Message = message
            };
        }
    }
}

namespace Ocano.OcanoAD.SSOAdapter.Contracts.Models
{
    public class CreateUserRequest
    {
        public string GivenName { get; set; }
        public string Surname { get; set; }
        public string EmailAddress { get; set; }
        public string CompanyCVR { get; set; }
    }
}

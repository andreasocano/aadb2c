namespace Ocano.OcanoAD.SSOAdapter.Contracts.Models
{
    public class UpdateUserRequest
    {
        /// <summary>
        /// Email is only used to retrieve the user. It will not be updated.
        /// </summary>
        public string EmailAddress { get; set; }

        public string GivenName { get; set; }
        public string Surname { get; set; }
        public string CompanyCVR { get; set; }
    }
}

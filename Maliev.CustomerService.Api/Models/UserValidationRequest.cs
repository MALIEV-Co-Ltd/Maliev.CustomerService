namespace Maliev.CustomerService.Api.Models
{
    public class UserValidationRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
}
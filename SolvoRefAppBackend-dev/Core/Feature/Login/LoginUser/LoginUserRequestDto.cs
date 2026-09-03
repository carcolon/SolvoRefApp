using System.ComponentModel.DataAnnotations;

namespace Core.Feature.Login.LoginUser
{
    public class LoginUserRequestDto
    {
        [Required]
        [EmailAddress(ErrorMessage = "Email format is not the correct")]
        public string Email { get; set; } = string.Empty;
    }
    public class RegisterDtLoginRequest : LoginUserRequestDto
    {
        public string FullName { get; set; } = string.Empty;
        public string PayrollCompany { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string SolId { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string PersonalId { get; set; } = string.Empty;
        public string PayrollFrequencyClassification { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}

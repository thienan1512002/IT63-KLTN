using System.ComponentModel.DataAnnotations;

namespace Web.Models.ViewModels.Identity
{
    public class ResetPasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[#$^+=!*()@%&]).{7,}$",
        ErrorMessage= "Password must contain: Minimum 7 characters at least 1 UpperCase Alphabet, 1 LowerCase Alphabet, 1 Number and 1 Special Character")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }
}
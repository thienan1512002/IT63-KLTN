using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Web.Models.ViewModels.Identity
{
    public class UpdatePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]   
        public string CurrentPass { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        
        [Required]
        [NewPasswordDifferentFromCurrent]
        [DataType(DataType.Password)]        
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[#$^+=!*()@%&]).{7,}$",
        ErrorMessage= "Password must contain: Minimum 7 characters at least 1 UpperCase Alphabet, 1 LowerCase Alphabet, 1 Number and 1 Special Character")]  
        public string NewPass { get; set; } = string.Empty;
        [DataType(DataType.Password)]
        [Compare("NewPass", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPass { get; set; } = string.Empty;

    }

     //check current password and new password different
    public class NewPasswordDifferentFromCurrentAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var changePasswordModel = (UpdatePasswordViewModel)validationContext.ObjectInstance;
            if (changePasswordModel.NewPass == changePasswordModel.CurrentPass)
            {
                return new ValidationResult("New password must be different from the current password.");
            }
            return ValidationResult.Success;
        }
    }
}
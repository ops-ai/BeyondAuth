using System.ComponentModel.DataAnnotations;

namespace Authentication.Models.PasswordReset
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string ReturnUrl { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace BeeciyeMarket.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(150)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }
        public string? City { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using RaceDayWeb.Services;

namespace RaceDayWeb.Models;

// The data annotations only check that the form is filled in properly before it is sent. Every business rule
// (duplicate emails, password rules, age limits) is still checked by the API, and its message is shown.

public class LoginViewModel
{
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Choose how you will use RaceDay.")]
    public string Role { get; set; } = Roles.Participant;

    [Required(ErrorMessage = "Enter your full name.")]
    [StringLength(100, ErrorMessage = "Full name must be 100 characters or fewer.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Type your password again.")]
    [Compare(nameof(Password), ErrorMessage = "The passwords don't match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Organisation name must be 100 characters or fewer.")]
    [Display(Name = "Organisation name")]
    public string? OrganisationName { get; set; }

    [StringLength(20, ErrorMessage = "Contact number must be 20 characters or fewer.")]
    [Phone(ErrorMessage = "Enter a valid contact number.")]
    [Display(Name = "Contact number")]
    public string? ContactNumber { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>Checks the fields that only one role needs.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Role == Roles.Organiser)
        {
            if (string.IsNullOrWhiteSpace(OrganisationName))
            {
                yield return new ValidationResult("Enter your organisation's name.", [nameof(OrganisationName)]);
            }

            if (string.IsNullOrWhiteSpace(ContactNumber))
            {
                yield return new ValidationResult("Enter a contact number.", [nameof(ContactNumber)]);
            }
        }
        else if (Role == Roles.Participant)
        {
            if (DateOfBirth is null)
            {
                yield return new ValidationResult("Enter your date of birth.", [nameof(DateOfBirth)]);
            }
            else if (DateOfBirth > DateOnly.FromDateTime(DateTime.Today))
            {
                yield return new ValidationResult("Date of birth must be in the past.", [nameof(DateOfBirth)]);
            }
        }
        else
        {
            yield return new ValidationResult("Choose Organiser or Participant.", [nameof(Role)]);
        }
    }
}

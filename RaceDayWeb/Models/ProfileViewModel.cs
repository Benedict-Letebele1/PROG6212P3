using System.ComponentModel.DataAnnotations;
using RaceDayWeb.Services;

namespace RaceDayWeb.Models;

/// <summary>The profile form. Organisers edit organisation details and participants edit their date of birth.</summary>
public class ProfileViewModel : IValidatableObject
{
    public string Role { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your full name.")]
    [StringLength(100, ErrorMessage = "Full name must be 100 characters or fewer.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

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

    /// <summary>Shown on the page only. It is never posted back.</summary>
    public string? ProfilePictureUrl { get; set; }

    public bool IsOrganiser => Role == Roles.Organiser;

    public string Initials => string.Concat(FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => char.ToUpperInvariant(w[0])));

    public static ProfileViewModel From(ProfileResponse profile, string? pictureUrl) => new()
    {
        Role = profile.Role,
        FullName = profile.FullName,
        Email = profile.Email,
        OrganisationName = profile.OrganisationName,
        ContactNumber = profile.ContactNumber,
        DateOfBirth = profile.DateOfBirth,
        ProfilePictureUrl = pictureUrl
    };

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IsOrganiser)
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
        else if (DateOfBirth is null)
        {
            yield return new ValidationResult("Enter your date of birth.", [nameof(DateOfBirth)]);
        }
    }
}

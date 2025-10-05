namespace DefectsManagement.Api.DTOs;
using System.ComponentModel.DataAnnotations;

public class ChangePasswordDto
{
    [Required]
    public string OldPassword {get; set;}
    [Required]
    public string NewPassword {get; set;}
    [Required]
    public string ConfirmPassword {get; set;}
}
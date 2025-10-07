namespace DefectsManagement.Api.DTOs;

public class CreateUserDto
{
    public required string Name { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string Role { get; set; }
}

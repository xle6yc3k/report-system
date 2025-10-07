// DTOs/ProjectDtos.cs
public record ProjectCreateDto(string Key, string Name);
public record ProjectUpdateDto(string Key, string Name);
public record AddMemberDto(Guid UserId, string Role); // "Engineer"|"Manager"|"Observer"

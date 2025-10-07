namespace DefectsManagement.Api.DTOs;

public class CreateCommentDto
{
    public string Text { get; set; } = null!;
}

public class UpdateCommentDto
{
    public string Text { get; set; } = null!;
}

public class CommentResponseDto
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }
}

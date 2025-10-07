namespace DefectsManagement.Api.DTOs;

public class AttachmentResponseDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Size { get; set; }
    public DateTime UploadedAt { get; set; }
    public Guid UploadedById { get; set; }
    public string? DownloadUrl { get; set; } // подсказка фронту
}

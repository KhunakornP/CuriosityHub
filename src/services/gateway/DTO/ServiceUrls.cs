namespace gateway.DTO;

public class ServiceUrls
{
    public string VideoStreaming { get; set; } = "http://localhost:5001";
    public string VideoMetadata { get; set; } = "http://localhost:5002";
    public string VideoViews { get; set; } = "http://localhost:5003";
    public string VideoUpload { get; set; } = "http://localhost:5004";
    public string CommentService { get; set; } = "http://localhost:5005";
}
namespace VidyaOSDAL.DTOs
{
    public class AIChatRequest
    {
        public int SchoolId { get; set; }
        public string Question { get; set; } = string.Empty;
    }

    public class AIChatResponse
    {
        public bool Success { get; set; }
        public string Answer { get; set; } = string.Empty;
        public string? Message { get; set; }

        public static AIChatResponse Ok(string answer) => new()
        {
            Success = true,
            Answer = answer
        };

        public static AIChatResponse Fail(string message) => new()
        {
            Success = false,
            Answer = string.Empty,
            Message = message
        };
    }
}

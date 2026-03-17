namespace ChatBot_BE.Models
{
    public class GeminiResult
    {
        public string Text { get; set; } = string.Empty;
        public string FunctionCallName { get; set; } = string.Empty;
        public System.Text.Json.JsonElement FunctionCallArgs { get; set; }
    }
}

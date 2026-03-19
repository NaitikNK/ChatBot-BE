namespace ChatBot_BE.Dto
{
    public class InputValidationResult
    {
        public bool IsValid { get; set; }
        public bool ContainsProfanity { get; set; }
        public bool HasSpellingErrors { get; set; }
        public string? ErrorMessage { get; set; }
        public string? CorrectedText { get; set; }
        public IEnumerable<string> DetectedProfanities { get; set; } = Enumerable.Empty<string>();
        public IEnumerable<string> SpellingErrors { get; set; } = Enumerable.Empty<string>();
    }
}

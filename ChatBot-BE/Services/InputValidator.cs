using ChatBot_BE.Dto;

namespace ChatBot_BE.Services
{
    /// <summary>
    /// Service for validating and sanitizing user input.
    /// </summary>
    public interface IInputValidator
    {
        InputValidationResult Validate(string text);
    }

    public class InputValidator : IInputValidator
    {
        private readonly IProfanityFilter _profanityFilter;
        private readonly ISpellChecker _spellChecker;
        private readonly ILogger<InputValidator> _logger;

        public InputValidator(
            IProfanityFilter profanityFilter,
            ISpellChecker spellChecker,
            ILogger<InputValidator> logger)
        {
            _profanityFilter = profanityFilter;
            _spellChecker = spellChecker;
            _logger = logger;
        }

        public InputValidationResult Validate(string text)
        {
            var result = new InputValidationResult();

            if (string.IsNullOrWhiteSpace(text))
            {
                result.IsValid = false;
                result.ErrorMessage = "Input cannot be empty.";
                return result;
            }

            // Check for profanity
            result.ContainsProfanity = _profanityFilter.ContainsProfanity(text);
            result.DetectedProfanities = _profanityFilter.GetDetectedProfanities(text);

            if (result.ContainsProfanity)
            {
                result.IsValid = false;
                result.ErrorMessage = "Please use respectful language. Avoid inappropriate or offensive words.";
                _logger.LogWarning("Profanity detected in input. Detected: {Profanities}", 
                    string.Join(", ", result.DetectedProfanities));
                return result;
            }

            // Check for spelling errors
            result.SpellingErrors = _spellChecker.GetSpellingErrors(text);
            result.HasSpellingErrors = result.SpellingErrors.Any();

            // Correct spelling if there are errors
            if (result.HasSpellingErrors)
            {
                result.CorrectedText = _spellChecker.CorrectSpelling(text);
                _logger.LogDebug("Spelling errors corrected. Original: {Original}, Corrected: {Corrected}",
                    text, result.CorrectedText);
            }
            else
            {
                result.CorrectedText = text;
            }

            result.IsValid = true;
            return result;
        }
    }
}

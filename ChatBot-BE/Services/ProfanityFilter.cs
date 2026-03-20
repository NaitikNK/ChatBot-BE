using System.Text.RegularExpressions;

namespace ChatBot_BE.Services
{
    /// <summary>
    /// Service for detecting and handling inappropriate language.
    /// </summary>
    public interface IProfanityFilter
    {
        bool ContainsProfanity(string text);
        string CensorProfanity(string text);
        IEnumerable<string> GetDetectedProfanities(string text);
    }

    public class ProfanityFilter : IProfanityFilter
    {
        // Common profanity and offensive words (expand as needed)
        private static readonly HashSet<string> ProfanityWords = new(StringComparer.OrdinalIgnoreCase)
        {
            // Severe profanity
            "fuck", "fucker", "fucking", "fucked", "fuk", "fck", "f***ing", "f***", "f*ck",
            "shit", "shitty", "shite", "sh*t",
            "asshole", "ass", "arse", "arsehole",
            "bitch", "bitches", "bitchy", "b*tch",
            "bastard", "bastards",
            "damn", "dammit", "damnit",
            "crap", "crappy",
            
            // Sexual/offensive
            "dick", "dickhead", "penis",
            "pussy", "vagina",
            "cock", "cocksucker",
            "cum", "cumming", "cumshot",
            "nude", "naked",
            
            // Derogatory terms
            "idiot", "stupid", "dumb",
            "moron", "retard", "retarded",
            "loser", "pathetic",
            
            // Hate speech / slurs (abbreviated - expand for production)
            "nigger", "nigga", "n1gger",
            "faggot", "fag",
            "gay", // Context-dependent - may be legitimate
            "tranny",
            
            // Religious profanity
            "god damn", "goddamn", // As expletives
            "hell", // Context-dependent
            
            // Other offensive
            "whore", "slut", "hooker", "prostitute",
            "pimp",
            "kill", "murder", "suicide", // Violence
            "rape", "rapist"
        };

        // Words that are sometimes profanity but context-dependent
        private static readonly HashSet<string> ContextDependentWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "gay", "hell", "damn", "ass", "crap"
        };

        public bool ContainsProfanity(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var normalized = NormalizeText(text);
            
            // Simple but effective: split into words and check each
            var words = normalized.Split(new[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':', '-', '_', '(', ')', '[', ']', '{', '}' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var word in words)
            {
                if (ProfanityWords.Contains(word))
                {
                    // Skip context-dependent words in legitimate contexts
                    if (ContextDependentWords.Contains(word) && IsLegitimateContext(text, word))
                        continue;
                        
                    return true;
                }
            }

            // Check for repeated characters (e.g., "fuuuuck")
            var compressed = CompressRepeatedCharacters(normalized);
            foreach (var word in ProfanityWords)
            {
                if (compressed.Contains(word))
                {
                    if (ContextDependentWords.Contains(word) && IsLegitimateContext(text, word))
                        continue;
                        
                    return true;
                }
            }

            return false;
        }

        public string CensorProfanity(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var result = text;
            var normalized = NormalizeText(text);

            foreach (var word in ProfanityWords)
            {
                if (ContextDependentWords.Contains(word) && IsLegitimateContext(text, word))
                    continue;

                var index = normalized.IndexOf(word, StringComparison.OrdinalIgnoreCase);
                while (index >= 0)
                {
                    // Find the actual word in the original text (handling repeated chars)
                    var actualWord = ExtractWordAtPosition(text, index);
                    var censored = new string('*', actualWord.Length);
                    result = result.Replace(actualWord, censored, StringComparison.OrdinalIgnoreCase);
                    
                    index = normalized.IndexOf(word, index + 1, StringComparison.OrdinalIgnoreCase);
                }
            }

            return result;
        }

        public IEnumerable<string> GetDetectedProfanities(string text)
        {
            var detected = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
                return detected;

            var normalized = NormalizeText(text);

            foreach (var word in ProfanityWords)
            {
                if (ContextDependentWords.Contains(word) && IsLegitimateContext(text, word))
                    continue;

                if (normalized.Contains(word) || CompressRepeatedCharacters(normalized).Contains(word))
                {
                    detected.Add(word);
                }
            }

            return detected.Distinct();
        }

        private static string NormalizeText(string text)
        {
            return text.ToLowerInvariant()
                .Replace("0", "o")
                .Replace("1", "i")
                .Replace("3", "e")
                .Replace("4", "a")
                .Replace("5", "s")
                .Replace("7", "t")
                .Replace("@", "u")
                .Replace("$", "s")
                .Replace("!", "i")
                .Replace(".", "")
                .Replace("-", " ")
                .Replace("_", "");
        }

        private static string CompressRepeatedCharacters(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var result = new System.Text.StringBuilder();
            char? lastChar = null;

            foreach (var c in text)
            {
                if (c != lastChar)
                {
                    result.Append(c);
                    lastChar = c;
                }
            }

            return result.ToString();
        }

        private static string ExtractWordAtPosition(string text, int startIndex)
        {
            // Find word boundaries
            int start = startIndex;
            int end = startIndex;

            // Go back to find start of word
            while (start > 0 && char.IsLetterOrDigit(text[start - 1]))
                start--;

            // Go forward to find end of word
            while (end < text.Length && char.IsLetterOrDigit(text[end]))
                end++;

            return text.Substring(start, end - start);
        }

        private static bool IsLegitimateContext(string text, string word)
        {
            var lowerText = text.ToLowerInvariant();

            // "ass" in "class", "pass", "grass", etc.
            if (word == "ass")
            {
                return lowerText.Contains("class") || 
                       lowerText.Contains("pass") || 
                       lowerText.Contains("grass") ||
                       lowerText.Contains("mass") ||
                       lowerText.Contains("gas") ||
                       lowerText.Contains("assistance") ||
                       lowerText.Contains("assessment") ||
                       lowerText.Contains("asset");
            }

            // "crap" in "crappy" but not as excretion
            if (word == "crap")
            {
                return lowerText.Contains("crappy"); // Already in list
            }

            // "hell" in "hello", "shell", etc.
            if (word == "hell")
            {
                return lowerText.Contains("hello") || 
                       lowerText.Contains("shell") ||
                       lowerText.Contains("well");
            }

            // "damn" in legitimate contexts
            if (word == "damn")
            {
                return lowerText.Contains("damning") || // Different meaning
                       lowerText.Contains("condemn");
            }

            // "gay" in legitimate contexts
            if (word == "gay")
            {
                // Allow when referring to sexuality in neutral/positive context
                return true; // For now, allow all - context is too complex
            }

            return false;
        }
    }
}

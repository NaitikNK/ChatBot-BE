using System.Text.RegularExpressions;

namespace ChatBot_BE.Services
{
    /// <summary>
    /// Service for detecting and correcting common spelling mistakes.
    /// Uses a dictionary of common misspellings and pattern-based corrections.
    /// </summary>
    public interface ISpellChecker
    {
        bool HasSpellingErrors(string text);
        string CorrectSpelling(string text);
        IEnumerable<string> GetSpellingErrors(string text);
    }

    public class SpellChecker : ISpellChecker
    {
        // Common misspellings and their corrections
        private static readonly Dictionary<string, string> CommonMisspellings = new(StringComparer.OrdinalIgnoreCase)
        {
            // Policy/Insurance related
            { "police", "policy" },
            { "polcies", "policies" },
            { "polcy", "policy" },
            { "policie", "policy" },
            { "insurence", "insurance" },
            { "insurace", "insurance" },
            { "coverage", "coverage" },
            { "claim", "claim" },
            
            // Common words
            { "teh", "the" },
            { "taht", "that" },
            { "thier", "their" },
            { "there", "their" },
            { "theyre", "they're" },
            { "your", "you're" },
            { "yuo", "you" },
            { "yuor", "your" },
            { "wanna", "want to" },
            { "gonna", "going to" },
            { "gotta", "have to" },
            { "lemme", "let me" },
            { "gimme", "give me" },
            
            // Common typos
            { "adn", "and" },
            { "nad", "and" },
            { "na", "and" },
            { "jsut", "just" },
            { "jst", "just" },
            { "becuase", "because" },
            { "becasue", "because" },
            { "becuse", "because" },
            { "cuase", "cause" },
            { "wich", "which" },
            { "wat", "what" },
            { "wut", "what" },
            { "hwat", "what" },
            { "hwere", "where" },
            { "whe", "when" },
            { "wen", "when" },
            { "howw", "how" },
            { "hwo", "who" },
            { "woh", "who" },
            { "wyh", "why" },
            
            // Name related
            { "naem", "name" },
            { "nmae", "name" },
            { "fname", "first name" },
            { "lname", "last name" },
            { "sname", "surname" },

            // Email/Contact
            { "emial", "email" },
            { "mail", "email" },
            { "phnoe", "phone" },
            { "fone", "phone" },
            
            // Action words
            { "cretae", "create" },
            { "creat", "create" },
            { "ceate", "create" },
            { "delet", "delete" },
            { "deltet", "delete" },
            { "veiw", "view" },
            { "vew", "view" },
            { "shw", "show" },
            { "sohw", "show" },
            { "lsit", "list" },
            { "lst", "list" },
            { "disply", "display" },

            // Record/Data
            { "recrod", "record" },
            { "reocrd", "record" },
            { "dat", "data" },
            { "informaiton", "information" },
            { "info", "information" },
            { "infor", "information" },
            
            // Numbers/Policy numbers
            { "numbr", "number" },
            { "numbe", "number" },
            { "nmuber", "number" },
            { "nuber", "number" },
            { "num", "number" },

            // Please/Thank you
            { "pleas", "please" },
            { "plz", "please" },
            { "pls", "please" },
            { "thx", "thanks" },
            { "thanx", "thanks" },

            // Yes/No
            { "yess", "yes" },
            { "yea", "yes" },
            { "yeah", "yes" },
            { "yep", "yes" },
            { "nope", "no" },
            { "nah", "no" },

            // Help
            { "hlp", "help" },
            { "halp", "help" },
            { "asist", "assist" },

            // Other common
            { "ok", "okay" },
            { "okie", "okay" },
            { "k", "okay" },
            { "sur", "sure" },
            { "alot", "a lot" },
            { "allot", "a lot" },
            { "seperate", "separate" },
            { "definately", "definitely" },
            { "definetly", "definitely" },
            { "occured", "occurred" },
            { "untill", "until" },
            { "wierd", "weird" },
            { "tehnk", "think" },
            { "gud", "good" }
        };

        public bool HasSpellingErrors(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return GetSpellingErrors(text).Any();
        }

        public string CorrectSpelling(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var result = text;

            foreach (var kvp in CommonMisspellings)
            {
                // Use regex to replace whole words only and preserve case
                var pattern = $@"\b{Regex.Escape(kvp.Key)}\b";
                result = Regex.Replace(result, pattern, match => PreserveCase(match.Value, kvp.Value), RegexOptions.IgnoreCase);
            }

            return result;
        }

        private static string PreserveCase(string original, string replacement)
        {
            if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(replacement))
                return replacement;

            // All uppercase
            if (original.All(c => !char.IsLetter(c) || char.IsUpper(c)))
                return replacement.ToUpperInvariant();

            // Title case (first letter uppercase)
            if (char.IsUpper(original[0]))
            {
                if (replacement.Length > 1)
                    return char.ToUpperInvariant(replacement[0]) + replacement.Substring(1);
                return replacement.ToUpperInvariant();
            }

            // Default to lowercase as per dictionary
            return replacement.ToLowerInvariant();
        }

        public IEnumerable<string> GetSpellingErrors(string text)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
                return errors;

            var words = Regex.Matches(text.ToLowerInvariant(), @"\b\w+\b")
                .Cast<Match>()
                .Select(m => m.Value);

            foreach (var word in words)
            {
                if (CommonMisspellings.ContainsKey(word))
                {
                    errors.Add(word);
                }
            }

            return errors.Distinct();
        }
    }
}

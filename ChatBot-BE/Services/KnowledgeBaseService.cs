using System.Collections.Concurrent;

namespace ChatBot_BE.Services
{
    public class KnowledgeBaseItem
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? SourceUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string PolicyType { get; set; } = string.Empty;
        public string? PolicyName { get; set; }
    }

    public interface IKnowledgeBaseService
    {
        Task InitializeAsync();
        Task AddDocumentAsync(KnowledgeBaseItem item);
        Task<IEnumerable<KnowledgeBaseItem>> SearchAsync(string query, int limit = 5);
        Task<IEnumerable<KnowledgeBaseItem>> GetAllAsync();
        Task<bool> DeleteDocumentAsync(string id);
    }

    /// <summary>
    /// Simple in-memory knowledge base service with keyword-based search.
    /// For production, consider using a proper vector database like Qdrant, Pinecone, or Azure AI Search.
    /// </summary>
    public class InMemoryKnowledgeBaseService : IKnowledgeBaseService
    {
        private readonly ConcurrentDictionary<string, KnowledgeBaseItem> _documents = new();
        private readonly ILogger<InMemoryKnowledgeBaseService> _logger;

        public InMemoryKnowledgeBaseService(ILogger<InMemoryKnowledgeBaseService> logger)
        {
            _logger = logger;
        }

        public Task InitializeAsync()
        {
            _logger.LogInformation("Knowledge base initialized");
            return Task.CompletedTask;
        }

        public Task AddDocumentAsync(KnowledgeBaseItem item)
        {
            var key = $"{item.Id}-{Guid.NewGuid():N}";
            item.Id = key;
            _documents[key] = item;

            _logger.LogInformation("Added knowledge base item: {Title} (Category: {Category})",
                item.Title, item.Category);

            return Task.CompletedTask;
        }

        public Task<IEnumerable<KnowledgeBaseItem>> SearchAsync(string query, int limit = 5)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Task.FromResult(Enumerable.Empty<KnowledgeBaseItem>());
            }

            // Break apart camel-case policy names (e.g., "EVInsurance" -> "EV Insurance", "AutoInsurance" -> "Auto Insurance")
            var processedQuery = System.Text.RegularExpressions.Regex.Replace(query, "([a-z])([A-Z])", "$1 $2");
            processedQuery = System.Text.RegularExpressions.Regex.Replace(processedQuery, "([A-Z]+)([A-Z][a-z])", "$1 $2");

            var queryLower = processedQuery.ToLowerInvariant();
            var queryWords = queryLower.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);

            var scored = _documents.Values.Select(doc =>
            {
                var contentLower = (doc.Title + " " + doc.Content + " " + doc.Category).ToLowerInvariant();
                var score = queryWords.Count(w => contentLower.Contains(w));
                return (Doc: doc, Score: score);
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .Select(x => x.Doc);

            _logger.LogDebug("Knowledge base search: '{Query}' found {Count} results", query, scored.Count());

            return Task.FromResult(scored);
        }

        public Task<IEnumerable<KnowledgeBaseItem>> GetAllAsync()
        {
            return Task.FromResult(_documents.Values.AsEnumerable());
        }

        public Task<bool> DeleteDocumentAsync(string id)
        {
            var removed = _documents.TryRemove(id, out _);
            if (removed)
            {
                _logger.LogInformation("Deleted knowledge base item: {Id}", id);
            }
            return Task.FromResult(removed);
        }
    }
}

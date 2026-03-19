using System.Text.Json;
using ChatBot_BE.Services;

namespace ChatBot_BE.Data
{
    public class KnowledgeBaseData
    {
        public List<KnowledgeBaseItem> Documents { get; set; } = new();
    }

    public static class KnowledgeBaseSeeder
    {
        public static async Task SeedAsync(IKnowledgeBaseService knowledgeBase, IWebHostEnvironment environment)
        {
            await knowledgeBase.InitializeAsync();

            var jsonPath = Path.Combine(environment.ContentRootPath, "Shared", "knowledge-base.json");
            
            if (!File.Exists(jsonPath))
            {
                return;
            }

            var json = await File.ReadAllTextAsync(jsonPath);
            var data = JsonSerializer.Deserialize<KnowledgeBaseData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });

            if (data?.Documents == null || data.Documents.Count == 0)
            {
                return;
            }

            foreach (var doc in data.Documents)
            {
                await knowledgeBase.AddDocumentAsync(doc);
            }
        }
    }
}

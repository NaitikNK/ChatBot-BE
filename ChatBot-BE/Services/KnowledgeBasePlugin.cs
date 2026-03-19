using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace ChatBot_BE.Services
{
    /// <summary>
    /// Semantic Kernel plugin for querying the knowledge base.
    /// Enables the AI to retrieve relevant information from insurance policy documentation.
    /// </summary>
    public class KnowledgeBasePlugin
    {
        private readonly IKnowledgeBaseService _knowledgeBase;
        private readonly ILogger<KnowledgeBasePlugin> _logger;

        public KnowledgeBasePlugin(
            IKnowledgeBaseService knowledgeBase,
            ILogger<KnowledgeBasePlugin> logger)
        {
            _knowledgeBase = knowledgeBase;
            _logger = logger;
        }

        [KernelFunction("search_knowledge_base")]
        [Description("Search the insurance policy knowledge base for relevant information")]
        public async Task<string> SearchKnowledgeBase(
            [Description("The search query or question from the user")] string query)
        {
            _logger.LogDebug("SearchKnowledgeBase called. Query: {Query}", query);

            if (string.IsNullOrWhiteSpace(query))
            {
                return "⚠️ Please provide a specific question or topic to search for.";
            }

            try
            {
                var results = await _knowledgeBase.SearchAsync(query, limit: 5);
                var resultList = results.ToList();

                if (resultList.Count == 0)
                {
                    _logger.LogInformation("No knowledge base results for query: {Query}", query);
                    return $"📭 No specific information found about \"{query}\". Let me help you with general policy information instead.";
                }

                var response = new System.Text.StringBuilder();
                response.AppendLine($"📋 Found {resultList.Count} relevant result(s) for \"{query}\":");
                response.AppendLine();

                foreach (var item in resultList)
                {
                    response.AppendLine($"• **{item.Title}** ({item.Category})");
                    response.AppendLine($"  {TruncateContent(item.Content, 200)}");
                    response.AppendLine();
                }

                _logger.LogInformation("KnowledgeBasePlugin returned {Count} results for query: {Query}", resultList.Count, query);
                return response.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching knowledge base for query: {Query}", query);
                return $"⚠️ Unable to search the knowledge base at the moment. Please try again.";
            }
        }

        [KernelFunction("get_policy_information")]
        [Description("Get general information about insurance policies, coverage, claims, and procedures")]
        public async Task<string> GetPolicyInformation(
            [Description("The type of information needed (e.g., 'coverage', 'claims', 'premiums', 'deductibles')")] string topic)
        {
            _logger.LogDebug("GetPolicyInformation called. Topic: {Topic}", topic);

            if (string.IsNullOrWhiteSpace(topic))
            {
                return "⚠️ Please specify what policy information you need (e.g., coverage, claims, premiums).";
            }

            try
            {
                var results = await _knowledgeBase.SearchAsync(topic, limit: 3);
                var resultList = results.ToList();

                if (resultList.Count == 0)
                {
                    return $"📭 I don't have specific information about {topic}. Please contact customer support for assistance.";
                }

                var response = new System.Text.StringBuilder();
                response.AppendLine($"📋 Information about **{topic}**:\n");

                foreach (var item in resultList)
                {
                    response.AppendLine($"**{item.Title}**");
                    response.AppendLine(item.Content);
                    response.AppendLine();
                }

                return response.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting policy information for topic: {Topic}", topic);
                return $"⚠️ Unable to retrieve policy information at the moment.";
            }
        }

        private static string TruncateContent(string content, int maxLength)
        {
            if (string.IsNullOrEmpty(content) || content.Length <= maxLength)
                return content ?? string.Empty;

            return content[..maxLength] + "...";
        }
    }
}

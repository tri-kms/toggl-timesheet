// Generated by Copilot
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TogglTimesheet.Timesheet
{
    public interface ITimeDataLoader
    {
        Task<(string jsonResponse, List<JsonTimeEntry> jsonTimeEntries)> FetchDetailedReportAsync(
            string apiToken, string workspaceId, string startDate, string endDate);
    }

    public class TimeDataLoader : ITimeDataLoader
    {
        private readonly HttpClient _httpClient;

        public TimeDataLoader(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Fetches a detailed report from the Toggl API.
        /// </summary>
        /// <param name="apiToken">The API token for authentication.</param>
        /// <param name="workspaceId">The workspace ID for the report.</param>
        /// <param name="startDate">The start date for the report.</param>
        /// <param name="endDate">The end date for the report.</param>
        /// <returns>A JSON string containing the detailed report.</returns>
        public async Task<(string jsonResponse, List<JsonTimeEntry> jsonTimeEntries)> FetchDetailedReportAsync(
            string apiToken, string workspaceId, string startDate, string endDate)
        {
            if (string.IsNullOrWhiteSpace(apiToken))
            {
            throw new ArgumentException("API token cannot be null or whitespace.", nameof(apiToken));
            }

            if (string.IsNullOrWhiteSpace(workspaceId))
            {
            throw new ArgumentException("Workspace ID cannot be null or whitespace.", nameof(workspaceId));
            }

            var requestUri = $"/reports/api/v3/workspace/{workspaceId}/search/time_entries";
            var requestBody = new
            {
            start_date = startDate,
            end_date = endDate
            };

            var requestJson = JsonSerializer.Serialize(requestBody);
            var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
            Content = requestContent
            };

            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiToken}:api_token"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);

            var response = await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonTimeEntries = JsonSerializer.Deserialize<List<JsonTimeEntry>>(responseContent) ?? new List<JsonTimeEntry>();

            return (responseContent, jsonTimeEntries);
        }
    }
}

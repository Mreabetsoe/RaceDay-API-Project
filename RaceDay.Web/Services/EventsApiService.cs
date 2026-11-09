using System.Net.Http.Json;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services;

public class EventsApiService
{
    private readonly IHttpClientFactory _factory;

    public EventsApiService(IHttpClientFactory factory) => _factory = factory;

    public async Task<List<EventSummary>> GetEventsAsync()
    {
        var client = _factory.CreateClient("RaceDayApi");
        var events = await client.GetFromJsonAsync<List<EventSummary>>("api/events");
        return events ?? new List<EventSummary>();
    }
}

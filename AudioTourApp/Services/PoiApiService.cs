using AudioTourApp.Models;
using System.Net.Http.Json;

namespace AudioTourApp.Services;

public class PoiApiService
{
    private HttpClient _http = new HttpClient
    {
        BaseAddress = new Uri("https://10.0.2.2:7114/")
    };

    public async Task<POI> GetPoiById(int id)
    {
        return await _http.GetFromJsonAsync<POI>($"api/Poi/{id}");
    }
}
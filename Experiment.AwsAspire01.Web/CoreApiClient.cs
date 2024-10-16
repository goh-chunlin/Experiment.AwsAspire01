namespace Experiment.AwsAspire01.Web;

public class CoreApiClient(HttpClient httpClient)
{
    public async Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        List<WeatherForecast>? forecasts = null;

        await foreach (var forecast in httpClient.GetFromJsonAsAsyncEnumerable<WeatherForecast>("/weatherforecast", cancellationToken))
        {
            if (forecasts?.Count >= maxItems)
            {
                break;
            }
            if (forecast is not null)
            {
                forecasts ??= [];
                forecasts.Add(forecast);
            }
        }

        return forecasts?.ToArray() ?? [];
    }
    
    public async Task<GalleryImage[]> GetGalleryImagesAsync(CancellationToken cancellationToken = default)
    {
        List<GalleryImage>? images = null;

        await foreach (var image in httpClient.GetFromJsonAsAsyncEnumerable<GalleryImage>("/images", cancellationToken))
        {
            if (image is null) continue;
            
            images ??= [];
            images.Add(image);
        }

        return images?.ToArray() ?? [];
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public record GalleryImage(string Url)
{
    
}
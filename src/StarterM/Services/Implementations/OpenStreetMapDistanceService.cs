using StarterM.Services.Interfaces;
using StarterM.ViewModels;
using System.Globalization;
using System.Text.Json;

namespace StarterM.Services.Implementations
{
    // OpenStreetMapDistanceService：
    // 專門負責和外部地圖服務溝通。
    //
    // 目前使用：
    // - Nominatim：地址 -> 座標
    // - OSRM：座標 -> 道路路線距離
    //
    // 其他 controller / service 不需要直接碰外部 API，
    // 只要透過 IDistanceService 呼叫即可。
    public class OpenStreetMapDistanceService : IDistanceService
    {
        // 呼叫外部 HTTP API
        private readonly HttpClient _httpClient;

        // 記錄失敗原因，方便追蹤外部服務錯誤
        private readonly ILogger<OpenStreetMapDistanceService> _logger;

        // 建構式：HttpClient 由 AddHttpClient 注入
        public OpenStreetMapDistanceService(HttpClient httpClient, ILogger<OpenStreetMapDistanceService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        // 傳統簡化版介面：只需要單程公里數時可呼叫此方法。
        // 實際上仍然是轉呼叫完整的 CalculateRouteAsync。
        public async Task<decimal?> CalculateDistanceAsync(string origin, string destination)
        {
            var route = await CalculateRouteAsync(new DistanceRouteRequestViewModel
            {
                Origin = origin,
                Destination = destination,
                IsRoundTrip = false
            });

            return route?.SingleDistanceKm;
        }

        // Geocode：把地址文字轉成經緯度。
        // 回傳後的 DisplayName 也能直接提供前端顯示。
        public async Task<DistanceGeocodeResultViewModel?> GeocodeAsync(string address)
        {
            try
            {
                // EscapeDataString：把地址轉成可安全放在 URL 裡的格式
                var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(address)}&format=json&limit=1";

                // Nominatim 規定請求要帶 User-Agent，否則可能被拒絕。
                if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
                {
                    _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("TravelCarbonManagementSystem/1.0");
                }

                // 呼叫 Nominatim
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                // 解析 JSON 回傳資料
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                // RootElement 是最外層；Nominatim 回傳的是陣列
                var arr = doc.RootElement;

                // 只取第一筆最相近的地址結果
                if (arr.GetArrayLength() > 0)
                {
                    // lat / lon 在 JSON 中是字串，
                    // 這裡用 InvariantCulture 避免不同語系造成小數點解析問題。
                    var lat = double.Parse(arr[0].GetProperty("lat").GetString()!, CultureInfo.InvariantCulture);
                    var lon = double.Parse(arr[0].GetProperty("lon").GetString()!, CultureInfo.InvariantCulture);
                    return new DistanceGeocodeResultViewModel
                    {
                        // display_name 是 OSM 整理好的可讀地址
                        DisplayName = arr[0].GetProperty("display_name").GetString() ?? address,
                        Latitude = lat,
                        Longitude = lon
                    };
                }

                // 找不到資料時回 null，交由上層決定提示訊息
                return null;
            }
            catch (Exception ex)
            {
                // 外部服務失敗時寫 log，但不把例外原封不動往上丟
                _logger.LogError(ex, "Geocode 失敗: {Address}", address);
                return null;
            }
        }

        // 計算完整路線資料：
        // 不只回傳公里數，也回傳 geometry 給前端畫地圖。
        public async Task<DistanceRouteResultViewModel?> CalculateRouteAsync(DistanceRouteRequestViewModel request)
        {
            try
            {
                // 先決定起終點座標來源：
                // 若前端已有座標，直接用；
                // 若沒有，才退回用地址 geocode。
                var originCoords = await ResolveCoordinatesAsync(
                    request.Origin,
                    request.OriginLatitude,
                    request.OriginLongitude);

                var destCoords = await ResolveCoordinatesAsync(
                    request.Destination,
                    request.DestinationLatitude,
                    request.DestinationLongitude);

                // 任一端沒座標，就無法算路線
                if (originCoords == null || destCoords == null)
                {
                    _logger.LogWarning(
                        "無法取得路線座標: origin={Origin}, destination={Destination}",
                        request.Origin,
                        request.Destination);
                    return null;
                }

                // OSRM 路線 API：
                // overview=full 取得完整路線
                // geometries=geojson 讓前端可直接用 Leaflet 畫線
                var url =
                    $"https://router.project-osrm.org/route/v1/driving/{originCoords.Value.lon},{originCoords.Value.lat};{destCoords.Value.lon},{destCoords.Value.lat}?overview=full&geometries=geojson";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                // 解析 OSRM 回傳
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                var routes = doc.RootElement.GetProperty("routes");
                if (routes.GetArrayLength() == 0)
                {
                    // API 有回應，但沒有可用路徑
                    return null;
                }

                var route = routes[0];

                // OSRM distance 單位是公尺，這裡轉成公里並保留 1 位小數
                var singleDistanceKm = Math.Round((decimal)(route.GetProperty("distance").GetDouble() / 1000), 1);

                // 如果是往返，總距離 = 單程 * 2
                var totalDistanceKm = request.IsRoundTrip ? singleDistanceKm * 2 : singleDistanceKm;

                return new DistanceRouteResultViewModel
                {
                    SingleDistanceKm = singleDistanceKm,
                    TotalDistanceKm = totalDistanceKm,
                    // JsonDocument 在 using 結束後就會釋放，
                    // 所以要 Clone() 後才能安全回傳 geometry。
                    Geometry = route.TryGetProperty("geometry", out var geometry)
                        ? geometry.Clone()
                        : null
                };
            }
            catch (Exception ex)
            {
                // 寫 log，交給上層回友善訊息
                _logger.LogError(ex, "計算路線失敗: {Origin} → {Destination}", request.Origin, request.Destination);
                return null;
            }
        }

        // 統一決定座標來源的 helper：
        // 1. 優先用前端已帶的座標
        // 2. 沒有座標時才用地址 geocode
        private async Task<(double lat, double lon)?> ResolveCoordinatesAsync(
            string? address,
            double? latitude,
            double? longitude)
        {
            // 若前端已帶座標，直接採用最準確
            if (latitude.HasValue && longitude.HasValue)
            {
                return (latitude.Value, longitude.Value);
            }

            // 既沒有座標又沒有地址，就無法解析
            if (string.IsNullOrWhiteSpace(address))
            {
                return null;
            }

            // 後備方案：用地址 geocode
            var geocode = await GeocodeAsync(address);
            if (geocode == null)
            {
                return null;
            }

            return (geocode.Latitude, geocode.Longitude);
        }
    }
}

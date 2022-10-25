using Microsoft.JSInterop;
using OIDC_ArtifactServer.Extensions;
using System.Text.Json;

namespace OIDC_ArtifactServer.Browser
{
    public class BrowserStorage
    {
        private readonly IJSRuntime _js;
        private readonly ILogger<BrowserStorage> _logger;
        private static TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

        public BrowserStorage(ILogger<BrowserStorage> logger, IJSRuntime js)
        {
            _logger = logger;
            _js = js;
        }

        public async Task SetSessionItem<T>(string key, T item) where T : class
        {
            await SetItem("window.sessionsStorage.setItem", key, item);
        }
        public async Task SetLocalItem<T>(string key, T item) where T : class
        {
            await SetItem("window.localStorage.setItem", key, item);
        }

        private async Task SetItem<T>(string fnName, string key, T item) where T : class
        {
            try
            {
                var itemStr = JsonSerializer.Serialize(item);
                await _js.InvokeVoidAsync(fnName, key, itemStr);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public async Task<T> RetrieveSessionItem<T>(string key) where T : class
        {
            var task = RetrieveItem<T>("window.sessionStorage.getItem", key);
            return await task.TimeoutAfter(DefaultTimeout);
        }

        public async Task<T> RetrieveLocalItem<T>(string key) where T : class
        {
            var task = RetrieveItem<T>("window.localStorage.getItem", key);
            return await task.TimeoutAfter(DefaultTimeout);
        }

        private async Task<T> RetrieveItem<T>(string fnName, string key) where T : class
        {
            var itemStr = await _js.InvokeAsync<string>(fnName, key);
            try
            {
                T item = JsonSerializer.Deserialize<T>(itemStr);
                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
        }
    }
}

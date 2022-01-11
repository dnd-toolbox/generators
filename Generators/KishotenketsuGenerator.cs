using System.Net.Http.Json;

namespace generators.Generators {
    
    public class KishotenketsuGenerator {

        private HttpClient _client;
        public KishotenketsuGenerator(HttpClient client) {
            _client = client;
        }
        public async Task<KishotenketsuModel> GenerateKishotenketsu() {
            KishotenketsuRecord data = await GetData();
            return new KishotenketsuModel(
                ki: data.ki.OrderBy(a => Guid.NewGuid()).First().value,
                sho: data.sho.OrderBy(a => Guid.NewGuid()).First().value,
                ten: data.ten.OrderBy(a => Guid.NewGuid()).First().value,
                ketsu: data.ketsu.OrderBy(a => Guid.NewGuid()).First().value
            );
        }

        private async Task<KishotenketsuRecord> GetData() {
            var path = "sample-data/kishotenketsu.json";
            return await _client.GetFromJsonAsync<KishotenketsuRecord>(path);
        }

        private record KishotenketsuRecord(List<KishotenketsuEntry> ki, List<KishotenketsuEntry> sho, List<KishotenketsuEntry> ten, List<KishotenketsuEntry> ketsu);
        private record KishotenketsuEntry(string value);
    }

    public record KishotenketsuModel(string ki, string sho, string ten, string ketsu);
}
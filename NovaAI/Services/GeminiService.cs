using System.Text;
using System.Text.Json;
using NovaAI.Models;

namespace NovaAI.Services
{
    public class GeminiService
    {
        private readonly HttpClient _http;

        // Çalışan gerçek Gemini şifreni buraya çakıyoruz, böylece ayar dosyasından okuma hatası eleniyor:
        private readonly string _apiKey = "APIKEYYAZ";

        public GeminiService(HttpClient http)
        {
            _http = http;
        }

        public async Task<(string Reply, string Intent)> AskAsync(string message, ChannelAnalysis? channel)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey.Trim()}";

            // ── SINIRLANDIRMA VE KURALLAR (SYSTEM PROMPT) ──
            string systemPrompt = "KURAL 1 (KİMLİK): Sen NOVA adında, sadece sosyal medya analizi ve NLP üzerine uzmanlaşmış bir yapay zeka asistanısın. Kısa, net ve emojilerle desteklenmiş cevaplar verirsin.\n" +
                                 "KURAL 2 (KATI SINIRLANDIRMA): Sen SADECE YouTube, sosyal medya, kanal analizleri, video istatistikleri ve yorum analizi (NLP/Duygu analizi) konularında konuşabilirsin. Bunların dışındaki hiçbir konuya (yemek tarifleri, genel sohbet, hava durumu, ödev yapma, kod yazma, felsefe vb.) ASLA cevap vermeyeceksin.\n" +
                                 "KURAL 3 (REDDETME): Kullanıcı sana bu sınırların dışında alakasız bir soru sorarsa veya genel sohbete girmeye çalışırsa, soruyu tamamen görmezden gelerek kelimesi kelimesine SADECE şu cevabı vereceksin: 'Ben sadece sosyal medya ve kanal analizi üzerine uzmanlaşmış bir yapay zeka asistanıyım. Lütfen analiz verileriyle ilgili bir soru sorun.'\n\n";

            if (channel != null)
            {
                systemPrompt += $"Şu an '{channel.ChannelName}' kanalını inceliyorsun. Kanalın {channel.Subscribers} abonesi ve {channel.VideoCount} videosu var. ";
            }

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = systemPrompt + "\nKullanıcının Sorusu: " + message } } }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {
                var response = await _http.PostAsync(url, jsonContent);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ($"API Hatası: {response.StatusCode} - {responseString}", "error");
                }

                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;

                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                    return (text ?? "Üzgünüm, anlamlı bir cevap üretemedim.", "chat");
                }

                return ("Gemini'den boş yanıt döndü.", "error");
            }
            catch (Exception ex)
            {
                return ($"Bağlantı Hatası: {ex.Message}", "error");
            }
        }
    }
}
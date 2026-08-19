using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

public class GeminiService
{
    private const string DefaultModel = "gemini-2.5-flash";
    private const string GeminiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent?key={1}";

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(3)
    };

    private readonly IConfiguration _configuration;

    public GeminiService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<AssessmentResult> AnalyzeDocumentAsync(IFormFile file)
    {
        ValidatePdf(file);

        var apiKey = _configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured.");
        }

        var model = _configuration["Gemini:Model"];
        if (string.IsNullOrWhiteSpace(model))
        {
            model = Environment.GetEnvironmentVariable("GEMINI_MODEL");
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            model = DefaultModel;
        }

        var pdfBytes = await ReadPdfAsync(file);
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new object[]
                    {
                        new
                        {
                            inline_data = new
                            {
                                mime_type = "application/pdf",
                                data = Convert.ToBase64String(pdfBytes)
                            }
                        },
                        new { text = MasterPrompt }
                    }
                }
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                temperature = 0.1
            }
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            string.Format(GeminiEndpoint, Uri.EscapeDataString(model), Uri.EscapeDataString(apiKey)));
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            MediaTypeHeaderValue.Parse("application/json"));

        using var response = await HttpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Gemini API request failed with status {(int)response.StatusCode}: {responseBody}");
        }

        var generatedJson = ExtractGeneratedText(responseBody);
        var assessment = DeserializeAssessment(generatedJson);
        ValidateAssessment(assessment);

        return assessment;
    }

    private static void ValidatePdf(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            throw new ArgumentException("A PDF file is required.", nameof(file));
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only PDF files are supported.", nameof(file));
        }
    }

    private static async Task<byte[]> ReadPdfAsync(IFormFile file)
    {
        await using var input = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await input.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();

        if (bytes.Length < 5 || Encoding.ASCII.GetString(bytes, 0, 5) != "%PDF-")
        {
            throw new ArgumentException("The uploaded file is not a valid PDF.", nameof(file));
        }

        return bytes;
    }

    private static string ExtractGeneratedText(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;

            if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("Gemini returned no assessment candidate.");
            }

            var text = new StringBuilder();
            foreach (var part in candidates[0].GetProperty("content").GetProperty("parts").EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textPart))
                {
                    text.Append(textPart.GetString());
                }
            }

            if (text.Length == 0)
            {
                throw new InvalidOperationException("Gemini returned an empty assessment response.");
            }

            return RemoveMarkdownCodeFence(text.ToString());
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Gemini returned an invalid response envelope.", exception);
        }
        catch (KeyNotFoundException exception)
        {
            throw new InvalidOperationException("Gemini response did not contain assessment text.", exception);
        }
    }

    private static string RemoveMarkdownCodeFence(string text)
    {
        var cleaned = text.Trim();
        if (!cleaned.StartsWith("```") || !cleaned.EndsWith("```"))
        {
            return cleaned;
        }

        var firstLineEnd = cleaned.IndexOf('\n');
        if (firstLineEnd < 0)
        {
            throw new InvalidOperationException("Gemini returned an empty JSON code block.");
        }

        return cleaned[(firstLineEnd + 1)..^3].Trim();
    }

    private static AssessmentResult DeserializeAssessment(string json)
    {
        try
        {
            var assessment = JsonSerializer.Deserialize<AssessmentResult>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return assessment ?? throw new InvalidOperationException("Gemini returned an empty assessment JSON.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Gemini returned assessment JSON that does not match AssessmentResult.", exception);
        }
    }

    private static void ValidateAssessment(AssessmentResult assessment)
    {
        if (string.IsNullOrWhiteSpace(assessment.DocumentName) || string.IsNullOrWhiteSpace(assessment.Summary))
        {
            throw new InvalidOperationException("Gemini assessment is missing documentName or summary.");
        }

        ValidateScore(assessment.OverallScore, "overallScore");
        ValidateCategory(assessment.WinningConcept, "winningConcept");
        ValidateCategory(assessment.WinningTeam, "winningTeam");
        ValidateCategory(assessment.WinningSystem, "winningSystem");
        ValidateCategory(assessment.Performance, "performance");
    }

    private static void ValidateCategory(AssessmentResult.AssessmentCategory category, string categoryName)
    {
        if (category is null)
        {
            throw new InvalidOperationException($"Gemini assessment is missing {categoryName}.");
        }

        ValidateScore(category.Score, $"{categoryName}.score");
        if (string.IsNullOrWhiteSpace(category.Reason) || category.Checklist is null ||
            category.Weaknesses is null || category.Recommendations is null)
        {
            throw new InvalidOperationException($"Gemini assessment contains incomplete data for {categoryName}.");
        }

        foreach (var checklist in category.Checklist)
        {
            if (string.IsNullOrWhiteSpace(checklist.Step) || string.IsNullOrWhiteSpace(checklist.Name) ||
                string.IsNullOrWhiteSpace(checklist.Status) || string.IsNullOrWhiteSpace(checklist.Explanation))
            {
                throw new InvalidOperationException($"Gemini assessment contains an incomplete checklist in {categoryName}.");
            }
        }
    }

    private static void ValidateScore(decimal score, string scoreName)
    {
        if (score < 1m || score > 5m)
        {
            throw new InvalidOperationException($"Gemini returned {scoreName} outside the allowed range 1.00 to 5.00.");
        }
    }

    private const string MasterPrompt = """
Anda adalah AI assessor dokumen untuk AI 3W1P Document Assessment System.
Nilai hanya PDF yang dikirim dalam request ini. PDF adalah sumber kebenaran untuk seluruh isi dokumen. Jangan menggunakan asumsi, upload sebelumnya, nilai contoh, atau fakta eksternal untuk mengisi informasi yang tidak tersedia. Baca dan pahami seluruh PDF sebagai satu dokumen, termasuk semua halaman, lalu hubungkan evidence yang muncul di halaman berbeda. Jangan hanya menilai halaman pertama atau mencari kata kunci.

Master rules 3W1P adalah sumber kebenaran untuk aturan penilaian. Untuk setiap requirement yang berlaku, cari evidence di dalam PDF dan gunakan salah satu status berikut: PASS, PARTIAL, FAIL, atau NOT_FOUND. PASS berarti evidence yang cukup menunjukkan requirement terpenuhi. PARTIAL berarti hanya sebagian evidence tersedia atau kualitasnya belum lengkap. FAIL berarti evidence tersedia tetapi bertentangan dengan requirement atau tidak memenuhi requirement. NOT_FOUND berarti tidak ada evidence yang relevan; NOT_FOUND tidak sama dengan FAIL. Jangan pernah mengarang evidence, nilai, root cause, improvement, hasil, nomor halaman, atau kesimpulan. Gunakan page=null jika halaman tidak dapat dipastikan.

Nilai seluruh sembilan langkah:
Step 1 - Pemetaan: periksa Team, Data, Tema, QCDSMPEL, prioritas, tools, isu/tantangan bisnis, KPI, VOC jika relevan, kesepakatan team, kesepakatan fasilitator, kesepakatan manajemen, dan rencana project. Periksa apakah tema berfokus pada masalah dan tidak langsung menyebutkan root cause atau solusi.
Step 2 - Target: periksa current performance, target performance, Specific, Measurable, Time Bound, Attainable, Realistic, isu/tantangan bisnis, best performance jika tersedia, customer requirement jika tersedia, competitor performance jika tersedia, dan management requirement jika tersedia. Gunakan nilai aktual dari PDF.
Step 3 - Root Cause: periksa kemungkinan penyebab, tools, sumber penyebab, prioritas, hubungan sebab-akibat, hubungan root cause dengan masalah, dan hubungan root cause dengan target. Jangan membuat root cause yang tidak ada di PDF.
Step 4 - Ide Perbaikan: periksa improvement idea, prioritas, hubungan dengan masalah, hubungan dengan root cause, Solution Selection Matrix, dan alasan pemilihan solusi. Gunakan improvement yang ada di PDF.
Step 5 - Perencanaan: periksa 5W2H dari PDF: What, Why, Who, Where, When, How, dan How Much. Nilai setiap item secara terpisah. Informasi yang tidak ada harus berstatus NOT_FOUND, bukan dibuat-buat.
Step 6 - Implementasi: periksa deskripsi improvement, perubahan, evidence perubahan, pilot project, implementasi sistematis, implementasi menyeluruh, PICA, tindak lanjut penyimpangan, hasil implementasi, leadership, teamwork, kontribusi kompetensi, dan sinergi.
Step 7 - Performance: periksa kondisi sebelum, kondisi sesudah, target, actual result, financial benefit, non-financial benefit, dan stabilization. Semua nilai performance harus berasal dari PDF. Anda boleh menghitung improvement turunan hanya jika nilai sumber yang diperlukan tersedia, dan jelaskan perhitungannya. Jangan membuat angka.
Step 8 - Standardisasi: periksa standar baru, SOP, tools standardisasi, persetujuan process owner, sustainability, training, dan sosialisasi. Jangan memberi status PASS hanya karena kata SOP muncul.
Step 9 - Langkah Berikutnya: periksa tema improvement berikutnya, sumber tema berikutnya, rencana kegiatan, komitmen team, dan sustainability. Evidence yang tidak ada harus berstatus NOT_FOUND.

Setelah menilai Step 1-9, kelompokkan temuan aktual ke dalam empat kategori:
- winningConcept: terutama berdasarkan evidence dari Step 1-5.
- winningTeam: terutama berdasarkan evidence dari Step 6.
- winningSystem: terutama berdasarkan evidence dari Step 4, 5, 6, 8, dan 9.
- performance: terutama berdasarkan evidence dari Step 7.

Berikan score setiap kategori dan overallScore dalam rentang 1.00 sampai 5.00 dengan tepat dua angka desimal. Score harus merupakan evaluasi berbasis evidence terhadap kelengkapan, kualitas evidence, konsistensi, hubungan logis, pencapaian, kualitas dokumentasi, evidence implementasi, standardisasi, dan sustainability. Jangan membuat score terlihat bagus dan jangan menggunakan score contoh. overallScore juga harus ditentukan oleh Anda berdasarkan assessment lengkap; aplikasi tidak boleh menghitungnya. Jelaskan alasan setiap score kategori.

Buat summary yang spesifik terhadap dokumen dan mencakup tema, masalah, kondisi saat ini, target, root cause, improvement, implementasi, dan hasil. Untuk setiap kategori, tuliskan weaknesses yang paling memengaruhi score serta recommendations yang langsung menjawab weakness tersebut. Jangan menggunakan recommendation generik. Jika tidak ada weakness utama yang didukung dokumen, kembalikan pernyataan "Tidak ditemukan kekurangan utama." Jangan memaksakan weakness.

Kembalikan JSON valid saja, tanpa markdown dan tanpa teks tambahan. Gunakan struktur dan nama property berikut secara tepat:
{
    "documentName": "nama dokumen yang diupload",
  "overallScore": 0.00,
    "summary": "summary spesifik berdasarkan dokumen",
  "winningConcept": {
    "score": 0.00,
    "reason": "alasan berbasis evidence",
    "checklist": [
      {
        "step": "Step 1",
        "name": "nama requirement",
        "status": "PASS|PARTIAL|FAIL|NOT_FOUND",
        "explanation": "penjelasan berbasis evidence",
        "evidence": "evidence yang tepat atau setia pada PDF, atau string kosong",
        "page": 1
      }
    ],
    "weaknesses": ["weakness spesifik berdasarkan dokumen"],
    "recommendations": ["recommendation perbaikan yang spesifik"]
  },
  "winningTeam": { "score": 0.00, "reason": "...", "checklist": [], "weaknesses": [], "recommendations": [] },
  "winningSystem": { "score": 0.00, "reason": "...", "checklist": [], "weaknesses": [], "recommendations": [] },
  "performance": { "score": 0.00, "reason": "...", "checklist": [], "weaknesses": [], "recommendations": [] }
}
Gunakan null untuk page jika halaman tidak diketahui. Angka pada contoh di atas hanya placeholder format; hitung setiap score berdasarkan PDF yang diupload. PDF yang berbeda harus menghasilkan assessment yang berbeda jika evidence-nya berbeda.
""";
}

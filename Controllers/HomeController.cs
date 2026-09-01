using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DocumentAssessmentSystem3W1P.Models;
using DocumentAssessmentSystem3W1P.Services;

namespace DocumentAssessmentSystem3W1P.Controllers;

public class HomeController : Controller
{
    private const long MaxFileSize = 20 * 1024 * 1024;

    private readonly GeminiService _geminiService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(GeminiService geminiService, ILogger<HomeController> logger)
    {
        _geminiService = geminiService;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(IFormFile? file)
    {
        if (file is null || string.IsNullOrWhiteSpace(file.FileName) || file.Length == 0)
        {
            ModelState.AddModelError(nameof(file), "Silakan pilih file PDF terlebih dahulu.");
            return View();
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(file), "File harus berformat PDF.");
            return View();
        }

        if (file.Length > MaxFileSize)
        {
            ModelState.AddModelError(nameof(file), "Ukuran file melebihi batas yang diperbolehkan.");
            return View();
        }

        try
        {
            var requestId = HttpContext.TraceIdentifier;

            _logger.LogInformation(
                "START DOCUMENT ASSESSMENT | RequestId: {RequestId} | File: {FileName}",
                requestId, file.FileName);

            var result = await _geminiService.AnalyzeDocumentAsync(
                file,
                HttpContext.RequestAborted);

            _logger.LogInformation(
                "END DOCUMENT ASSESSMENT | RequestId: {RequestId} | File: {FileName}",
                requestId,
                file.FileName);

            return View("Result", result);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Document assessment failed.");
            ModelState.AddModelError(
                string.Empty,
                "Dokumen gagal dianalisis. Silakan coba lagi.");

            return View();
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

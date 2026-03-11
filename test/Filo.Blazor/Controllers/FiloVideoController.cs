using ManuHub.Filo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Filo.Blazor.Controllers;

[Route("api/video")]
[ApiController]
public class FiloVideoController : ControllerBase
{
    private readonly string _filoPath = "wwwroot/temp/backup.filo";
    private readonly byte[] _key = /* your AES key */ new byte[32];

    [HttpGet("{fileName}")]
    public async Task<IActionResult> StreamVideo(string fileName)
    {
        var reader = new FiloReader(_filoPath);
        await reader.InitializeAsync();

        var filoStream = new FiloStream(reader, fileName);

        return File(filoStream, "video/mp4", enableRangeProcessing: true);
    }
}

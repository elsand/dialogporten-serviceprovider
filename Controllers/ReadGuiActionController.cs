using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Digdir.BDB.Dialogporten.ServiceProvider.Controllers;

[ApiController]
[Authorize]
[Route("guiaction/read")]
public class ReadGuiActionController : ControllerBase
{
    [HttpGet]
    public IActionResult Get(string? xacmlaction)
    {
        var htmlContent = RenderPage();
        return Content(htmlContent, "text/html", Encoding.UTF8);
    }

    private string RenderPage()
    {
        var page =
            """
            <html>
            <head>
            <title>Read Gui Action</title></head>
            <body>
            <h1>Hello from ReadGuiAction</h1>
            
            <p>Logged in user: {0}</p>
            
            </html>
            """;

        return string.Format(page, GetPid());
    }

    private string GetPid()
    {
        return User.Claims.FirstOrDefault(c => c.Type == "pid")?.Value ?? "(unknown)";
    }
}

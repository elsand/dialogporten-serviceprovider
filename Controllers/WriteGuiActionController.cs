using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Digdir.BDB.Dialogporten.ServiceProvider.Controllers;

[ApiController]
[Route("guiaction/write/{xacmlaction?}")]
[Authorize(AuthenticationSchemes = "DialogToken")]
[EnableCors("AllowedOriginsPolicy")]
public class WriteGuiActionController : Controller
{
    [HttpPost]
    public IActionResult Post(string? xacmlaction, [FromQuery] bool isDelayed = false)
    {
        return Ok();
    }

    [HttpPut]
    public IActionResult Put(string? xacmlaction, [FromQuery] bool isDelayed = false)
    {
        return Ok();
    }

    [HttpDelete]
    public IActionResult Delete(string? xacmlaction, [FromQuery] bool isDelayed = false)
    {
        return Ok();
    }
}

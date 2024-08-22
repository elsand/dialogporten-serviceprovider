using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Digdir.BDB.Dialogporten.ServiceProvider.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = "DialogToken")]
[Route("guiaction/write/{xacmlaction?}")]
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

    [HttpOptions]
    [AllowAnonymous]
    public IActionResult Options()
    {
        // Handle the preflight request
        return Ok();
    }
}

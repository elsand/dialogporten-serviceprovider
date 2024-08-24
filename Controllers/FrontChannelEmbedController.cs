using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Digdir.BDB.Dialogporten.ServiceProvider.Controllers;

[ApiController]
[Route("fce")]
[Authorize(AuthenticationSchemes = "DialogToken")]
[EnableCors("AllowedOriginsPolicy")]
public class FrontChannelEmbedController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var sb = new StringBuilder();
        foreach (var claim in User.Claims)
        {
            sb.AppendLine($"* {claim.Type}: {claim.Value}");
        }
        return Content(
            $"""
            # Hello from FrontChannelEmbed
            
            This is a paragraph with some text. [Here is a link](https://www.example.com). Here is some additional text.
            
            * Item 1
            * Item 2
            * Item 3
            
            ## Subsection
            
            This is some more text. Lorem ipsum dolor sit amet, consectetur adipiscing elit sed do eiusmod tempor 
            incididunt ut labore et dolore magna aliqua.
            
            ## Another subsection
            
            Lorem ipsum dolor sit amet, consectetur adipiscing elit sed do eiusmod tempor incididunt ut labore et 
            dolore
            
            ## Dialog token
            
            {sb}
            """, "text/markdown");
    }
}

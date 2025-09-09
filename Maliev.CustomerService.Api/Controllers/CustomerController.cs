using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Maliev.CustomerService.Api.Models;

namespace Maliev.CustomerService.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("customers/v{version:apiVersion}")]
public class CustomerController : ControllerBase
{
    [HttpPost("validate")]
    public IActionResult Validate([FromBody] UserValidationRequest request)
    {
        if(request == null)
        {
            return BadRequest("Request body is null.");
        }
        
        return Ok();
    }
}
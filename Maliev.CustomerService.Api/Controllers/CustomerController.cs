using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.CustomerService.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("customers/v{version:apiVersion}")]
public class CustomerController : ControllerBase
{
    [HttpGet("validate")]
    public IActionResult Validate()
    {
        return Ok();
    }
}
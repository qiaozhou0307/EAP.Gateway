using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace EAP.Gateway.Api.Controllers.Base;

/// <summary>
/// API控制器基类
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public abstract class ApiControllerBase : ControllerBase
{
    private IMediator? _mediator;
    protected IMediator Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();

    protected IActionResult HandleResult<T>(T result) where T : class
    {
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    protected IActionResult HandleException(Exception ex, string operation)
    {
        var logger = HttpContext.RequestServices.GetRequiredService<ILogger<ApiControllerBase>>();
        logger.LogError(ex, "执行操作失败: {Operation}", operation);

        return StatusCode(StatusCodes.Status500InternalServerError,
            new { Message = "An internal error occurred.", Operation = operation });
    }
}

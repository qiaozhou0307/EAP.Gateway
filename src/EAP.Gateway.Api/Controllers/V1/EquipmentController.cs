using EAP.Gateway.Api.Models.Requests;
using EAP.Gateway.Application.Commands.Equipment;
using EAP.Gateway.Application.DTOs;
using EAP.Gateway.Application.Queries.Equipment;
using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EAP.Gateway.Api.Controllers.V1;

/// <summary>
/// 设备管理控制器 - 立即修复版本
/// 支持FR-API-001需求：设备状态查询API
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class EquipmentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EquipmentController> _logger;

    public EquipmentController(IMediator mediator, ILogger<EquipmentController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// 获取设备状态
    /// </summary>
    /// <param name="equipmentId">设备ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设备状态信息</returns>
    [HttpGet("{equipmentId}/status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStatusAsync(
        [FromRoute] string equipmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            // ✅ 修复：使用 EquipmentId.TryCreate 替代 new EquipmentId()
            if (!EquipmentId.TryCreate(equipmentId, out var validEquipmentId))
            {
                _logger.LogWarning("收到无效的设备ID: {EquipmentId}", equipmentId);
                return BadRequest(new
                {
                    Error = "无效的设备ID格式",
                    ProvidedId = equipmentId,
                    ExpectedFormat = "字母、数字、下划线或连字符，最长50字符"
                });
            }

            var query = new GetEquipmentStatusQuery(validEquipmentId!);
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
            {
                return NotFound($"设备 {equipmentId} 未找到或状态不可用");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备状态失败 - 设备ID: {EquipmentId}", equipmentId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Error = "获取设备状态时发生内部错误", EquipmentId = equipmentId });
        }
    }

    /// <summary>
    /// 获取所有设备状态 - 推荐版本
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>所有设备状态列表</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(IEnumerable<EquipmentStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetAllEquipmentStatusQuery();
            var result = await _mediator.Send(query, cancellationToken);

            // 直接返回结果，如果为null则返回空集合
            return Ok(result ?? []);
            //return Ok(result ?? Enumerable.Empty<EquipmentStatusDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有设备状态失败");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Error = "获取所有设备状态时发生内部错误" });
        }
    }

    /// <summary>
    /// 连接设备
    /// </summary>
    /// <param name="equipmentId">设备ID</param>
    /// <param name="forceReconnect">是否强制重连</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>连接结果</returns>
    [HttpPost("{equipmentId}/connect")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConnectAsync(
        [FromRoute] EquipmentId equipmentId,
        [FromQuery] bool forceReconnect = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new ConnectEquipmentCommand(equipmentId, forceReconnect);
            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccessful)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "连接设备失败 - 设备ID: {EquipmentId}", equipmentId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Error = "连接设备时发生内部错误", EquipmentId = equipmentId });
        }
    }

    /// <summary>
    /// 断开设备连接
    /// </summary>
    /// <param name="equipmentId">设备ID</param>
    /// <param name="reason">断开原因</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>断开结果</returns>
    [HttpPost("{equipmentId}/disconnect")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DisconnectAsync(
        [FromRoute] EquipmentId equipmentId,
        [FromQuery] string reason = "Manual disconnect",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new DisconnectEquipmentCommand(equipmentId, reason);
            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccessful)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "断开设备连接失败 - 设备ID: {EquipmentId}", equipmentId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Error = "断开设备连接时发生内部错误", EquipmentId = equipmentId });
        }
    }

    /// <summary>
    /// 发送远程命令
    /// </summary>
    /// <param name="equipmentId">设备ID</param>
    /// <param name="command">命令名称</param>
    /// <param name="request">命令请求体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>命令执行结果</returns>
    [HttpPost("{equipmentId}/commands/{command}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendCommandAsync(
        [FromRoute] string equipmentId,
        [FromRoute] string command,
        [FromBody] SendCommandRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // ✅ 修复：使用 EquipmentId.TryCreate 替代 new EquipmentId()
            if (!EquipmentId.TryCreate(equipmentId, out var validEquipmentId))
            {
                return BadRequest(new
                {
                    Error = "无效的设备ID格式",
                    ProvidedId = equipmentId
                });
            }

            if (string.IsNullOrWhiteSpace(command))
            {
                return BadRequest(new { Error = "命令名称不能为空" });
            }

            var sendCommand = new SendRemoteCommandCommand(
                validEquipmentId!,
                command,
                request?.Parameters,
                User?.Identity?.Name ?? "Unknown");

            var result = await _mediator.Send(sendCommand, cancellationToken);

            if (result.IsSuccessful)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送远程命令失败 - 设备ID: {EquipmentId}, 命令: {Command}",
                equipmentId, command);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Error = "发送远程命令时发生内部错误", EquipmentId = equipmentId, Command = command });
        }
    }

    /// <summary>
    /// 获取设备详细信息
    /// </summary>
    /// <param name="equipmentId">设备ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设备详细信息</returns>
    [HttpGet("{equipmentId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEquipmentDetailsAsync(
        [FromRoute] string equipmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            // ✅ 修复：使用 EquipmentId.TryCreate 替代 new EquipmentId()
            if (!EquipmentId.TryCreate(equipmentId, out var validEquipmentId))
            {
                return BadRequest(new
                {
                    Error = "无效的设备ID格式",
                    ProvidedId = equipmentId
                });
            }

            var query = new GetEquipmentDetailsQuery(validEquipmentId!);
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
            {
                return NotFound($"设备 {equipmentId} 未找到");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备详情失败 - 设备ID: {EquipmentId}", equipmentId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Error = "获取设备详情时发生内部错误", EquipmentId = equipmentId });
        }
    }
}

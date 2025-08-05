using System.ComponentModel.DataAnnotations;
using EAP.Gateway.Core.Models;
using EAP.Gateway.Core.Services;
using EAP.Gateway.Core.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace EAP.Gateway.Api.Controllers.V1;

/// <summary>
/// 裂片机管理控制器
/// </summary>
[ApiController]
[Route("api/v1/dicing-machines")]
public class DicingMachineController : ControllerBase
{
    private readonly IMultiDicingMachineConnectionManager _connectionManager;
    private readonly ILogger<DicingMachineController> _logger;

    public DicingMachineController(
        IMultiDicingMachineConnectionManager connectionManager,
        ILogger<DicingMachineController> logger)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取所有裂片机状态
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<IEnumerable<DicingMachineStatus>>> GetAllStatusAsync()
    {
        try
        {
            var statuses = await _connectionManager.GetAllDicingMachineStatusAsync();
            return Ok(statuses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取裂片机状态失败");
            return StatusCode(500, "获取状态失败");
        }
    }

    /// <summary>
    /// 获取连接统计信息
    /// </summary>
    [HttpGet("statistics")]
    public ActionResult<ConnectionStatistics> GetStatistics()
    {
        try
        {
            var statistics = _connectionManager.GetConnectionStatistics();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取连接统计失败");
            return StatusCode(500, "获取统计失败");
        }
    }

    /// <summary>
    /// 手动添加并连接裂片机
    /// </summary>
    [HttpPost("connect")]
    public async Task<ActionResult<DicingMachineConnectionResult>> ConnectDicingMachineAsync(
        [FromBody] ConnectDicingMachineRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("🔌 手动连接裂片机请求: {IP}:{Port}", request.IpAddress, request.Port);

            var result = await _connectionManager.AddAndConnectDicingMachineAsync(
                request.IpAddress,
                request.Port,
                request.ExpectedMachineNumber,
                TimeSpan.FromSeconds(request.TimeoutSeconds));

            if (result.IsSuccessful)
            {
                _logger.LogInformation("✅ 手动连接成功: {MachineNumber}", result.MachineMetadata?.MachineNumber);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("❌ 手动连接失败: {Error}", result.ErrorMessage);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "手动连接裂片机异常");
            return StatusCode(500, "连接失败");
        }
    }

    /// <summary>
    /// 重连指定裂片机
    /// </summary>
    [HttpPost("{machineNumber}/reconnect")]
    public async Task<ActionResult<bool>> ReconnectDicingMachineAsync(string machineNumber)
    {
        try
        {
            _logger.LogInformation("🔄 重连裂片机请求: {MachineNumber}", machineNumber);

            var success = await _connectionManager.ReconnectDicingMachineAsync(machineNumber);

            if (success)
            {
                _logger.LogInformation("✅ 重连成功: {MachineNumber}", machineNumber);
                return Ok(true);
            }
            else
            {
                _logger.LogWarning("❌ 重连失败: {MachineNumber}", machineNumber);
                return BadRequest(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重连裂片机异常: {MachineNumber}", machineNumber);
            return StatusCode(500, "重连失败");
        }
    }

    /// <summary>
    /// 断开所有裂片机连接
    /// </summary>
    [HttpPost("disconnect-all")]
    public async Task<ActionResult> DisconnectAllAsync()
    {
        try
        {
            _logger.LogInformation("🛑 断开所有裂片机连接请求");
            await _connectionManager.DisconnectAllDicingMachinesAsync();
            return Ok("所有连接已断开");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "断开所有连接异常");
            return StatusCode(500, "断开连接失败");
        }
    }
}

/// <summary>
/// 连接裂片机请求模型
/// </summary>
public class ConnectDicingMachineRequest
{
    /// <summary>
    /// IP地址
    /// </summary>
    [Required]
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// 端口号
    /// </summary>
    [Range(1, 65535)]
    public int Port { get; set; } = 5000;

    /// <summary>
    /// 期望的裂片机编号
    /// </summary>
    public string? ExpectedMachineNumber { get; set; }

    /// <summary>
    /// 连接超时时间（秒）
    /// </summary>
    [Range(5, 300)]
    public int TimeoutSeconds { get; set; } = 30;
}

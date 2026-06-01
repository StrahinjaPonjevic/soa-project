using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourService.DTOs;
using TourService.Models;
using TourService.Services;

namespace TourService.Controllers;

[ApiController]
[Route("internal/rpc")]
public class TourRpcController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ITourManagementService _tourManagementService;
    private readonly ITourExecutionService _tourExecutionService;

    public TourRpcController(
        ICurrentUserService currentUserService,
        ITourManagementService tourManagementService,
        ITourExecutionService tourExecutionService)
    {
        _currentUserService = currentUserService;
        _tourManagementService = tourManagementService;
        _tourExecutionService = tourExecutionService;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Execute([FromBody] JsonRpcRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.TryGetCurrentUser(User, out var currentUser))
        {
            return Ok(JsonRpcResponse.Failure(request.Id, 401, "Missing user claims in token."));
        }

        try
        {
            return request.Method switch
            {
                "TourService.PublishTour" => Ok(JsonRpcResponse.Success(
                    request.Id,
                    ToursController.ToAuthorResponse(
                        await _tourManagementService.PublishAsync(
                            ExtractParam<int>(request.Params, "tourId"),
                            currentUser!,
                            cancellationToken)))),

                "TourService.ArchiveTour" => Ok(JsonRpcResponse.Success(
                    request.Id,
                    ToursController.ToAuthorResponse(
                        await _tourManagementService.ArchiveAsync(
                            ExtractParam<int>(request.Params, "tourId"),
                            currentUser!,
                            cancellationToken)))),

                // RPC 1: Tourist starts a tour — creates TourExecution
                "TourExecutionService.StartTour" => Ok(JsonRpcResponse.Success(
                    request.Id,
                    ToExecutionDto(
                        await _tourExecutionService.StartTourAsync(
                            ExtractParam<int>(request.Params, "tourId"),
                            currentUser!.UserId,
                            Request.Headers.Authorization.ToString(),
                            cancellationToken)))),

                // RPC 2: Check if tourist is near an uncompleted key point
                "TourExecutionService.CheckNearbyKeyPoint" => Ok(JsonRpcResponse.Success(
                    request.Id,
                    ToCheckNearbyDto(
                        await _tourExecutionService.CheckNearbyKeyPointAsync(
                            ExtractParam<int>(request.Params, "executionId"),
                            currentUser!.UserId,
                            cancellationToken)))),

                // Compensation RPC — called by SAGA orchestrator to roll back StartTour
                "TourExecutionService.CancelExecution" => await HandleCancelAsync(request, cancellationToken),

                _ => Ok(JsonRpcResponse.Failure(request.Id, 404, $"Unknown RPC method '{request.Method}'."))
            };
        }
        catch (TourOperationException ex)
        {
            return Ok(JsonRpcResponse.Failure(request.Id, ex.StatusCode, ex.Message, ex.Errors));
        }
    }

    private async Task<IActionResult> HandleCancelAsync(JsonRpcRequest request, CancellationToken ct)
    {
        await _tourExecutionService.CancelAsync(
            ExtractParam<int>(request.Params, "executionId"),
            ct);
        return Ok(JsonRpcResponse.Success(request.Id, new { cancelled = true }));
    }

    private static T ExtractParam<T>(JsonElement? parameters, string name) where T : struct
    {
        if (parameters is null || !parameters.Value.TryGetProperty(name, out var element))
            throw new TourOperationException(400, $"RPC parameter '{name}' is required.");

        if (typeof(T) == typeof(int))
        {
            if (!element.TryGetInt32(out var intVal))
                throw new TourOperationException(400, $"RPC parameter '{name}' must be an integer.");
            return (T)(object)intVal;
        }

        throw new TourOperationException(400, $"Unsupported parameter type for '{name}'.");
    }

    internal static TourExecutionResponseDto ToExecutionDto(TourExecution e)
    {
        return new TourExecutionResponseDto
        {
            Id = e.Id,
            TourId = e.TourId,
            TouristId = e.TouristId,
            Status = e.Status.ToString(),
            StartedAtUtc = e.StartedAtUtc,
            CompletedAtUtc = e.CompletedAtUtc,
            AbandonedAtUtc = e.AbandonedAtUtc,
            LastActivityAtUtc = e.LastActivityAtUtc,
            InitialLatitude = e.InitialLatitude,
            InitialLongitude = e.InitialLongitude,
            CompletedKeyPoints = e.CompletedKeyPoints
                .Select(ckp => new CompletedKeyPointDto
                {
                    KeyPointId = ckp.KeyPointId,
                    CompletedAtUtc = ckp.CompletedAtUtc
                })
                .ToList()
        };
    }

    internal static CheckNearbyResponseDto ToCheckNearbyDto(CheckNearbyResult result)
    {
        return new CheckNearbyResponseDto
        {
            IsNearKeyPoint = result.IsNearKeyPoint,
            KeyPointId = result.KeyPointId,
            KeyPointName = result.KeyPointName,
            IsNewlyCompleted = result.IsNewlyCompleted,
            AllKeyPointsCompleted = result.AllKeyPointsCompleted,
            Execution = ToExecutionDto(result.Execution)
        };
    }

    public sealed class JsonRpcRequest
    {
        public string JsonRpc { get; set; } = "2.0";
        public string Method { get; set; } = string.Empty;
        public JsonElement? Params { get; set; }
        public JsonElement? Id { get; set; }
    }

    public sealed class JsonRpcResponse
    {
        public string JsonRpc { get; set; } = "2.0";
        public JsonElement? Id { get; set; }
        public object? Result { get; set; }
        public JsonRpcError? Error { get; set; }

        public static JsonRpcResponse Success(JsonElement? id, object result)
        {
            return new JsonRpcResponse
            {
                Id = id,
                Result = result
            };
        }

        public static JsonRpcResponse Failure(JsonElement? id, int code, string message, IReadOnlyList<string>? details = null)
        {
            return new JsonRpcResponse
            {
                Id = id,
                Error = new JsonRpcError
                {
                    Code = code,
                    Message = message,
                    Details = details ?? Array.Empty<string>()
                }
            };
        }
    }

    public sealed class JsonRpcError
    {
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
        public IReadOnlyList<string> Details { get; set; } = Array.Empty<string>();
    }
}

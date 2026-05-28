using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourService.Services;

namespace TourService.Controllers;

[ApiController]
[Authorize(Roles = "Guide")]
[Route("internal/rpc")]
public class TourRpcController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ITourManagementService _tourManagementService;

    public TourRpcController(
        ICurrentUserService currentUserService,
        ITourManagementService tourManagementService)
    {
        _currentUserService = currentUserService;
        _tourManagementService = tourManagementService;
    }

    [HttpPost]
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
                            ExtractTourId(request.Params),
                            currentUser!,
                            cancellationToken)))),
                "TourService.ArchiveTour" => Ok(JsonRpcResponse.Success(
                    request.Id,
                    ToursController.ToAuthorResponse(
                        await _tourManagementService.ArchiveAsync(
                            ExtractTourId(request.Params),
                            currentUser!,
                            cancellationToken)))),
                _ => Ok(JsonRpcResponse.Failure(request.Id, 404, $"Unknown RPC method '{request.Method}'."))
            };
        }
        catch (TourOperationException ex)
        {
            return Ok(JsonRpcResponse.Failure(request.Id, ex.StatusCode, ex.Message, ex.Errors));
        }
    }

    private static int ExtractTourId(JsonElement? parameters)
    {
        if (parameters is null ||
            !parameters.Value.TryGetProperty("tourId", out var tourIdElement) ||
            !tourIdElement.TryGetInt32(out var tourId))
        {
            throw new TourOperationException(400, "RPC parameter 'tourId' is required.");
        }

        return tourId;
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

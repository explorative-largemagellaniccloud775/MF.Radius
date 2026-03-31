using System.Diagnostics.CodeAnalysis;
using System.Net;
using MF.Radius.SampleServer.Application.Abstractions.Messaging;
using MF.Radius.SampleServer.Application.Features.Nas.Commands;
using MF.Radius.SampleServer.Application.Features.Nas.Models;
using MF.Radius.SampleServer.Presentation.WebApi.Contracts;
using MF.Radius.SampleServer.Shared.Contracts;
using MF.Radius.SampleServer.Shared.DTOs.Rr;

namespace MF.Radius.SampleServer.Presentation.WebApi.Endpoints;

public static class NasCommandEndpoints
{
    public static RouteGroupBuilder MapNasCommandEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/commands");
        
        group.MapPost("/restrict", HandleRestrictAsync)
            .WithName(EndpointNames.PostNasRestrict)
            .WithSummary("Sends RADIUS CoA-Request to apply ACL restriction.")
            .WithDescription("HTTP 200 for Success/Rejected (see data.status/code). HTTP 400 invalid endpoint/input. HTTP 504 timeout. HTTP 502 transport/protocol failure.")
            .Produces<ApiEnvelope<NasCommandResult>>()
            .Produces<ApiEnvelope<NasCommandResult>>(StatusCodes.Status400BadRequest)
            .Produces<ApiEnvelope<NasCommandResult>>(StatusCodes.Status502BadGateway)
            .Produces<ApiEnvelope<NasCommandResult>>(StatusCodes.Status504GatewayTimeout);

        group.MapPost("/disconnect", HandleDisconnectAsync)
            .WithName(EndpointNames.PostNasDisconnect)
            .WithSummary("Sends RADIUS Disconnect-Request (DM) to NAS.")
            .WithDescription("HTTP 200 for Success/Rejected (see data.status/code). HTTP 400 invalid endpoint/input. HTTP 504 timeout. HTTP 502 transport/protocol failure.")
            .Produces<ApiEnvelope<NasCommandResult>>()
            .Produces<ApiEnvelope<NasCommandResult>>(StatusCodes.Status400BadRequest)
            .Produces<ApiEnvelope<NasCommandResult>>(StatusCodes.Status502BadGateway)
            .Produces<ApiEnvelope<NasCommandResult>>(StatusCodes.Status504GatewayTimeout);
        
        return api;
    }
    
    private static async Task<IResult> HandleRestrictAsync(
        RestrictRequest request,
        HttpContext httpContext,
        ICommandDispatcher dispatcher,
        CancellationToken ct
    )
    {
        if (!TryParseNasEndPoint(request.NasIp, request.NasPort, out var nasEp))
            return ApiResponseFactory.Fail<NasCommandResult>(
                httpContext,
                StatusCodes.Status400BadRequest,
                ApiCodes.Nas.InvalidEndpoint,
                ApiMessages.InvalidNasEndpoint,
                target: nameof(request.NasIp)
            );

        var command = new RestrictSessionCommand
        {
            NasEndPoint = nasEp,
            SessionId = request.SessionId,
            UserName = request.UserName,
            AclName = request.AclName
        };
        var result = await dispatcher.SendAsync<RestrictSessionCommand, NasCommandResult>(command, ct);
        return ToHttpResult(httpContext, result);
    }
    
    private static async Task<IResult> HandleDisconnectAsync(
        DisconnectRequest request,
        HttpContext httpContext,
        ICommandDispatcher dispatcher,
        CancellationToken ct
    )
    {
        if (!TryParseNasEndPoint(request.NasIp, request.NasPort, out var nasEp))
            return ApiResponseFactory.Fail<NasCommandResult>(
                httpContext,
                StatusCodes.Status400BadRequest,
                ApiCodes.Nas.InvalidEndpoint,
                ApiMessages.InvalidNasEndpoint,
                target: nameof(request.NasIp)
            );

        var command = new DisconnectSessionCommand
        {
            NasEndPoint = nasEp,
            SessionId = request.SessionId,
            UserName = request.UserName
        };
        var result = await dispatcher.SendAsync<DisconnectSessionCommand, NasCommandResult>(command, ct);
        return ToHttpResult(httpContext, result);
    }

    private static bool TryParseNasEndPoint(
        string nasIp, 
        int nasPort, 
        [NotNullWhen(true)] out IPEndPoint? nasEndPoint
    )
    {
        nasEndPoint = null;

        if (!IPAddress.TryParse(nasIp, out var ip)) 
            return false;

        if ((uint)nasPort > ushort.MaxValue)
            return false;

        nasEndPoint = new IPEndPoint(ip, nasPort);
        return true;
    }

    private static IResult ToHttpResult(HttpContext httpContext, NasCommandResult result) =>
        result.Status switch
        {
            NasCommandStatus.Success => ApiResponseFactory.Success(
                httpContext,
                result,
                ApiCodes.Nas.Success
            ),
            NasCommandStatus.Rejected => ApiResponseFactory.Success(
                httpContext,
                result,
                ApiCodes.Nas.Rejected
            ),
            NasCommandStatus.InvalidInput => ApiResponseFactory.Fail<NasCommandResult>(
                httpContext,
                StatusCodes.Status400BadRequest,
                ApiCodes.Nas.InvalidInput,
                result.ErrorMessage ?? ApiMessages.InvalidNasEndpoint
            ),
            NasCommandStatus.Timeout => ApiResponseFactory.Fail<NasCommandResult>(
                httpContext,
                StatusCodes.Status504GatewayTimeout,
                ApiCodes.Nas.Timeout,
                result.ErrorMessage ?? ApiMessages.NasCommandTimeout
            ),
            _ => ApiResponseFactory.Fail<NasCommandResult>(
                httpContext,
                StatusCodes.Status502BadGateway,
                ApiCodes.Nas.TransportFailure,
                result.ErrorMessage ?? ApiMessages.NasCommandFailed
            )
        };
    
}

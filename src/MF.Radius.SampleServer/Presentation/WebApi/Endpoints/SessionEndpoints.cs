using System.Net;
using MF.Radius.SampleServer.Application.Abstractions.Messaging;
using MF.Radius.SampleServer.Application.Features.Sessions.Queries;
using MF.Radius.SampleServer.Presentation.WebApi.Contracts;
using MF.Radius.SampleServer.Shared.Contracts;
using MF.Radius.SampleServer.Shared.DTOs;

namespace MF.Radius.SampleServer.Presentation.WebApi.Endpoints;

public static class SessionEndpoints
{
    
    public static RouteGroupBuilder MapSessionEndpoints(this RouteGroupBuilder api)
    {
        var sessions = api.MapGroup("/sessions");

        sessions.MapGet("/", GetAll)
            .WithName(EndpointNames.GetAllSessions)
            .WithSummary("Returns all active sessions.")
            .Produces<ApiEnvelope<IReadOnlyList<SessionDto>>>();
        
        sessions.MapGet("/{userName}", GetByUserNameAsync)
            .WithName(EndpointNames.GetSessionsByUserName)
            .WithSummary("Returns active sessions for a subscriber.")
            .WithDescription("The data array can be empty when no active sessions are found.")
            .Produces<ApiEnvelope<IReadOnlyList<SessionDto>>>();
        
        sessions.MapGet("/by-id", GetByIdAsync)
            .WithName(EndpointNames.GetSessionByNasAndId)
            .WithSummary("Returns one session by NAS endpoint and Acct-Session-Id.")
            .WithDescription("HTTP 400 for invalid NAS IP; HTTP 404 when session is not found.")
            .Produces<ApiEnvelope<SessionDto>>()
            .Produces<ApiEnvelope<SessionDto>>(StatusCodes.Status400BadRequest)
            .Produces<ApiEnvelope<SessionDto>>(StatusCodes.Status404NotFound);

        return api;
    }

    private static async Task<IResult> GetAll(
        HttpContext httpContext,
        IQueryDispatcher queryDispatcher,
        CancellationToken ct
    )
    {
        var query = new GetSessionsAllQuery();
        var sessions = await queryDispatcher.SendAsync<GetSessionsAllQuery, IReadOnlyList<SessionDto>>(query, ct);
        return ApiResponseFactory.Success(
            httpContext,
            sessions,
            ApiCodes.Sessions.ListReturned
        );
    }
    
    private static async Task<IResult> GetByUserNameAsync(
        string userName,
        HttpContext httpContext,
        IQueryDispatcher queryDispatcher,
        CancellationToken ct
    )
    {
        var query = new GetSessionsByUserNameQuery
        {
            UserName = userName,
        };
        var sessions = 
            await queryDispatcher.SendAsync<GetSessionsByUserNameQuery, IReadOnlyList<SessionDto>>(query, ct);
        return ApiResponseFactory.Success(
            httpContext,
            sessions,
            ApiCodes.Sessions.ListReturned
        );
    }

    private static async Task<IResult> GetByIdAsync(
        string nasIp,
        int nasPort,
        string sessionId,
        HttpContext httpContext,
        IQueryDispatcher queryDispatcher,
        CancellationToken ct
    )
    {
        if (!IPAddress.TryParse(nasIp, out var nasIpParsed))
            return ApiResponseFactory.Fail<SessionDto>(
                httpContext,
                StatusCodes.Status400BadRequest,
                ApiCodes.Sessions.InvalidNasIp,
                ApiMessages.InvalidNasIp,
                target: nameof(nasIp)
            );
        var query = new GetSessionByNasAndIdQuery
        {
            NasIpEndPoint = new IPEndPoint(nasIpParsed, nasPort),
            SessionId = sessionId,
        };
        var session = await queryDispatcher.SendAsync<GetSessionByNasAndIdQuery, SessionDto?>(query, ct);
        return session != null
            ? ApiResponseFactory.Success(
                httpContext,
                session,
                ApiCodes.Sessions.Found
            )
            : ApiResponseFactory.Fail<SessionDto>(
                httpContext,
                StatusCodes.Status404NotFound,
                ApiCodes.Sessions.NotFoundByNasAndId,
                ApiMessages.SessionNotFound,
                target: nameof(sessionId)
            );
    }
    
}
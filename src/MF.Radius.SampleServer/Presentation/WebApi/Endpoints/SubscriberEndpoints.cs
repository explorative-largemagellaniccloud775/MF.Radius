using MF.Radius.SampleServer.Application.Abstractions.Messaging;
using MF.Radius.SampleServer.Application.Features.Subscribers.Queries;
using MF.Radius.SampleServer.Presentation.WebApi.Contracts;
using MF.Radius.SampleServer.Shared.Contracts;
using MF.Radius.SampleServer.Shared.DTOs;

namespace MF.Radius.SampleServer.Presentation.WebApi.Endpoints;

public static class SubscriberEndpoints
{
    
    public static RouteGroupBuilder MapSubscriberEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/subscribers");

        group.MapGet("/{userName}", GetByUserNameAsync)
            .WithName(EndpointNames.GetSubscriberByUserName)
            .WithSummary("Gets subscriber profile by User-Name.")
            .WithDescription("HTTP 404 when subscriber is not found.")
            .Produces<ApiEnvelope<SubscriberDto>>()
            .Produces<ApiEnvelope<SubscriberDto>>(StatusCodes.Status404NotFound);

        return api;
    }

    private static async Task<IResult> GetByUserNameAsync(
        string userName,
        HttpContext httpContext,
        IQueryDispatcher queryDispatcher,
        CancellationToken ct
    )
    {
        var subscriber = await queryDispatcher.SendAsync<GetSubscriberQuery, SubscriberDto?>(
            new GetSubscriberQuery
            {
                UserName = userName,
            },
            ct
        );
        return subscriber != null
            ? ApiResponseFactory.Success(
                httpContext,
                subscriber,
                ApiCodes.Subscribers.Found
            )
            : ApiResponseFactory.Fail<SubscriberDto>(
                httpContext,
                StatusCodes.Status404NotFound,
                ApiCodes.Subscribers.NotFoundByUserName,
                ApiMessages.SubscriberNotFound,
                target: nameof(userName)
            );
    }
    
}

using Api.Common.Infrastructure;
using Domain.ParkingSpots;
using FastEndpoints;
using FluentValidation;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Api.Bookings;

[PublicAPI]
public sealed record MakeMySpotAvailableRequest
{
    public required DateTimeOffset From { get; init; }
    public required DateTimeOffset To { get; init; }
}

[PublicAPI]
public sealed record MakeMySpotAvailableResponse
{
    public required decimal EarnedCredits { get; init; }
}

internal sealed class MakeMySpotAvailableValidator : Validator<MakeMySpotAvailableRequest>
{
    public MakeMySpotAvailableValidator()
    {
        RuleFor(x => x.To).GreaterThan(x => x.From);
        RuleFor(x => x.From).GreaterThanOrEqualTo(_ => DateTimeOffset.UtcNow);
    }
}

internal sealed class MakeMySpotAvailable(AppDbContext dbContext)
    : Endpoint<MakeMySpotAvailableRequest, MakeMySpotAvailableResponse>
{
    public override void Configure()
    {
        Post("/@me/spot/availability");
    }

    public override async Task HandleAsync(MakeMySpotAvailableRequest req, CancellationToken ct)
    {
        var parkingLot = await dbContext.Set<ParkingLot>().FirstOrDefaultAsync(ct);

        if (parkingLot is null)
        {
            ThrowError("No spot defined");
            return;
        }

        var earnedCredits = parkingLot.MakeAvailable(req.From, req.To);

        dbContext.Set<ParkingLot>().Update(parkingLot);
        await dbContext.SaveChangesAsync(ct);

        await SendOkAsync(
            new MakeMySpotAvailableResponse
            {
                EarnedCredits = earnedCredits
            },
            ct);
    }
}

using Drive.Application.Features.DriveItems.Models;
using MediatR;

namespace Drive.Application.Features.DriveItems.Queries.ListAssignments;

public sealed record ListAssignmentsQuery(Guid DriveItemId)
    : IRequest<IReadOnlyList<AssignmentResult>?>;

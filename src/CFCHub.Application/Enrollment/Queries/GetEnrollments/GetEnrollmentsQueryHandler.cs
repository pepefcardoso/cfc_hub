using CFCHub.Domain.Shared;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CFCHub.Application.Enrollment.Queries.GetEnrollments;

public class GetEnrollmentsQueryHandler : IRequestHandler<GetEnrollmentsQuery, PagedResult<EnrollmentResult>>
{
    public Task<PagedResult<EnrollmentResult>> Handle(GetEnrollmentsQuery request, CancellationToken cancellationToken)
    {
        var result = new PagedResult<EnrollmentResult>(new List<EnrollmentResult>(), null, false, 0);
        return Task.FromResult(result);
    }
}

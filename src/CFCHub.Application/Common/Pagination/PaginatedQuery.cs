using MediatR;

namespace CFCHub.Application.Common.Pagination;

public abstract record PaginatedQuery<TResult>(string? Cursor, int Limit = 20) : IRequest<TResult>;

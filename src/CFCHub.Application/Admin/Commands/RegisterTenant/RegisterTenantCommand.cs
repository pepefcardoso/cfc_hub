using System;
using MediatR;

namespace CFCHub.Application.Admin.Commands.RegisterTenant;

public record RegisterTenantCommand(
    string Name,
    string Slug,
    string ContactEmail,
    string Cnpj
) : IRequest<RegisterTenantResult>;

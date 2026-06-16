using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CFCHub.Infrastructure.Persistence;

public class TenantRegistry : ITenantRegistry
{
    private readonly AppDbContext _context;

    public TenantRegistry(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsSlugUniqueAsync(string slug, CancellationToken ct = default)
    {
        var connection = _context.Database.GetDbConnection();
        var wasClosed = connection.State == ConnectionState.Closed;
        if (wasClosed) await connection.OpenAsync(ct);

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM public.tenants WHERE slug = @slug;";
            
            var param = command.CreateParameter();
            param.ParameterName = "@slug";
            param.Value = slug;
            command.Parameters.Add(param);

            var result = await command.ExecuteScalarAsync(ct);
            return Convert.ToInt64(result) == 0;
        }
        finally
        {
            if (wasClosed) await connection.CloseAsync();
        }
    }

    public async Task<TenantRecord?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var connection = _context.Database.GetDbConnection();
        var wasClosed = connection.State == ConnectionState.Closed;
        if (wasClosed) await connection.OpenAsync(ct);

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, slug, schema_name, status FROM public.tenants WHERE slug = @slug LIMIT 1;";
            
            var param = command.CreateParameter();
            param.ParameterName = "@slug";
            param.Value = slug;
            command.Parameters.Add(param);

            using var reader = await command.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                return new TenantRecord(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3)
                );
            }
            return null;
        }
        finally
        {
            if (wasClosed) await connection.CloseAsync();
        }
    }
}

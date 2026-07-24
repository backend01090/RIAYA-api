using Riaya.Api.Common;
using Riaya.Api.Data;
using Riaya.Api.DTOs.Billing;
using Riaya.Api.Entities;
using Riaya.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Riaya.Api.Services;

public class MedicalServiceService : IMedicalServiceService
{
    private readonly AppDbContext _context;

    public MedicalServiceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<MedicalServiceDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.MedicalServices
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new MedicalServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                Price = s.Price,
                IsActive = s.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<MedicalServiceDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.MedicalServices
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new MedicalServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                Price = s.Price,
                IsActive = s.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceResult<MedicalServiceDto>> CreateAsync(UpsertMedicalServiceDto dto, CancellationToken cancellationToken = default)
    {
        var name = dto.Name.Trim();
        var duplicateExists = await _context.MedicalServices
            .AnyAsync(s => s.IsActive && s.Name.ToLower() == name.ToLower(), cancellationToken);

        if (duplicateExists && dto.IsActive)
            return ServiceResult<MedicalServiceDto>.Fail("Active medical service name already exists.", ErrorType.Conflict, "MedicalServiceNameExists");

        var service = new MedicalService
        {
            Name = name,
            Price = dto.Price,
            IsActive = dto.IsActive
        };

        _context.MedicalServices.Add(service);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<MedicalServiceDto>.Ok(await GetByIdAsync(service.Id, cancellationToken), "Medical service created successfully.");
    }

    public async Task<ServiceResult<MedicalServiceDto>> UpdateAsync(int id, UpsertMedicalServiceDto dto, CancellationToken cancellationToken = default)
    {
        var service = await _context.MedicalServices.FindAsync(new object[] { id }, cancellationToken);
        if (service is null)
            return ServiceResult<MedicalServiceDto>.Fail("Medical service not found.", ErrorType.NotFound, "MedicalServiceNotFound");

        var name = dto.Name.Trim();
        var duplicateExists = await _context.MedicalServices.AnyAsync(s =>
            s.Id != id &&
            s.IsActive &&
            s.Name.ToLower() == name.ToLower(),
            cancellationToken);

        if (duplicateExists && dto.IsActive)
            return ServiceResult<MedicalServiceDto>.Fail("Active medical service name already exists.", ErrorType.Conflict, "MedicalServiceNameExists");

        service.Name = name;
        service.Price = dto.Price;
        service.IsActive = dto.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<MedicalServiceDto>.Ok(await GetByIdAsync(service.Id, cancellationToken), "Medical service updated successfully.");
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var service = await _context.MedicalServices.FindAsync(new object[] { id }, cancellationToken);
        if (service is null)
            return ServiceResult.Fail("Medical service not found.", ErrorType.NotFound, "MedicalServiceNotFound");

        var hasInvoiceItems = await _context.InvoiceItems.AnyAsync(i => i.MedicalServiceId == id, cancellationToken);
        if (hasInvoiceItems)
            return ServiceResult.Fail("Cannot delete medical service because invoice items are linked to it.", ErrorType.BusinessRule, "MedicalServiceHasInvoiceItems");

        _context.MedicalServices.Remove(service);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok("Medical service deleted successfully.");
    }
}

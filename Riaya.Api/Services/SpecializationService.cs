using Riaya.Api.Common;
using Riaya.Api.Data;
using Riaya.Api.DTOs.Specialization;
using Riaya.Api.Entities;
using Riaya.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Riaya.Api.Services;

public class SpecializationService : ISpecializationService
{
    private readonly AppDbContext _context;

    public SpecializationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<SpecializationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Specializations
            .OrderBy(s => s.Name)
            .Select(s => new SpecializationDto
            {
                Id = s.Id,
                Name = s.Name
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<SpecializationDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Specializations
            .Where(s => s.Id == id)
            .Select(s => new SpecializationDto
            {
                Id = s.Id,
                Name = s.Name
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceResult<SpecializationDto>> CreateAsync(CreateSpecializationDto dto, CancellationToken cancellationToken = default)
    {
        var name = dto.Name.Trim();

        var exists = await _context.Specializations
            .AnyAsync(s => s.Name.ToLower() == name.ToLower(), cancellationToken);

        if (exists)
            return ServiceResult<SpecializationDto>.Fail("Specialization name already exists.", ErrorType.Conflict, "SpecializationNameExists");

        var specialization = new Specialization
        {
            Name = name
        };

        _context.Specializations.Add(specialization);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<SpecializationDto>.Ok(new SpecializationDto
        {
            Id = specialization.Id,
            Name = specialization.Name
        }, "Specialization created successfully.");
    }

    public async Task<ServiceResult<SpecializationDto>> UpdateAsync(int id, CreateSpecializationDto dto, CancellationToken cancellationToken = default)
    {
        var specialization = await _context.Specializations.FindAsync(new object[] { id }, cancellationToken);
        if (specialization is null)
            return ServiceResult<SpecializationDto>.Fail("Specialization not found.", ErrorType.NotFound, "SpecializationNotFound");

        var name = dto.Name.Trim();

        var exists = await _context.Specializations
            .AnyAsync(s => s.Id != id && s.Name.ToLower() == name.ToLower(), cancellationToken);

        if (exists)
            return ServiceResult<SpecializationDto>.Fail("Specialization name already exists.", ErrorType.Conflict, "SpecializationNameExists");

        specialization.Name = name;
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<SpecializationDto>.Ok(new SpecializationDto
        {
            Id = specialization.Id,
            Name = specialization.Name
        }, "Specialization updated successfully.");
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var specialization = await _context.Specializations.FindAsync(new object[] { id }, cancellationToken);
        if (specialization is null)
            return ServiceResult.Fail("Specialization not found.", ErrorType.NotFound, "SpecializationNotFound");

        var hasDoctors = await _context.Doctors.AnyAsync(d => d.SpecializationId == id, cancellationToken);
        if (hasDoctors)
            return ServiceResult.Fail("Cannot delete specialization because there are doctors linked to this specialization.", ErrorType.BusinessRule, "SpecializationHasDoctors");

        _context.Specializations.Remove(specialization);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok("Specialization deleted successfully.");
    }
}

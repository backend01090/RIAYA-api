using Riaya.Api.Common;
using Riaya.Api.Data;
using Riaya.Api.DTOs.Department;
using Riaya.Api.Entities;
using Riaya.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Riaya.Api.Services;

public class DepartmentService : IDepartmentService
{
    private readonly AppDbContext _context;

    public DepartmentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<DepartmentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                IsActive = d.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DepartmentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                IsActive = d.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceResult<DepartmentDto>> CreateAsync(UpsertDepartmentDto dto, CancellationToken cancellationToken = default)
    {
        var name = dto.Name.Trim();
        var duplicateExists = await _context.Departments
            .AnyAsync(d => d.IsActive && d.Name.ToLower() == name.ToLower(), cancellationToken);

        if (duplicateExists && dto.IsActive)
            return ServiceResult<DepartmentDto>.Fail("Active department name already exists.", ErrorType.Conflict, "DepartmentNameExists");

        var department = new Department
        {
            Name = name,
            Description = dto.Description?.Trim(),
            IsActive = dto.IsActive
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<DepartmentDto>.Ok(await GetByIdAsync(department.Id, cancellationToken), "Department created successfully.");
    }

    public async Task<ServiceResult<DepartmentDto>> UpdateAsync(int id, UpsertDepartmentDto dto, CancellationToken cancellationToken = default)
    {
        var department = await _context.Departments.FindAsync(new object[] { id }, cancellationToken);
        if (department is null)
            return ServiceResult<DepartmentDto>.Fail("Department not found.", ErrorType.NotFound, "DepartmentNotFound");

        var name = dto.Name.Trim();
        var duplicateExists = await _context.Departments
            .AnyAsync(d => d.Id != id && d.IsActive && d.Name.ToLower() == name.ToLower(), cancellationToken);

        if (duplicateExists && dto.IsActive)
            return ServiceResult<DepartmentDto>.Fail("Active department name already exists.", ErrorType.Conflict, "DepartmentNameExists");

        department.Name = name;
        department.Description = dto.Description?.Trim();
        department.IsActive = dto.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<DepartmentDto>.Ok(await GetByIdAsync(department.Id, cancellationToken), "Department updated successfully.");
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var department = await _context.Departments.FindAsync(new object[] { id }, cancellationToken);
        if (department is null)
            return ServiceResult.Fail("Department not found.", ErrorType.NotFound, "DepartmentNotFound");

        var hasActiveRooms = await _context.ClinicRooms.AnyAsync(r => r.DepartmentId == id && r.IsActive, cancellationToken);
        if (hasActiveRooms)
            return ServiceResult.Fail("Cannot delete department because active clinic rooms are linked to it.", ErrorType.BusinessRule, "DepartmentHasRooms");

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok("Department deleted successfully.");
    }
}

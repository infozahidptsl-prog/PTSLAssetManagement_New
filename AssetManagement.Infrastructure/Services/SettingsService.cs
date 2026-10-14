using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AssetManagement.Core.Entities;
using AssetManagement.Core.Interfaces;
using AssetManagement.Infrastructure.Data;

namespace AssetManagement.Infrastructure.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly ApplicationDbContext _context;

        public SettingsService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Brands
        public async Task<IEnumerable<Brand>> GetBrandsAsync()
        {
            return await _context.Brands.ToListAsync();
        }

        public async Task<Brand> AddBrandAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty");
            var brand = new Brand { Name = name.Trim() };
            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();
            return brand;
        }

        public async Task<Brand> UpdateBrandAsync(Brand brand)
        {
            _context.Brands.Update(brand);
            await _context.SaveChangesAsync();
            return brand;
        }

        public async Task DeleteBrandAsync(string id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand != null)
            {
                _context.Brands.Remove(brand);
                await _context.SaveChangesAsync();
            }
        }

        // Categories
        public async Task<IEnumerable<DeviceCategoryModel>> GetCategoriesAsync()
        {
            var categories = await _context.DeviceCategories.ToListAsync();
            CategoryTypeRegistry.Sync(categories);
            return categories;
        }

        public async Task<DeviceCategoryModel> AddCategoryAsync(string name, string type = "System")
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty");
            var trimmedName = name.Trim();
            
            // Check if category already exists (case-insensitive)
            var existing = await _context.DeviceCategories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == trimmedName.ToLower());
                
            if (existing != null)
            {
                existing.Type = type;
                await _context.SaveChangesAsync();
                CategoryTypeRegistry.SetType(existing.Name, existing.Type);
                return existing;
            }

            var category = new DeviceCategoryModel { Name = trimmedName, Type = type };
            _context.DeviceCategories.Add(category);
            await _context.SaveChangesAsync();
            CategoryTypeRegistry.SetType(category.Name, category.Type);
            return category;
        }

        public async Task<DeviceCategoryModel> UpdateCategoryAsync(DeviceCategoryModel category)
        {
            // Fetch the old category to see if the name changed
            var oldCategory = await _context.DeviceCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == category.Id);
            
            _context.DeviceCategories.Update(category);
            await _context.SaveChangesAsync();
            
            // If the name changed, cascade the change to all devices
            if (oldCategory != null && oldCategory.Name != category.Name)
            {
                CategoryTypeRegistry.Remove(oldCategory.Name);
                var devicesToUpdate = await _context.Devices
                    .Where(d => d.Category == oldCategory.Name)
                    .ToListAsync();
                    
                foreach (var device in devicesToUpdate)
                {
                    device.Category = category.Name;
                }
                
                if (devicesToUpdate.Any())
                {
                    _context.Devices.UpdateRange(devicesToUpdate);
                    await _context.SaveChangesAsync();
                }
            }

            CategoryTypeRegistry.SetType(category.Name, category.Type);
            
            return category;
        }

        public async Task DeleteCategoryAsync(string id)
        {
            var category = await _context.DeviceCategories.FindAsync(id);
            if (category != null)
            {
                CategoryTypeRegistry.Remove(category.Name);
                _context.DeviceCategories.Remove(category);
                await _context.SaveChangesAsync();
            }
        }

        // Designations
        public async Task<IEnumerable<Designation>> GetDesignationsAsync()
        {
            return await _context.Designations.ToListAsync();
        }

        public async Task<Designation> AddDesignationAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty");
            var designation = new Designation { Name = name.Trim() };
            _context.Designations.Add(designation);
            await _context.SaveChangesAsync();
            return designation;
        }

        public async Task<Designation> UpdateDesignationAsync(Designation designation)
        {
            _context.Designations.Update(designation);
            await _context.SaveChangesAsync();
            return designation;
        }

        public async Task DeleteDesignationAsync(string id)
        {
            var designation = await _context.Designations.FindAsync(id);
            if (designation != null)
            {
                _context.Designations.Remove(designation);
                await _context.SaveChangesAsync();
            }
        }

        // Departments
        public async Task<IEnumerable<Department>> GetDepartmentsAsync()
        {
            return await _context.Departments.ToListAsync();
        }

        public async Task<Department> AddDepartmentAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty");
            var department = new Department { Name = name.Trim() };
            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
            return department;
        }

        public async Task<Department> UpdateDepartmentAsync(Department department)
        {
            _context.Departments.Update(department);
            await _context.SaveChangesAsync();
            return department;
        }

        public async Task DeleteDepartmentAsync(string id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department != null)
            {
                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
            }
        }

        // Project Managers
        public async Task<IEnumerable<ProjectManager>> GetProjectManagersAsync()
        {
            return await _context.ProjectManagers.ToListAsync();
        }

        public async Task<ProjectManager> AddProjectManagerAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty");
            var pm = new ProjectManager { Name = name.Trim() };
            _context.ProjectManagers.Add(pm);
            await _context.SaveChangesAsync();
            return pm;
        }

        public async Task<ProjectManager> UpdateProjectManagerAsync(ProjectManager projectManager)
        {
            _context.ProjectManagers.Update(projectManager);
            await _context.SaveChangesAsync();
            return projectManager;
        }

        public async Task DeleteProjectManagerAsync(string id)
        {
            if (int.TryParse(id, out int parsedId))
            {
                var pm = await _context.ProjectManagers.FindAsync(parsedId);
                if (pm != null)
                {
                    _context.ProjectManagers.Remove(pm);
                    await _context.SaveChangesAsync();
                }
            }
        }
        public async Task<IEnumerable<OwnerCompany>> GetOwnerCompaniesAsync()
        {
            return await _context.OwnerCompanies.ToListAsync();
        }

        public async Task<OwnerCompany> AddOwnerCompanyAsync(string name, string? prefix = null)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty");
            var company = new OwnerCompany { Name = name.Trim(), Prefix = prefix?.Trim() };
            _context.OwnerCompanies.Add(company);
            await _context.SaveChangesAsync();
            return company;
        }

        public async Task<OwnerCompany> UpdateOwnerCompanyAsync(OwnerCompany ownerCompany)
        {
            var existingCompany = await _context.OwnerCompanies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == ownerCompany.Id);
            if (existingCompany != null && existingCompany.Name != ownerCompany.Name)
            {
                var oldName = existingCompany.Name;
                var devices = await _context.Devices.Where(d => d.OwnerCompany == oldName).ToListAsync();
                foreach (var device in devices)
                {
                    device.OwnerCompany = ownerCompany.Name;
                }
            }

            _context.OwnerCompanies.Update(ownerCompany);
            await _context.SaveChangesAsync();
            return ownerCompany;
        }

        public async Task DeleteOwnerCompanyAsync(string id)
        {
            if (int.TryParse(id, out int parsedId))
            {
                var company = await _context.OwnerCompanies.FindAsync(parsedId);
                if (company != null)
                {
                    _context.OwnerCompanies.Remove(company);
                    await _context.SaveChangesAsync();
                }
            }
        }
        public async Task<IEnumerable<Team>> GetTeamsAsync()
        {
            return await _context.Teams.Include(t => t.ProjectManager).ToListAsync();
        }

        public async Task<Team> AddTeamAsync(string name, int? projectManagerId = null)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty");
            var team = new Team { Name = name.Trim(), ProjectManagerId = projectManagerId };
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();
            return team;
        }

        public async Task<Team> UpdateTeamAsync(Team team)
        {
            _context.Teams.Update(team);
            await _context.SaveChangesAsync();
            return team;
        }

        public async Task DeleteTeamAsync(int id)
        {
            var team = await _context.Teams.FindAsync(id);
            if (team != null)
            {
                _context.Teams.Remove(team);
                await _context.SaveChangesAsync();
            }
        }

        // Locations
        public async Task<IEnumerable<Location>> GetLocationsAsync()
        {
            return await _context.Locations.ToListAsync();
        }

        public async Task<Location> AddLocationAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty");
            var location = new Location { Name = name.Trim() };
            _context.Locations.Add(location);
            await _context.SaveChangesAsync();
            return location;
        }

        public async Task<Location> UpdateLocationAsync(Location location)
        {
            _context.Locations.Update(location);
            await _context.SaveChangesAsync();
            return location;
        }

        public async Task DeleteLocationAsync(string id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location != null)
            {
                _context.Locations.Remove(location);
                await _context.SaveChangesAsync();
            }
        }

        // Blood Groups
        public async Task<IEnumerable<BloodGroup>> GetBloodGroupsAsync()
        {
            return await _context.BloodGroups.ToListAsync();
        }

        public async Task<BloodGroup> AddBloodGroupAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty");
            var bg = new BloodGroup { Name = name.Trim() };
            _context.BloodGroups.Add(bg);
            await _context.SaveChangesAsync();
            return bg;
        }

        public async Task<BloodGroup> UpdateBloodGroupAsync(BloodGroup bloodGroup)
        {
            _context.BloodGroups.Update(bloodGroup);
            await _context.SaveChangesAsync();
            return bloodGroup;
        }

        public async Task DeleteBloodGroupAsync(string id)
        {
            var bg = await _context.BloodGroups.FindAsync(id);
            if (bg != null)
            {
                _context.BloodGroups.Remove(bg);
                await _context.SaveChangesAsync();
            }
        }
    }
}

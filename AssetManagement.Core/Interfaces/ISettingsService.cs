using System.Collections.Generic;
using System.Threading.Tasks;
using AssetManagement.Core.Entities;

namespace AssetManagement.Core.Interfaces
{
    public interface ISettingsService
    {
        Task<IEnumerable<Brand>> GetBrandsAsync();
        Task<Brand> AddBrandAsync(string name);
        Task<Brand> UpdateBrandAsync(Brand brand);
        Task DeleteBrandAsync(string id);

        Task<IEnumerable<DeviceCategoryModel>> GetCategoriesAsync();
        Task<DeviceCategoryModel> AddCategoryAsync(string name, string type = "System");
        Task<DeviceCategoryModel> UpdateCategoryAsync(DeviceCategoryModel category);
        Task DeleteCategoryAsync(string id);

        Task<IEnumerable<Designation>> GetDesignationsAsync();
        Task<Designation> AddDesignationAsync(string name);
        Task<Designation> UpdateDesignationAsync(Designation designation);
        Task DeleteDesignationAsync(string id);

        Task<IEnumerable<Department>> GetDepartmentsAsync();
        Task<Department> AddDepartmentAsync(string name);
        Task<Department> UpdateDepartmentAsync(Department department);
        Task DeleteDepartmentAsync(string id);

        Task<IEnumerable<ProjectManager>> GetProjectManagersAsync();
        Task<ProjectManager> AddProjectManagerAsync(string name);
        Task<ProjectManager> UpdateProjectManagerAsync(ProjectManager projectManager);
        Task DeleteProjectManagerAsync(string id);

        Task<IEnumerable<OwnerCompany>> GetOwnerCompaniesAsync();
        Task<OwnerCompany> AddOwnerCompanyAsync(string name, string? prefix = null);
        Task<OwnerCompany> UpdateOwnerCompanyAsync(OwnerCompany ownerCompany);
        Task DeleteOwnerCompanyAsync(string id);

        Task<IEnumerable<Team>> GetTeamsAsync();
        Task<Team> AddTeamAsync(string name, int? projectManagerId = null);
        Task<Team> UpdateTeamAsync(Team team);
        Task DeleteTeamAsync(int id);

        Task<IEnumerable<Location>> GetLocationsAsync();
        Task<Location> AddLocationAsync(string name);
        Task<Location> UpdateLocationAsync(Location location);
        Task DeleteLocationAsync(string id);

        Task<IEnumerable<BloodGroup>> GetBloodGroupsAsync();
        Task<BloodGroup> AddBloodGroupAsync(string name);
        Task<BloodGroup> UpdateBloodGroupAsync(BloodGroup bloodGroup);
        Task DeleteBloodGroupAsync(string id);
    }
}

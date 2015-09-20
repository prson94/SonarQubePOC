using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using d360.model;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using d360.core.entities;
using System.Data;

namespace d360.model.interfaces
{
    public interface ISecurityRepository : IRepository<Resource, int>
    {
        Group FindSingleGroup(int id);

        List<Group> FindAllGroups();
        List<Resource> FindAllByGroup(int groupID);

        Role FindSingleRole(int id);

        Resource FindSingle(int id);

        IQueryable<Resource> FindAllResources();

        IQueryable<Role> FindAllRoles();
        IQueryable<Resource> FindAllByRole(int roleID);

        void Delete(Resource resource);
        void Insert(Resource resource);
        void Update(Resource resource);
        void DeleteGroup(Group group);
        void InsertGroup(Group group);
        void UpdateGroup(Group group);
        void DeleteRole(Role role);
        void InsertRole(Role role);
        void UpdateRole(Role role);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using d360.model;
using System.Xml.Linq;
using System.Diagnostics;
using d360.core.entities;
using System.Data;
using d360.core.enums;

namespace d360.model.interfaces
{
    public interface IIntersectRepository : IRepository<Intersect, long>
    {
        long AddIntersect(int intersectTypeID, long sourceID, SystemObjects sourceType, long targetID, SystemObjects targetType, int? constrainingID, SystemObjects? constrainingType);
        IQueryable<Intersect> FindAllByType(int typeID);
        List<AllIntersectPoint> GetAllIntersections(int sourceID, string sourceType, int filterID, string filterType);
        List<IntersectionPoint> GetIntersections(int sourceID, int targetTypeID, string sourceType, string targetType);
        List<NonIntersectionPoint> GetNonIntersections(int sourceID, int targetTypeID, string sourceType, string targetType, int intersectTypeID);
    }
    public interface IIntersectTypeRepository : IRepository<IntersectType, int>
    {
        List<AllowedIntersectionType> GetActiveIntersectionTypes(SystemObjects source, int sourceID);
        List<AllowedIntersectionType> GetAllowedIntersectionTypes(int sourceTypeID, string sourceType);
        List<IntersectTypeOption> GetIntersectTypeOptions();
        void ValidateIntersectType(int id, XElement value);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using d360.core.entities;
using d360.core.enums;

namespace d360.model.interfaces
{
    public interface IQualityExceptionRepository : IRepository<QualityException, long>
    {
        IQueryable<QualityExceptionLog> GetLogsByID(long id);
        IQueryable<QualityException> GetByObjectType(SystemObjects objectType, int objectID, int objectTypeID);
    }

    public interface IQualityExceptionDefinitionRepository : IRepository<QualityExceptionDefinition, long>
    {
    }

    public interface IQualityExceptionLogRepository : IRepository<QualityExceptionLog, long>
    {
    }

    public interface IQualityExceptionTypeRepository : IRepository<QualityExceptionType, Guid>
    {
    }

    public interface IQualityRuleRepository : IRepository<QualityRule, int>
    {
        int GetRelatedItemCount(int id);
    }

    public interface IQualityRuleTypeRepository : IRepository<QualityRuleType, int>
    {
    }

    public interface IResolutionRepository : IRepository<Resolution, int>
    {
    }
}

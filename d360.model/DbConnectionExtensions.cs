using d360.core.entities;
using Dapper;
using System.Collections.Generic;
using System.Data.Common;

namespace d360.model
{
    public static class DbConnectionExtensionscs
    {
        public static IEnumerable<ObjectResult> GetWhenResults(this DbConnection cnn, ResponsibilityTypeRelationRule rule)
        {
            string whenSql = "";

            #region WhenSql

            whenSql = $@"
select	A.ID as AssetID, utility.GetAssetDisplayValueWrapper(A.ID) as Name 
from	Asset A 
		inner join AssetType T on T.ID = A.AssetTypeID and T.Object = '{rule.Object}' and T.ObjectID = {rule.ObjectID} ";
            var fCount = 1;
            var rCount = 1;
            if (rule.StructuredDefinition.When != null)
            {
                rule.StructuredDefinition.When.ForEach(w => {
                    if (w.CheckType == "F")
                    {
                        if (w.FieldTypeID > 0)
                        {
                            whenSql += $@"inner join Field F{fCount} on F{fCount}.ObjectType = A.Object and F{fCount}.ObjectID = A.ObjectID and F{fCount}.FieldTypeID = {w.FieldTypeID} and F{fCount}.Value = '{w.Value}' ";
                        }
                        else
                        {
                            //something else here, static field
                        }
                        fCount++;
                    }
                    if (w.CheckType == "R")
                    {
                        whenSql += $@"inner join [Intersect] I{rCount} on 
        I{rCount}.IntersectTypeID = {w.IntersectTypeID} and 
        ( 
        (I{rCount}.Subject = A.Object and I{rCount}.SubjectID = A.ObjectID and I{rCount}.Object = '{w.TargetObject}' and I{rCount}.ObjectID = {w.TargetObjectID}) OR 
        (I{rCount}.Object = A.Object and I{rCount}.ObjectID = A.ObjectID and I{rCount}.Subject = '{w.TargetObject}' and I{rCount}.SubjectID = {w.TargetObjectID}) 
        ) ";
                        rCount++;
                    }
                });
            }

            return cnn.Query<ObjectResult>(whenSql);

            #endregion
        }

        public static IEnumerable<SecurityResult> GetThenResults(this DbConnection cnn, ResponsibilityTypeRelationRule rule)
        {
            string thenSql = "";

            int tCount = 1;
            string whenSuffix = "";

            if (rule.StructuredDefinition.Then.Object == "OrganizationType")
            {
                thenSql = $@"
select	'O' as SecurityAsset,
        O.ID as SecurityAssetID,
		O.Name
from	Organization O ";

                rule.StructuredDefinition.Then.Conditions.ForEach(rc =>
                {
                    if (rc.FieldTypeID > 0)
                    {
                        thenSql += $"inner join Field F{tCount} on F{tCount}.ObjectType = 'Organization' and F{tCount}.ObjectID = O.ID and F{tCount}.FieldTypeID = {rc.FieldTypeID} and F{tCount}.Value = '{rc.Value}' ";
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(rc.FieldTypeName))
                        {
                            if (rc.FieldTypeName == "Name")
                            {
                                whenSuffix += (string.IsNullOrEmpty(whenSuffix) ? $" where " : " and ") + $"O.ID = {rc.Value}";
                            }
                            else
                            {
                                whenSuffix += (string.IsNullOrEmpty(whenSuffix) ? $" where " : " and ") + $"O.{rc.FieldTypeName} = '{rc.Value}'";
                            }
                        }
                    }

                    tCount++;
                });
            }

            if (rule.StructuredDefinition.Then.Object == "GroupType")
            {
                thenSql = $@"
select	'G' as SecurityAsset,
        O.ID as SecurityAssetID,
        O.Name as GroupName
from	[Group] O ";
//		inner join ResourceGroup RG on RG.GroupID = G.ID 
//        inner join reporting.Global_Resource R on R.ResourceID = RG.ResourceID";

                if (rule.StructuredDefinition.Then.Conditions != null)
                {
                    rule.StructuredDefinition.Then.Conditions.ForEach(rc =>
                    {
                        if (rc.FieldTypeID > 0)
                        {
                            thenSql += $"inner join Field F{tCount} on F{tCount}.ObjectType = 'Group' and F{tCount}.ObjectID = O.ID and F{tCount}.FieldTypeID = {rc.FieldTypeID} and F{tCount}.Value = '{rc.Value}' ";
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(rc.FieldTypeName))
                            {
                                if (rc.FieldTypeName == "Name")
                                {
                                    whenSuffix += (string.IsNullOrEmpty(whenSuffix) ? $" where " : " and ") + $"G.ID = {rc.Value}";
                                }
                                else
                                {
                                    whenSuffix += (string.IsNullOrEmpty(whenSuffix) ? $" where " : " and ") + $"G.{rc.FieldTypeName} = '{rc.Value}'";
                                }
                            }
                        }

                        tCount++;
                    });
                }
            }

            if (rule.StructuredDefinition.Then.Object == "ResourceType")
            {
                thenSql = $@"
select	'R' as SecurityAsset,
        O.ResourceID as SecurityAssetID,
		O.FirstName + ' ' + O.LastName as Name
from	reporting.Global_Resource O ";

                rule.StructuredDefinition.Then.Conditions.ForEach(rc =>
                {
                    if (rc.FieldTypeID > 0)
                    {
                        thenSql += $"inner join Field F{tCount} on F{tCount}.ObjectType = 'Resource' and F{tCount}.ObjectID = O.ResourceID and F{tCount}.FieldTypeID = {rc.FieldTypeID} and F{tCount}.Value = '{rc.Value}' ";
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(rc.FieldTypeName))
                        {
                            if (rc.FieldTypeName == "Name")
                            {
                                whenSuffix += (string.IsNullOrEmpty(whenSuffix) ? $" where " : " and ") + $"O.ResourceID = {rc.Value}";
                            }
                            else
                            {
                                whenSuffix += (string.IsNullOrEmpty(whenSuffix) ? $" where " : " and ") + $"O.{rc.FieldTypeName} = '{rc.Value}'";
                            }
                        }
                    }

                    tCount++;
                });
            }

            thenSql += whenSuffix;

            return cnn.Query<SecurityResult>(thenSql);
        }
    }
}

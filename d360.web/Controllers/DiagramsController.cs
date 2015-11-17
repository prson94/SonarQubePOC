using d360.core;
using System.Linq;
using System.Web.Mvc;
using d360.web.Models;
using d360.core.entities;
using d360.model;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Linq;

namespace d360.web.Controllers
{
    [RoutePrefix("diagrams"), Authorize]
    public class DiagramsController : BaseController
    {
        #region DI

        public DiagramsController(CommunityContext community, CompanyContext company) : base(community, company) { }

        #endregion

        #region Diagram Tooltips

        public ContentResult DiagramRelationshipsTooltip(string type, int id)
        {
            var html = Company.Query<string>(@"
declare @html nvarchar(max)

set @html = '<div style=""max-height: 300px; overflow-y: scroll""><table class=""table-striped table-condensed"" style=""width:100%"">'
set @html = @html + '<thead><th>Type</th><th>Name</th><th>Classification</th><th>Has Technical Relationships?</th></thead>'
set @html = @html + '<tbody>'

select		@html =	@html + 
					'<tr>' +
					'<td>' + TargetTypeName + '</td>' +
					'<td><a href=""' + TargetUrl + '"" data-context=""Preview"" data-type=""' + TargetObjectType + '"" data-id=""' + cast(TargetObjectID as varchar(15)) + '"">' + TargetName + '</a></td>' +
					'<td>' + case Classification when 1 then 'Critical' else  'Normal' end + '</td>' +
					'<td>' + case HasTechnicalRelationships when 1 then 'Yes' else 'No' end + '</td>' +
					'</tr>'
from		Relationship 
where		SourceObjectType = @type 
			and SourceObjectID = @id
order by	TargetTypeName,
			TargetName
set @html = @html + '</tbody>'
set @html = @html + '</table></div>'

select @html", new { type = type, id = id }).Single();

            return Content(html);
        }

        #endregion

        #region Diagram Data

        #region Information Catalog Diagram

        [DataContract]
        public class InformationCatalogDiagramDataItem
        {
            [DataMember(Name = "id")]
            public int ID { get; set; }
            [IgnoreDataMember]
            public int? ParentID { get; set; }
            [DataMember(Name = "name")]
            public string Name { get; set; }
            [DataMember(Name = "url")]
            public string Url { get; set; }
            [DataMember]
            public bool RelationshipsExist { get; set; }
            [DataMember(Name = "children")]
            public List<InformationCatalogDiagramDataItem> Children { get; set; }
        }

        public JsonNetResult InformationCatalogDiagramData(int id)
        {
            var query = Company.Query<InformationCatalogDiagramDataItem>(
@"with h as (
select		top 100 percent	
			T.ID,
			0 as ParentID,
			T.Name,
            dbo.GenerateObjectUrl('Taxonomy', T.TaxonomyTypeID, T.ID) as Url
from		Taxonomy T
where	    T.TaxonomyTypeID = @id
			and T.ParentID is null
order by	Name
union all
select		top 100 percent	
			C.ID,
			C.ParentID,
			C.Name,
            dbo.GenerateObjectUrl('Taxonomy', C.TaxonomyTypeID, C.ID) as Url
from		Taxonomy C
			inner join h on h.ID = C.ParentID
order by	C.Name
)
select	0 as ID, 
		null as ParentID,
		Name,
        dbo.GenerateObjectUrl('TaxonomyType', ID, ID) as Url,
        cast(0 as bit) as RelationshipsExist
from	TaxonomyType
where	ID = @ID
union
select	ID, 
		ParentID, 
		Name,
        Url,
        cast(R.RelationshipsExist as bit) as RelationshipsExist
from	h
        cross apply (
                    select  case 
                                when count(1) > 0 then 1
                                else 0
                            end as RelationshipsExist
                    from    IntersectNode N 
                    where   ObjectType = 'Taxonomy' and ObjectID = h.ID
                    ) R
", new { id = id }).ToList();
            var rootModel = query.Single(i => i.ID == 0);
            rootModel.Children = loadInformationCatalogDiagramData(rootModel, query);
            return new JsonNetResult {
                Data = rootModel,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        List<InformationCatalogDiagramDataItem> loadInformationCatalogDiagramData(InformationCatalogDiagramDataItem model, List<InformationCatalogDiagramDataItem> rawItems)
        {
            if (rawItems.Any(i => (model != null) ? i.ParentID == model.ID : !i.ParentID.HasValue))
            {
                var list = new List<InformationCatalogDiagramDataItem>();
                foreach (var c in rawItems.Where(i => (model != null) ? i.ParentID == model.ID : !i.ParentID.HasValue).OrderBy(i => i.Name))
                {
                    c.Children = loadInformationCatalogDiagramData(c, rawItems);
                    list.Add(c);
                }
                return list;
            }
            else
            {
                return null;
            }
        }

        #endregion


        #region Lineage/Environment Details Diagram

        [DataContract]
        public class LineageDiagramDataContext
        {
            [DataMember]
            public string Code { get; set; }

            [DataMember]
            public string Name { get; set; }

            [DataMember]
            public string Lookup { get; set; }
        }

        [DataContract]
        public class LineageDiagramDataTechnicalRelationship
        {
            [DataMember]
            public string Type { get; set; }

            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public string Attribute { get; set; }

            [DataMember]
            public string Fusion { get; set; }

            [DataMember]
            public string Name { get; set; }

            [DataMember] //Not in query yet
            public string Url { get; set; }
        }

        [DataContract]
        public class LineageDiagramDataTransformation
        {
            [DataMember]
            public string Type { get; set; }

            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public string Description { get; set; }
        }

        [DataContract]
        public class BaseDiagramItem
        {
            //public int IntersectID { get; set; }

            [DataMember]
            public int ID { get; set; }

            public int? ParentID { get; set; }

            [DataMember]
            public string ObjectType { get; set; }

            [DataMember]
            public int ObjectID { get; set; }

            [DataMember]
            public string Name { get; set; }

            [DataMember]
            public string Type { get; set; }

            [DataMember]
            public string BackColor { get; set; }

            [DataMember]
            public string ForeColor { get; set; }

            [DataMember]
            public string Role { get; set; }

            [DataMember]
            public string Url { get; set; }

            public string TechnicalRelationships { get; set; }
            public string Contexts { get; set; }
            public string Transformations { get; set; }

            [DataMember(Name = "Contexts")]
            public List<LineageDiagramDataContext> ContextItems { get; set; }

            [DataMember(Name = "Relationships")]
            public List<LineageDiagramDataTechnicalRelationship> Relationships { get; set; }

            [DataMember(Name = "Transformations")]
            public List<LineageDiagramDataTransformation> TransformationItems { get; set; }
        }

        [DataContract]
        public class LineageDiagramItem : BaseDiagramItem
        {
            [DataMember]
            public List<LineageDiagramItem> children { get; set; }
        }

        [DataContract]
        public class EnvironmentDetailDiagramItem : BaseDiagramItem
        {
            [DataMember]
            public string AssigningItemType { get; set; }

            [DataMember]
            public int AssigningItemID { get; set; }

            [DataMember]
            public List<EnvironmentDetailDiagramItem> children { get; set; }
        }


        List<EnvironmentDetailDiagramItem> loadEnvironmentDetailDiagramChildren(List<EnvironmentDetailDiagramItem> items, EnvironmentDetailDiagramItem parent)
        {
            List<EnvironmentDetailDiagramItem> children = null;

            if (items.Any(i => i.ParentID == parent.ID))
            {
                children = new List<EnvironmentDetailDiagramItem>();
                foreach (var i in items.Where(i => i.ParentID == parent.ID).OrderBy(i => i.Name))
                {
                    loadLineageDiagramItem(i);

                    i.children = loadEnvironmentDetailDiagramChildren(items, i);

                    children.Add(i);
                }
            }

            return children;
        }

        List<LineageDiagramItem> loadLineageDiagramChildren(List<LineageDiagramItem> items, LineageDiagramItem parent)
        {
            List<LineageDiagramItem> children = null;

            if (items.Any(i => i.ParentID == parent.ID))
            {
                children = new List<LineageDiagramItem>();
                foreach (var i in items.Where(i => i.ParentID == parent.ID).OrderBy(i => i.Name))
                {
                    loadLineageDiagramItem(i);

                    i.children = loadLineageDiagramChildren(items, i);

                    children.Add(i);
                }
            }

            return children;
        }

        void loadLineageDiagramItem(BaseDiagramItem i)
        {
            XElement xml = null;

            if (!string.IsNullOrEmpty(i.Contexts))
            {
                xml = XElement.Parse(i.Contexts);
                i.ContextItems = xml.Elements("context")
                    .Select(e => new LineageDiagramDataContext 
                    { 
                        Code = e.Attribute("code").Value,
                        Lookup = e.Attribute("lookup").Value,
                        Name = e.Attribute("name").Value 
                    }).ToList();

            }

            if (!string.IsNullOrEmpty(i.TechnicalRelationships))
            {
                xml = XElement.Parse(i.TechnicalRelationships);
                i.Relationships = xml.Elements("relationship")
                    .Select(e => new LineageDiagramDataTechnicalRelationship
                    {
                        Attribute = e.Attribute("attribute").Value,
                        Fusion = e.Attribute("fusion").Value,
                        ID = int.Parse(e.Attribute("id").Value),
                        Name = e.Attribute("name").Value,
                        Type = e.Attribute("type").Value//,
                        //Url = e.Attribute("url").Value
                    }).ToList();

            }

            if (!string.IsNullOrEmpty(i.Transformations))
            {
                xml = XElement.Parse(i.Transformations);
                i.TransformationItems = xml.Elements("transformation")
                    .Select(e => new LineageDiagramDataTransformation
                    {
                        Description = e.Element("description").Value,
                        ID = int.Parse(e.Attribute("id").Value),
                        Type = e.Attribute("type").Value//,
                        //Url = e.Attribute("url").Value
                    }).ToList();
            }
        }

        /// <summary>
        /// Gets the actual sources for the given relationship.
        /// </summary>
        /// <param name="id">The target IntersectID</param>
        /// <returns>JSON Data</returns>
        public JsonNetResult LineageDiagramData(int id)
        {
            var items = Company.Query<LineageDiagramItem>(
                    "EXEC GetLineageDiagramData @IntersectID",
                    new { IntersectID = id }
                ).ToList();

            LineageDiagramItem root = null;

            if (items != null)
            {
                if (items.Count > 0)
                {
                    root = items.SingleOrDefault(i => !i.ParentID.HasValue);
                    loadLineageDiagramItem(root);
                    root.children = loadLineageDiagramChildren(items, root);                
                }
            }

            if (root == null)
            {
                root = new LineageDiagramItem { Name = "No data", ID = 0 };
            }

            return new JsonNetResult { Data = root, Formatting = Newtonsoft.Json.Formatting.None };
        }

        /// <summary>
        /// Gets the ideal sources for the given relationship.
        /// </summary>
        /// <param name="id">The target IntersectID</param>
        /// <returns>JSON Data</returns>
        public JsonNetResult EnvironmentDetailsDiagramData(SystemObjects type, int id)
        {
            var items = Company.Query<EnvironmentDetailDiagramItem>(
                    "EXEC GetEnvironmentDetailsDiagramData @ObjectType, @ObjectID",
                    new { ObjectType = type.ToString(), ObjectID = id }
                ).ToList();

            var root = items.SingleOrDefault(i => !i.ParentID.HasValue);

            root.children = loadEnvironmentDetailDiagramChildren(items, root);

            return new JsonNetResult { Data = root, Formatting = Newtonsoft.Json.Formatting.None };
        }

        #endregion

        #endregion
    }
}

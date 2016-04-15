using System.Web.Mvc;
using d360.core.entities;
using d360.model;

namespace d360.web.Controllers
{
    [HandleError(View = "Error")]
    public class HomeController : BaseController
    {
        #region DI

        public HomeController(CommunityContext community, CompanyContext company)
            : base(community, company) 
        { }

        #endregion

        [Authorize]
        public ActionResult Index()
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            ViewData.Add("ResourceID", Company.CurrentResourceID);
            ViewData.Add("Settings", Community.GetCompanySettings());
            return View("SPA");
        }

        public ActionResult Main()
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            ViewData.Add("ResourceID", Company.CurrentResourceID);
            ViewData.Add("Settings", Community.GetCompanySettings());
            return View();
        }

        public ActionResult Ember()
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            ViewData.Add("ResourceID", Company.CurrentResourceID);
            ViewData.Add("Settings", Community.GetCompanySettings());
            return View("Core");
        }


        #region Overlays

        public ActionResult ArtifactActivityOverlay(string mode, int artifactTypeID, int lookBackDays)
        {
            ViewData.Add("LookBackDays", lookBackDays);

            var type = Company.GetById<ArtifactType>(artifactTypeID);
            
            ViewData.Add("ArtifactType",type);            
            return PartialView();
        }

        public ActionResult AssignmentActivityOverlay(string mode, int type, int lookBackDays, int resourceID = -1)
        {
            ViewData.Add("LookBackDays", lookBackDays);
            ViewData.Add("WorkflowTypeID", type);
            ViewData.Add("ResourceID", resourceID == Company.CurrentResourceID ? -1 : resourceID);

            switch ((workflow.WorkflowType)type)
            {
                case workflow.WorkflowType.SuggestNewArtifact:
                    ViewData.Add("WorkflowName", Resources.Core.WorkflowType_SuggestNewArtifact);
                    break;
                case workflow.WorkflowType.CertifyArtifact:
                    ViewData.Add("WorkflowName", Resources.Core.WorkflowType_CertifyArtifact);
                    break;
                case workflow.WorkflowType.WorkIssue:
                    ViewData.Add("WorkflowName", Resources.Core.WorkflowType_WorkIssue);
                    break;
                case workflow.WorkflowType.ChallengeArtifact:
                    ViewData.Add("WorkflowName", Resources.Core.WorkflowType_ChallengeArtifact);
                    break;
                default:
                    ViewData.Add("WorkflowName", "Unknown");
                    break;
            }
                        
            return PartialView();
        }

        public ActionResult SocialActivityOverlay(int type, int lookBackDays)
        {
            ViewData.Add("LookBackDays", lookBackDays);
            ViewData.Add("Category", type);

            var typeName = string.Empty;

            switch ((core.enums.CommentType)type)
            {                
                case core.enums.CommentType.Social:
                    typeName = Resources.Core.CommentType_Social;
                    break;                
                case core.enums.CommentType.Issue:
                    typeName = Resources.Core.CommentType_Issue;
                    break;
                case core.enums.CommentType.Task:
                    typeName = Resources.Core.CommentType_Task;
                    break;                
                case core.enums.CommentType.DataEvent:
                    typeName = Resources.Core.CommentType_DataEvent;
                    break;
                case core.enums.CommentType.Challenge:
                    typeName = Resources.Core.CommentType_Challenge;
                    break;
                    //case core.enums.CommentType.Question:
                    //    typeName = Resources.Core.CommentType_Question;
                    //    break;             
            }

            ViewData.Add("CategoryName", typeName);

            return PartialView();
        }

        #endregion
    }
}

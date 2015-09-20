using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace d360.tests.ui.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        class PieChart
        {
            public string label { get; set; }
            public string x { get; set; }
            public double y { get; set; }
        }

        public JsonResult PieReport()
        {
            /*
             State,Under 5 Years,5 to 13 Years,14 to 17 Years,18 to 24 Years,25 to 44 Years,45 to 64 Years,65 Years and Over
            AL,310504,552339,259034,450818,1231572,1215966,641667
            AK,52083,85640,42153,74257,198724,183159,50277
            AZ,515910,828669,362642,601943,1804762,1523681,862573
            AR,202070,343207,157204,264160,754420,727124,407205
             */
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        class ChartItem
        {
            public string label { get; set; }
            public string x { get; set; }
            public double y { get; set; }
        }

        public JsonResult LineReport()
        {
            var list = new List<ChartItem>();
            list.Add(new ChartItem { label = "New York", x = "20131001", y = 63.4 });
            list.Add(new ChartItem { label = "San Francisco", x = "20131001", y = 62.7 });
            list.Add(new ChartItem { label = "Austin", x = "20131001", y = 72.2 });

            list.Add(new ChartItem { label = "New York", x = "20131002", y = 58.0 });
            list.Add(new ChartItem { label = "San Francisco", x = "20131002", y = 59.9 });
            list.Add(new ChartItem { label = "Austin", x = "20131002", y = 67.7 });

            list.Add(new ChartItem { label = "New York", x = "20131003", y = 53.3 });
            list.Add(new ChartItem { label = "San Francisco", x = "20131003", y = 59.1 });
            list.Add(new ChartItem { label = "Austin", x = "20131003", y = 69.4 });

            list.Add(new ChartItem { label = "New York", x = "20131004", y = 55.7 });
            list.Add(new ChartItem { label = "San Francisco", x = "20131004", y = 58.8 });
            list.Add(new ChartItem { label = "Austin", x = "20131004", y = 68.0 });

            list.Add(new ChartItem { label = "New York", x = "20131005", y = 64.2 });
            list.Add(new ChartItem { label = "San Francisco", x = "20131005", y = 58.7 });
            list.Add(new ChartItem { label = "Austin", x = "20131005", y = 72.4 });

            list.Add(new ChartItem { label = "New York", x = "20131006", y = 58.8 });
            list.Add(new ChartItem { label = "San Francisco", x = "20131006", y = 57.0 });
            list.Add(new ChartItem { label = "Austin", x = "20131006", y = 77.0 });

            list.Add(new ChartItem { label = "New York", x = "20131007", y = 57.9 });
            list.Add(new ChartItem { label = "San Francisco", x = "20131007", y = 56.7 });
            list.Add(new ChartItem { label = "Austin", x = "20131007", y = 82.3 });

            list.Add(new ChartItem { label = "New York", x = "20131008", y = 61.8 });
            list.Add(new ChartItem { label = "San Francisco", x = "20131008", y = 56.8 });
            list.Add(new ChartItem { label = "Austin", x = "20131008", y = 78.9 });

            list.Add(new ChartItem { label = "New York", x = "20131009", y = 69.3 });
            list.Add(new ChartItem { label = "San Francisco", x = "20131009", y = 56.7 });
            list.Add(new ChartItem { label = "Austin", x = "20131009", y = 68.8 });

            list.Add(new ChartItem { label = "New York", x = "20131010", y = 71.2 });
            list.Add(new ChartItem { label = "San Francisco", x = "20131010", y = 60.1 });
            list.Add(new ChartItem { label = "Austin", x = "20131010", y = 68.7 });

            return Json(list, JsonRequestBehavior.AllowGet);
        }
	}
}
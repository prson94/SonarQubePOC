using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace d360.web.Models
{

    public class NodeDataArray
    {
        public string key { get; set; }
        public string icon { get; set; }
        public string category { get; set; }
        public string loc { get; set; }
        public string refItemColor { get; set; }
        public string name { get; set; }
        public string assetTypeName { get; set; }
        public string assetTypeUid { get; set; }
    }

    public class LinkDataArray
    {
        public string from { get; set; }
        public string to { get; set; }
        public IList<double> points { get; set; }
    }

    public class ProcessDiagramModel
    {
        public string @class { get; set; }
        public IList<NodeDataArray> nodeDataArray { get; set; }
        public IList<LinkDataArray> linkDataArray { get; set; }
    }

}
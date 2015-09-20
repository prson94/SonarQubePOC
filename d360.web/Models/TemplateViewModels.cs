using System.Collections.Generic;
using d360.core.entities;
using d360.core;
namespace d360.web.Models
{
    public class TemplateEditModel
    {
        public List<string> Names { get; set; }

        public TemplateEditModel()
        {
            Names = new List<string>();
            foreach (var name in System.Enum.GetNames(typeof(SystemObjects)))
            {
                Names.Add(name);
            }
            Names.Add("Global.Footer");
            Names.Add("Global.Header");
        }
    }

    public class EmailTemplateEditModel : TemplateEditModel
    {
        public EmailTemplate Template { get; set; }
    }

    public class TooltipTemplateEditModel : TemplateEditModel
    {
        public TooltipTemplate Template { get; set; }
    }
}
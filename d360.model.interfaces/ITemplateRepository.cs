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
using d360.core.enums;
using d360.core.entities.Views;

namespace d360.model.interfaces
{
    public interface ITooltipTemplateRepository : IRepository<TooltipTemplate, int>
    {
        string Render(string action, string objectType, int objectID);
    }

    public interface IEmailTemplateRepository : IRepository<EmailTemplate, int>
    {
        string Render(string action, string objectType, int objectID);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace d360.extensions
{
    public interface IWorkflowActivity
    {
        int ID { get; }

        string Name { get; }

        //XElement Fields { get; set; }

        XElement Settings { get; set; }

        void Execute(string settings, bool isTest = false);
    }
}

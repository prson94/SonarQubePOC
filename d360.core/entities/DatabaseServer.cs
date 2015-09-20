using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using d360.core.entities;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Serialization;
using System.Web.Script.Serialization;
using System.ComponentModel;
using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class DatabaseServer : BaseIntObject, IIntObject
    {
        public string Server { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }
    }
}

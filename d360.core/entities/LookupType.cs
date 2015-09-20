using System.Xml.Linq;
using d360.core.entities.Contracts;
using System;
using System.Data;
using System.Xml.Schema;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.LookupType, "LookupType")]
    public class LookupType : BaseIntObject, IIntObject, ISearchable, IUpdatedMetadata
    {
        [DataMember(Name = "name")]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Name_ErrorRequired")]
        [StringLength(250)]
        public string Name { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        [XmlIgnore()]
        [ForeignKey("LookupTypeID")]
        public virtual ICollection<Lookup> Lookups { get; set; }

        public DataTable GetLookupValuesDataTable()
        {
            /*
#region Build columns in data table.

byte[] bytes = Encoding.UTF8.GetBytes(Schema.ToString());
MemoryStream stream = new MemoryStream(bytes);

XmlSchema s = XmlSchema.Read(stream, new ValidationEventHandler(validationCallback));

var root = s.Items[0] as XmlSchemaElement;

var fields = ((root.SchemaType as XmlSchemaComplexType).Particle as XmlSchemaGroupBase).Items;

var columnList = new List<SchemaColumn>();

foreach (XmlSchemaElement el in fields)//s.Items)
{
    columnList.Add(
                    new SchemaColumn
                    {
                        Name = el.Name,
                        Caption = el.Name,
                        Description = "",
                        Type = "string"
                    }
                  );
}
*/
            DataTable dt = new DataTable("Lookups");

            /*
            DataColumn c;
            //lookup.LookupValues.First().ID

            c = new DataColumn("ID", typeof(int));
            c.Caption = "ID";
            dt.Columns.Add(c);

            foreach (var column in columnList)
            {
                c = new DataColumn();
                c.Caption = column.Caption;
                c.ColumnName = column.Name;

                //switch (column.Type)
                //{
                //    case "xsd:dateTime":
                //        c.DataType = typeof(DateTime);
                //        break;
                //    case "xsd:int":
                //        c.DataType = typeof(int);
                //        break;
                //    default:
                c.DataType = typeof(string);
                //        break;
                //}

                dt.Columns.Add(c);
            }

            #endregion

            #region Build Data Table with values to bind to log grid.

            foreach (var v in Lookups)
            {
                XElement xml = XElement.Parse(v.Value);
                DataRow row = dt.NewRow();
                row["ID"] = v.ID;

                foreach (var column in columnList)
                {
                    row[column.Name] = (xml.Element(column.Name) != null) ? xml.Element(column.Name).Value : "";
                }

                dt.Rows.Add(row);
            }

            #endregion

            #region Rename column names to what the column caption is, to support friendly naming

            foreach (DataColumn dc in dt.Columns)
            {
                dc.ColumnName = dc.Caption;
            }

            #endregion
            */
            return dt;
        }
    }
}

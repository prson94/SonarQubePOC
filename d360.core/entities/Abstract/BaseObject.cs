using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [Serializable, DataContract(Namespace = NAMESPACE)]
    public abstract class BaseObject
    {
        internal const string NAMESPACE = constants.NAMESPACE;

        public string GetObjectType()
        {
            ObjectTypeAttribute attr;

            try
            {
                attr = (ObjectTypeAttribute)System.Attribute.GetCustomAttribute(this.GetType(), typeof(ObjectTypeAttribute));
                return attr.ObjectType;
            }
            catch (Exception)
            {
                return string.Empty;
            }
            finally 
            {
                attr = null;
            }
        }
    }

    [Serializable, DataContract(Namespace = NAMESPACE)]
    public abstract class BaseGuidObject : BaseObject
    {
        [
        DataMember,
        Key,
        DatabaseGenerated(DatabaseGeneratedOption.Identity),
        Display(ResourceType = typeof(d360.core.resources.Fields), Name = "ID_Name", Description = "ID_Description")
        ]
        public Guid ID { get; set; }
    }


    [Serializable, DataContract(Namespace = NAMESPACE)]
    public abstract class BaseIntObject : BaseObject
    {
        [
        DataMember,
        Key,
        DatabaseGenerated(DatabaseGeneratedOption.Identity),
        Display(ResourceType = typeof(d360.core.resources.Fields), Name = "ID_Name", Description = "ID_Description")
        ]
        public int ID { get; set; }
    }
}

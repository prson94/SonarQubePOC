using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [Serializable, DataContract(Namespace = NAMESPACE)]
    public abstract class BaseObject
    {
        internal const string NAMESPACE = constants.NAMESPACE;
    }

    [Serializable, DataContract(Namespace = NAMESPACE)]
    public abstract class BaseUidObject : BaseObject
    {
        [
        DataMember,
        Key,
        DatabaseGenerated(DatabaseGeneratedOption.Identity)
        ]
        public Guid Uid { get; set; }
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
    public abstract class BaseCreatedAndUpdatedGuidObject : BaseUidObject, IUpdatedMetadata, ICreatedMetadata
    {
        public int? CreatedBy { get; set; } = 0;

        [DataMember]
        public DateTime? CreatedOn { get; set; } = DateTime.UtcNow;

        public int? UpdatedBy { get; set; } = 0;

        [DataMember]
        public DateTime? UpdatedOn { get; set; } = DateTime.UtcNow;
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

    [Serializable, DataContract(Namespace = NAMESPACE)]
    public abstract class BaseCreatedAndUpdatedUidObject : BaseUidObject, IUpdatedMetadata, ICreatedMetadata
    {
        [DataMember]
        public int? CreatedBy { get; set; } = 0;

        [DataMember]
        public DateTime? CreatedOn { get; set; } = DateTime.UtcNow;

        [DataMember]
        public int? UpdatedBy { get; set; } = 0;

        [DataMember]
        public DateTime? UpdatedOn { get; set; } = DateTime.UtcNow;
    }

    [Serializable, DataContract(Namespace = NAMESPACE)]
    public abstract class BaseCreatedIntObject : BaseIntObject
    {
        public int? CreatedBy { get; set; }

        public DateTime CreatedOn
        {
            get
            {
                return this.createdon.HasValue
                   ? this.createdon.Value
                   : DateTime.UtcNow;
            }

            set { this.createdon = value; }
        }

        private DateTime? createdon = null;
    }

    [Serializable, DataContract(Namespace = NAMESPACE)]
    public abstract class BaseCreatedAndUpdatedIntObject : BaseIntObject, IUpdatedMetadata, ICreatedMetadata
    {
        public int? CreatedBy { get; set; }

        [DataMember]
        public DateTime? CreatedOn
        {
            get
            {
                return this.createdon.HasValue
                   ? this.createdon.Value
                   : DateTime.UtcNow;
            }

            set { this.createdon = value; }
        }

        private DateTime? createdon = null;

        public int? UpdatedBy { get; set; }

        [DataMember]
        public DateTime? UpdatedOn
        {
            get
            {
                return this.updatedon.HasValue
                   ? this.updatedon.Value
                   : DateTime.UtcNow;
            }

            set { this.updatedon = value; }
        }

        private DateTime? updatedon = null;
    }

    [Serializable, DataContract(Namespace = NAMESPACE)]
    public abstract class BaseCreatedObject : BaseObject
    {
        public int? CreatedBy { get; set; }

        public DateTime? CreatedOn
        {
            get
            {
                return this.createdon.HasValue
                   ? this.createdon.Value
                   : DateTime.UtcNow;
            }

            set { this.createdon = value; }
        }

        private DateTime? createdon = null;
    }

    [Serializable, DataContract(Namespace = NAMESPACE)]
    public abstract class BaseCreatedAndUpdatedObject : BaseObject
    {
        [DataMember]
        public int? CreatedBy { get; set; }

        [DataMember]
        public DateTime? CreatedOn
        {
            get
            {
                return this.createdon.HasValue
                   ? this.createdon.Value
                   : DateTime.UtcNow;
            }

            set { this.createdon = value; }
        }

        private DateTime? createdon = null;

        [DataMember]
        public int? UpdatedBy { get; set; }

        [DataMember]
        public DateTime? UpdatedOn
        {
            get
            {
                return this.updatedon.HasValue
                   ? this.updatedon.Value
                   : DateTime.UtcNow;
            }

            set { this.updatedon = value; }
        }

        private DateTime? updatedon = null;
    }


    [Serializable, DataContract(Namespace = NAMESPACE)]
    public abstract class BaseLongObject : BaseObject
    {
        [
        DataMember,
        Key,
        DatabaseGenerated(DatabaseGeneratedOption.Identity),
        Display(ResourceType = typeof(d360.core.resources.Fields), Name = "ID_Name", Description = "ID_Description")
        ]
        public long ID { get; set; }
    }

    [Serializable, DataContract(Namespace = NAMESPACE)]
    public abstract class BaseCreatedAndUpdatedLongObject : BaseLongObject
    {
        public int? CreatedBy { get; set; }

        [DataMember]
        public DateTime? CreatedOn
        {
            get
            {
                return this.createdon.HasValue
                   ? this.createdon.Value
                   : DateTime.UtcNow;
            }

            set { this.createdon = value; }
        }

        private DateTime? createdon = null;

        public int? UpdatedBy { get; set; }

        [DataMember]
        public DateTime? UpdatedOn
        {
            get
            {
                return this.updatedon.HasValue
                   ? this.updatedon.Value
                   : DateTime.UtcNow;
            }

            set { this.updatedon = value; }
        }

        private DateTime? updatedon = null;
    }




    [Serializable, DataContract(Namespace = NAMESPACE)]
    public abstract class BaseTemplateGuidObject : BaseObject
    {
        [DataMember, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Uid { get; set; }
    }

    [Serializable, DataContract(Namespace = NAMESPACE)]
    public abstract class BaseTemplateCreatedAndUpdatedGuidObject : BaseTemplateGuidObject
    {
        [DataMember]
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        [DataMember]
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
    }
}

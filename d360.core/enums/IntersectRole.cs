using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.enums
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class DependentIntersectRoleAttribute : Attribute 
    {
        public IntersectRole Role { get; set; }
        public DependentIntersectRoleAttribute(IntersectRole role)
        {
            Role = role;
        }
    }
    public enum IntersectRole
    {
        OriginatingSource = 1,
        [DependentIntersectRole(IntersectRole.OriginatingSource), DependentIntersectRole(IntersectRole.AcquiringSource)]
        Consumer = 2,
        [DependentIntersectRole(IntersectRole.OriginatingSource)]
        AcquiringSource = 3
    }
}

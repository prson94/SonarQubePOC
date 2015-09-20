using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;

namespace d360.core
{
    public static partial class Enums
    {
        public static Dictionary<int, string> GetEnumValuesAsDictionary<T>()
        {
            var model = new Dictionary<int, string>();

            var type = typeof(T);
            foreach (MemberInfo tm in type.GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                model.Add((int)Enum.Parse(type, tm.Name), tm.Name);
            }

            return model;
        }
    }
}

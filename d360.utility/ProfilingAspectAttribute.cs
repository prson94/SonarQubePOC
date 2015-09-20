using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Extensibility;
using System.Diagnostics;

namespace d360.utility
{
    [Serializable]
    public class ProfilingAspectAttribute : MethodLevelAspect //TypeLevelAspect
    {
        string Prefix { get; set; }
        bool WriteToEventLog { get; set; }

        public ProfilingAspectAttribute(bool writeToEventLog = false)
        {
            WriteToEventLog = writeToEventLog;
            Prefix = string.Empty;
        }
        public ProfilingAspectAttribute(string prefix, bool writeToEventLog = false) 
        {
            WriteToEventLog = writeToEventLog;
            Prefix = prefix;
        }

        //[OnMethodInvokeAdvice, MulticastPointcut(Targets = MulticastTargets.Method)]
        //public void OnInvoke(MethodInterceptionArgs args)
        //{
        //    Trace.WriteLine(string.Format("{0}.{1} - Before", Prefix, args.Method.Name));
        //    args.Proceed();
        //    Trace.WriteLine(string.Format("{0}.{1} - After", Prefix, args.Method.Name));
        //}

        //[OnMethodEntryAdvice, MulticastPointcut(Targets = MulticastTargets.Method)] 
        public void OnEntry(MethodExecutionArgs args)
        {
            string msg = string.Format("{0}.{1} - Before", Prefix, args.Method.Name);
            if (WriteToEventLog)
                EventLog.WriteEntry("D360", msg, EventLogEntryType.Error);
            else
                Trace.WriteLine(msg);
        }

        //[OnMethodExitAdvice(Master = "OnEntry")]
        public void OnExit(MethodExecutionArgs args)
        {
            string msg = string.Format("{0}.{1} - After", Prefix, args.Method.Name);
            if (WriteToEventLog)
                EventLog.WriteEntry("D360 API", msg, EventLogEntryType.Error);
            else
                Trace.WriteLine(msg);
        }

        //[OnLocationSetValueAdvice, MulticastPointcut(Targets = MulticastTargets.Property)]
        //public void OnPropertySet(LocationInterceptionArgs args)
        //{
        //    Trace.WriteLine(string.Format("Setting property: {0} = {1}.", args.LocationName, args.Value));
        //}
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PostSharp.Aspects;
using System.Diagnostics;
using System.Reflection;

namespace d360.utility
{
    [Serializable]
    public class ExceptionWrapper : OnExceptionAspect
    {

        public ExceptionWrapper(Type exceptionTypeToCover, bool writeToEventLog = false)
        {
            WriteToEventLog = writeToEventLog;
            ExceptionTypeToCover = exceptionTypeToCover;
        }

        bool WriteToEventLog { get; set; }
        public Type ExceptionTypeToCover { get; set; }

        public override void OnException(MethodExecutionArgs args)
        {
            string msg = string.Format("{0} had an error @ {1}: {2}\n{3}",
                                       args.Method.Name, 
                                       DateTime.Now,
                                       args.Exception.Message, 
                                       args.Exception.StackTrace
                                      );
            if (WriteToEventLog)
                EventLog.WriteEntry("Application", msg, EventLogEntryType.Error); //"D360 API", 
            else
                Trace.WriteLine(msg);

            //throw new Exception("There was a problem connecting to the underlying data source.  Please try again later.");
        }

        public override Type GetExceptionType(MethodBase targetMethod)
        {
            return ExceptionTypeToCover;
        }
    }
}

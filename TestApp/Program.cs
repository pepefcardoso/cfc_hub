using System;
using System.Linq;
using System.Reflection;

class P { 
    static void Main() {
        try {
            var asm1 = Assembly.Load("OpenTelemetry.Contrib.Extensions.AWSXRay");
            var t1 = asm1.GetType("OpenTelemetry.Trace.TracerProviderBuilderExtensions");
            if(t1 != null) {
                Console.WriteLine("AWSXRay Contrib Methods:");
                foreach(var m in t1.GetMethods()) Console.WriteLine(m.Name);
            }
        } catch(Exception e) { Console.WriteLine(e.Message); }
        
        try {
            var asm2 = Assembly.Load("OpenTelemetry.Instrumentation.AWS");
            var t2 = asm2.GetType("OpenTelemetry.Trace.TracerProviderBuilderExtensions");
            if(t2 != null) {
                Console.WriteLine("AWS Instrumentation Methods:");
                foreach(var m in t2.GetMethods()) Console.WriteLine(m.Name);
            }
        } catch(Exception e) { Console.WriteLine(e.Message); }
        
        try {
            var asm3 = Assembly.Load("OpenTelemetry.Contrib.Extensions.AWSXRay");
            Console.WriteLine("All types in AWSXRay Contrib:");
            foreach(var t in asm3.GetTypes()) Console.WriteLine(t.FullName);
        } catch(Exception e) { Console.WriteLine(e.Message); }
    } 
}

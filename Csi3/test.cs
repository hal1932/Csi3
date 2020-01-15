//#load "test1.cs"
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace Csi3
{
    class test
    {
        static void Main(string[] args)
        {
            //Debugger.Launch();
            //Debugger.Break();

            Console.WriteLine(string.Join(", ", args));
            Console.WriteLine("executing: " + Assembly.GetExecutingAssembly());
            Console.WriteLine("entry: " + Assembly.GetEntryAssembly());
            Console.WriteLine("calling: " + Assembly.GetCallingAssembly());
            Console.WriteLine("domain base: " + AppDomain.CurrentDomain.BaseDirectory);

            new Test1().F();

            //Debug.Close();
        }
    }
}

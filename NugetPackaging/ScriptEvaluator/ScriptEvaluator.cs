#region *   License     *
/*
    SimpleHelpers - ScriptEvaluator   

    Copyright © 2014 Khalid Salomão

    Permission is hereby granted, free of charge, to any person
    obtaining a copy of this software and associated documentation
    files (the “Software”), to deal in the Software without
    restriction, including without limitation the rights to use,
    copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the
    Software is furnished to do so, subject to the following
    conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
    OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
    NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
    HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
    WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
    FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
    OTHER DEALINGS IN THE SOFTWARE. 

    License: http://www.opensource.org/licenses/mit-license.php
    Website: https://github.com/khalidsalomao/SimpleHelpers.Net
 */
#endregion

using System;
using System.Linq;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;

namespace SimpleHelpers
{
    public class ScriptEvaluator
    {
        public string Script { get; set; }

        public bool HasError { get; set; }

        public string Message { get; set; }
        
        public string MainClassName { get; set; }

        private Mono.CSharp.CompiledMethod _createMethod;
        private List<Assembly> _assemblies = new List<Assembly> ();

        public ScriptEvaluator (string csharpCode, string mainClassName)
            : this (csharpCode, mainClassName, null)
        {            
        }

        public ScriptEvaluator (string csharpCode, string mainClassName, Type baseType)
            
        {
            Script = csharpCode;
            MainClassName = mainClassName;
            HasError = false;
            if (baseType != null)
            AddReference (baseType);
        }

        public static ScriptEvaluator Create (string csharpCode, string mainClassName)
        {
            return (new ScriptEvaluator (csharpCode, mainClassName)).Compile ();
        }

        public ScriptEvaluator Compile ()
        { 
            Message = null;
            HasError = false;

            var reportWriter = new System.IO.StringWriter ();
                        
            try
            {
                var settings = new Mono.CSharp.CompilerSettings ();
                settings.GenerateDebugInfo = false;
                settings.LoadDefaultReferences = true;
                settings.Optimize = true;
                settings.WarningsAreErrors = false;  
                
                var reporter = new Mono.CSharp.ConsoleReportPrinter (reportWriter);

                var ctx = new Mono.CSharp.CompilerContext (settings, reporter);

                var scriptEngine = new Mono.CSharp.Evaluator (ctx);

                AddReference (this.GetType ());

                // add assemblies
                for (int i = 0; i < _assemblies.Count; i++)
                {
                    scriptEngine.ReferenceAssembly (_assemblies[i]);   
                }
                
                if (String.IsNullOrWhiteSpace (Script))
                    throw new ArgumentNullException ("Expression");
                
                if (!scriptEngine.Run (Script))
                    throw new Exception (reportWriter.ToString ());

                if (reporter.ErrorsCount > 0)
                {
                    throw new Exception (reportWriter.ToString ());
                }

                _createMethod = scriptEngine.Compile ("new " + MainClassName + "()");

                if (reporter.ErrorsCount > 0)
                {
                    throw new Exception (reportWriter.ToString ());
                }
                if (_createMethod == null)
                {
                    throw new Exception ("script method could be created");
                }
            }
            catch (Exception e)
            {                
                Message = e.Message;
                HasError = true;
            }
            return this;
        }

        /// <summary>
        /// Try to add an assembly to the evaluator by checking first if it was already added.
        /// </summary>
        public ScriptEvaluator AddReference (Assembly assembly)
        {
            if (!_assemblies.Any (i => i.FullName == assembly.FullName))
                _assemblies.Add (assembly);
            return this;
        }

        /// <summary>
        /// Try to add an assembly by it type to the evaluator by checking first if it was already added.
        /// </summary>
        public ScriptEvaluator AddReference (Type type)
        {
            return AddReference (type.Assembly);
        }

        public T CreateInstance<T> () where T: class
        {
            return CreateInstance () as T;
        }

        public object CreateInstance ()
        {
            if (!HasError)
            {
                if (_createMethod == null)
                {
                    Compile ();
                }

                if (_createMethod != null)
                {
                    object result = null;
                    _createMethod (ref result);
                    return result;
                }
            }
            return null;
        }
    }
}
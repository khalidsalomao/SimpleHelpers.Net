#region *   License     *
/*
    SimpleHelpers - ModuleContainer   

    Copyright © 2015 Khalid Salomão

    Permission is hereby granted, free of charge, to any person
    obtaining a copy of this software and associated documentation
    files (the "Software"), to deal in the Software without
    restriction, including without limitation the rights to use,
    copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the
    Software is furnished to do so, subject to the following
    conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SimpleHelpers
{
    public class ModuleContainer
    {
        class ModuleInfo
        {
            public Type TypeInfo;
            public Func<object> Factory;
        }

        object padlock = new object ();

        HashSet<string> blackListedAssemblies = new HashSet<string> (StringComparer.OrdinalIgnoreCase) { "mscorlib", "vshost", "NancyHostLib", "NLog", "Newtonsoft.Json", "Topshelf", "Topshelf.Linux", "Topshelf.NLog", "Nancy", "AWSSDK", "Dapper", "Mono.CSharp", "Mono.Security", "NCrontab", "Renci.SshNet", "System.Net.FtpClient", "MongoDB.Bson", "MongoDB.Driver", "System.Data.SQLite", "System.Net.Http.Formatting", "System.Web.Razor", "Microsoft.Owin.Hosting", "Microsoft.Owin", "Owin" };

        Dictionary<string, Assembly> loadedAssemblies;
        List<Assembly> validAssemblies = new List<Assembly> (30);

        ConcurrentDictionary<string, List<Type>> exportedTypesByBaseType = new ConcurrentDictionary<string, List<Type>> (StringComparer.Ordinal);

        ConcurrentDictionary<string, ModuleInfo> exportedModules = new ConcurrentDictionary<string, ModuleInfo> (StringComparer.Ordinal);

        #region *   Events and Event Handlers   *

        public delegate void ModuleErrorEventHandler (string message, Exception ex);

        public event ModuleErrorEventHandler OnModuleLoadingError;

        private bool HasEventListeners ()
        {
            if (OnModuleLoadingError != null)
            {
                return OnModuleLoadingError.GetInvocationList ().Length != 0;
            }
            return false;
        }

        private void FireModuleLoadingError (string message, Exception ex)
        {
            if (HasEventListeners ())
                OnModuleLoadingError (message, ex);
        }

        #endregion


        static ModuleContainer _instance = new ModuleContainer ();

        /// <summary>
        /// Gets the singleton instance for the ModuleContainer.
        /// </summary>
        /// <value>The instance.</value>
        public static ModuleContainer Instance
        {
            get
            {
                return _instance;
            }
        }

        /// <summary>
        /// Initializes the specified modules folder.<para/>
        /// Will load all assemblies found in the modules folder and subfolders and
        /// scan for the types derived from listOfInterfaces.
        /// </summary>
        /// <param name="modulesFolder">List of folders where plugin/modules are located.</param>
        /// <param name="listOfInterfaces">The list of interfaces or base types.</param>
        public void LoadModules (string[] modulesFolder, Type[] listOfInterfaces = null)
        {
            ContainerInitialization (modulesFolder, listOfInterfaces);
        }

        /// <summary>
        /// Initializes the specified modules folder.<para/>
        /// Will load all assemblies found in the modules folder and subfolders and
        /// scan for the types derived from listOfInterfaces.
        /// </summary>
        /// <param name="modulesFolder">The modules folder.</param>
        /// <param name="listOfInterfaces">The list of interfaces or base types.</param>
        public void LoadModules (string modulesFolder, Type[] listOfInterfaces = null)
        {
            ContainerInitialization (new string[] { modulesFolder }, listOfInterfaces);
        }

        private void ContainerInitialization (string[] modulesFolder, Type[] listOfInterfaces)
        {
            lock (padlock)
            {
                if (loadedAssemblies == null)
                    loadedAssemblies = new Dictionary<string, Assembly> (StringComparer.Ordinal);

                // prepare extension folder
                if (modulesFolder == null || modulesFolder.Length == 0 || (modulesFolder.Length == 1 && String.IsNullOrEmpty (modulesFolder[0])))
                    modulesFolder = new string[] { };//new string[] { AppDomain.CurrentDomain.BaseDirectory };

                // get mscorelib assembly
                //Assembly mscorelib = 333.GetType ().Assembly;
                int oldAssembliesCount = loadedAssemblies.Count;

                if (loadedAssemblies.Count == 0)
                {
                    // register assembly resolution for our loaded modules
                    AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
                    AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                }

                // load current assemblies code to register their types and avoid duplicity
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies ())
                {
                    if (!a.IsDynamic && !loadedAssemblies.ContainsKey (a.FullName))
                    {
                        loadedAssemblies[a.FullName] = a;
                        if (!a.GlobalAssemblyCache && !blackListedAssemblies.Contains (ParseAssemblyName (a.FullName)))
                            validAssemblies.Add (a);
                    }
                }

                HashSet<string> parsedAssemblies = new HashSet<string> (StringComparer.OrdinalIgnoreCase);

                // check the directory exists
                foreach (var folder in modulesFolder)
                {
                    var path = PrepareFilePath (folder);
                    if (path == null || parsedAssemblies.Contains (path.Item1 + path.Item2))
                        continue;
                    parsedAssemblies.Add (path.Item1 + path.Item2);

                    // check folder existance
                    var directoryInfo = new DirectoryInfo (path.Item1);
                    if (!directoryInfo.Exists)
                        continue;

                    // read all files in modules folder, looking for assemblies
                    // let's read all files, since linux use case sensitive search wich could lead
                    // to case problems like ".dll" and ".Dll"            
                    foreach (var file in directoryInfo.EnumerateFiles (path.Item2, SearchOption.AllDirectories))
                    {
                        // check if file has a valid assembly extension
                        if (!file.Extension.EndsWith (".dll", StringComparison.OrdinalIgnoreCase) &&
                            !file.Extension.EndsWith (".exe", StringComparison.OrdinalIgnoreCase))
                            continue;

                        // check list of parsed files
                        LoadAssembly (file, parsedAssemblies);
                    }
                }

                if (oldAssembliesCount != loadedAssemblies.Count)
                {
                    exportedTypesByBaseType.Clear ();
                }

                // try to load derived types
                SearchForImplementations (listOfInterfaces);
            }
        }

        private void LoadAssembly (FileInfo file, HashSet<string> parsedAssemblies)
        {
            // check list of parsed files            
            // lets ignore it if it was already loaded or we were unable to load it...
            if (parsedAssemblies != null)
            {
                if (parsedAssemblies.Contains (file.Name))
                    return;
                parsedAssemblies.Add (file.Name);
            }

            // try to get assembly full name: "Assembly text name, Version, Culture, PublicKeyToken"
            // lets ignore it if we are unable to load it (access denied, invalid assembly or native code, etc...)
            string assemblyName = null;
            try
            {
                assemblyName = AssemblyName.GetAssemblyName (file.FullName).FullName;
            }
            catch (BadImageFormatException badImage)
            {
                return;
            }
            catch (Exception ex)
            {
                FireModuleLoadingError ("Assembly ignored. Load error: " +
                              ex.GetType ().Name + ", location: " + file.FullName.Replace (AppDomain.CurrentDomain.BaseDirectory, "./").Replace ('\\', '/'), ex);
                return;
            }

            // check if assembly was already loaded
            if (loadedAssemblies.ContainsKey (assemblyName))
                return;

            // try to load assembly
            try
            {
                // load assembly
                var a = Assembly.LoadFile (file.FullName);

                // register in our loaded assemblies lookup map for AppDomain.CurrentDomain.AssemblyResolve resolution
                loadedAssemblies.Add (assemblyName, a);
                if (!blackListedAssemblies.Contains (ParseAssemblyName (assemblyName)))
                    validAssemblies.Add (a);
            }
            catch (Exception ex)
            {
                FireModuleLoadingError ("Assembly ignored. Load error: " +
                              ex.GetType ().Name + ", location: " + file.FullName.Replace (AppDomain.CurrentDomain.BaseDirectory, "./").Replace ('\\', '/'), ex);
            }
        }

        private void SearchForImplementations (IList<string> fullTypeNameList)
        {
            SearchForImplementations (SearchForType (fullTypeNameList).ToArray ());
        }

        private void SearchForImplementations (IList<Type> listOfInterfaces)
        {
            // sanity check
            if (listOfInterfaces == null || listOfInterfaces.Count == 0)
                return;

            // prepare list of loaded interfaces
            for (int i = 0; i < listOfInterfaces.Count; i++)
            {
                if (!exportedTypesByBaseType.ContainsKey (listOfInterfaces[i].FullName))
                {
                    exportedTypesByBaseType[listOfInterfaces[i].FullName] = new List<Type> ();
                }
            }

             // try to load derived types
            try
            {
                // try to load derived types            
                List<Type> implementationsList;
                ModuleInfo info;

                foreach (var t in ListLoadedAssemblyTypes())
                {
                    if (t == null || t.IsAbstract || t.IsGenericTypeDefinition || !t.IsClass) //t.IsInterface
                        continue;
                    for (int j = 0; j < listOfInterfaces.Count; j++)
                    {
                        if (listOfInterfaces[j].IsAssignableFrom (t))
                        {
                            // register type in exportedModules map
                            if (!exportedModules.TryGetValue (t.FullName, out info))
                            {
                                info = new ModuleInfo { TypeInfo = t };
                                // register type by fullName (namespace + name) and name
                                exportedModules[t.FullName] = info;
                                exportedModules[t.Name] = info;
                            }

                            // add to interface list of types                                
                            if (!exportedTypesByBaseType.TryGetValue (listOfInterfaces[j].FullName, out implementationsList))
                            {
                                implementationsList = new List<Type> ();
                                exportedTypesByBaseType[listOfInterfaces[j].FullName] = implementationsList;
                            }
                            implementationsList.Add (t);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                FireModuleLoadingError ("Error loading assembly types.", ex);
            }
        }

        private Type SearchForType (string fullTypeName)
        {
            // search loaded types (slow)
            foreach (var t in ListLoadedAssemblyTypes ())
            { 
                if (fullTypeName == t.FullName)
                {
                    return t;
                }
            }
            return null;
        }

        private IEnumerable<Type> SearchForType (IList<string> fullTypeNameList)
        {            
            foreach (var name in fullTypeNameList)
            {
                // search loaded types (slow)
                var t = SearchForType (name);
                if (t != null)
                    yield return t;
            }
        }

        private IEnumerable<Type> ListLoadedAssemblyTypes()
        {
            // check if first assembly scan was executed
            if (loadedAssemblies == null)
                ContainerInitialization (null, null);

            // search listed assemblies
            foreach (var a in validAssemblies)
            {
                // dynamic assemblies don't have GetExportedTypes method
                if (a.IsDynamic)
                    continue;

                // try to list public types
                // only .net 4.5+ has this method implemented!
                Type[] types = null;
                try
                {
                    types = a.GetExportedTypes ();
                }
                catch (Exception ex)
                {
                    FireModuleLoadingError ("Assembly .net version lower than .net 4.5. Assembly: " + a.FullName + ", location: " + a.Location.Replace (AppDomain.CurrentDomain.BaseDirectory, "./").Replace ('\\', '/'), ex);
                }

                // check if types were listed!
                if (types == null)
                    continue;

                // search for types derived from desired types list (listOfInterfaces)
                for (int i = 0; i < types.Length; i++)
                {
                    yield return types[i];                        
                }
            }           
        }

        private Assembly CurrentDomain_AssemblyResolve (object sender, ResolveEventArgs args)
        {
            Assembly a;
            if (!loadedAssemblies.TryGetValue (args.Name, out a))
            {
                var aName = new AssemblyName (args.Name);
                if (!args.RequestingAssembly.IsDynamic && !args.RequestingAssembly.GlobalAssemblyCache)
                {
                    var path = Path.Combine (Path.GetDirectoryName (args.RequestingAssembly.Location), aName.Name);
                    if (File.Exists (path + ".dll"))
                        LoadAssembly (new FileInfo (path + ".dll"), null);
                    else if (File.Exists (path + ".exe"))
                        LoadAssembly (new FileInfo (path + ".exe"), null);
                    loadedAssemblies.TryGetValue (args.Name, out a);
                }
                //if (a == null)
                //{
                //    throw new InvalidOperationException (
                //        String.Format ("Assembly not available in plugin/modules path; assembly name '{0}'.", args.Name));                        
                //}
            }
            return a;
        }

        private static string ParseAssemblyName (string name)
        {
            return new AssemblyName (name).Name;
        }

        private static string ParseFirstNamespace (string name)
        {
            int i = name.IndexOf ('.');
            return (i > 0) ? name.Substring (0, i) : name;
        }
        /// <summary>
        /// Adjust file path.
        /// </summary>
        public static Tuple<string, string> PrepareFilePath (string path)
        {
            if (String.IsNullOrWhiteSpace (path))
                return null;
            int ix = path.IndexOf ("${basedir}", StringComparison.OrdinalIgnoreCase);
            if (ix >= 0)
            {
                int tagLen = "${basedir}".Length;
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                // check ending with tag
                if (path.Length > ix + tagLen && (path[ix + tagLen] == '\\' || path[ix + tagLen] == '/'))
                    appDir = appDir.EndsWith ("/") || appDir.EndsWith ("\\") ? appDir.Substring (0, appDir.Length - 1) : appDir;
                // replace tag (ignorecase)
                path = path.Remove (ix, tagLen);
                path = path.Insert (ix, appDir);
            }
            path = path.Replace ('\\', '/');

            // split 
            var s = SplitByLastPathPart (path);
            // check split result
            if (String.IsNullOrWhiteSpace (s.Item1))
                return null;
            if (String.IsNullOrWhiteSpace (s.Item2))
                return Tuple.Create (s.Item1, "*");
            // if there is no file extension and no wild card, treat path as directory
            if (s.Item2.IndexOf ('.') < 0 && s.Item2.IndexOf ('*') < 0)
                return Tuple.Create (path, "*");
            return s;
        }

        public static Tuple<string, string> SplitByLastPathPart (string pattern)
        {
            if (pattern != null)
            {
                var pos = Math.Max (pattern.LastIndexOf ('\\'), pattern.LastIndexOf ('/')) + 1;
                if (pos < 1)
                    return Tuple.Create (pattern, "");
                return Tuple.Create (pos > 0 ? pattern.Substring (0, pos) : "", pattern.Substring (pos));
            }
            return null;
        }

        /// <summary>
        /// Creates the type constructor delegate for a type.
        /// </summary>
        /// <typeparam name="T">The type to create a constructor.</typeparam>
        /// <returns></returns>
        private Func<object> CreateFactory<T> ()
        {
            return CreateFactory (typeof (T));
        }

        /// <summary>
        /// Creates the type constructor delegate for a type.
        /// </summary>
        /// <param name="type">The type to create a constructor.</param>
        /// <returns></returns>
        private Func<object> CreateFactory (Type type)
        {
            var ctor = type.GetConstructors ().Where (i => i.GetParameters ().Length == 0).FirstOrDefault ();
            if (ctor == null)
                throw new Exception (String.Format ("Type {0} must have a parameters less contructor!", type.FullName));
            return CreateFactory (ctor);
        }

        /// <summary>
        /// Creates the type constructor delegate for a type..
        /// </summary>
        /// <param name="ctor">The constructor information.</param>
        /// <returns></returns>
        private Func<object> CreateFactory (ConstructorInfo ctor)
        {
            // code http://rogeralsing.com/2008/02/28/linq-expressions-creating-objects/
            Type type = ctor.DeclaringType;
            ParameterInfo[] paramsInfo = ctor.GetParameters ();
            LambdaExpression lambda;

            // prepare parameters            
            //create a single param of type object[]
            var param = Expression.Parameter (typeof (object[]), "args");

            var argsExp = new Expression[paramsInfo.Length];

            //pick each arg from the params array 
            //and create a typed expression of them
            for (int i = 0; i < paramsInfo.Length; i++)
            {
                var index = Expression.Constant (i);
                Type paramType = paramsInfo[i].ParameterType;

                var paramAccessorExp = Expression.ArrayIndex (param, index);

                var paramCastExp = Expression.Convert (paramAccessorExp, paramType);

                argsExp[i] = paramCastExp;
            }

            //make a NewExpression that calls the
            //ctor with the args we just created
            var newExp = paramsInfo.Length > 0 ? Expression.New (ctor, argsExp) : Expression.New (ctor);

            //create a lambda with the New
            //Expression as body and our param object[] as arg
            lambda = paramsInfo.Length > 0 ? Expression.Lambda (typeof (Func<object>), newExp, param) : Expression.Lambda (typeof (Func<object>), newExp);

            //compile it
            var compiled = (Func<object>)lambda.Compile ();
            return compiled;
        }

        /// <summary>
        /// Gets an instance for a registered type and cast it
        /// </summary>
        /// <typeparam name="T">The type of the T.</typeparam>
        /// <param name="fullTypeName">Full name of the type.</param>
        /// <returns></returns>
        public T GetInstanceAs<T> (string fullTypeName) where T : class
        {
            return GetInstance (fullTypeName) as T;
        }

        /// <summary>
        /// Gets an instance for a registered type and cast it.
        /// </summary>
        /// <typeparam name="T">The type of the T.</typeparam>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public T GetInstanceAs<T> (Type type) where T : class
        {
            return GetInstance (type) as T;
        }

        /// <summary>
        /// Gets an instance for a registered type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public object GetInstance (Type type)
        {
            var ctor = GetConstructor (type);
            return ctor != null ? ctor () : null;
        }

        /// <summary>
        /// Gets an instance for a registered type by its full type name.
        /// </summary>
        /// <param name="fullTypeName">Full name of the type, namespace and class name.</param>
        /// <returns></returns>
        public object GetInstance (string fullTypeName)
        {
            var ctor = GetConstructor (fullTypeName);
            return ctor != null ? ctor () : null;
        }

        /// <summary>
        /// Gets an instance that implements the desired type T.
        /// </summary>
        /// <param name="type">The base type.</param>
        /// <returns></returns>
        public T GetInstanceOf<T> () where T : class
        {
            return GetInstancesOf<T> ().FirstOrDefault ();
        }

        /// <summary>
        /// Gets instances for all registered types for a given interface or base type.
        /// </summary>
        /// <typeparam name="T">The interface or base type.</typeparam>
        /// <returns>List of intances of registered types</returns>
        public IEnumerable<T> GetInstancesOf<T> () where T : class
        {
            foreach (var t in GetTypesOf<T> ())
                yield return GetInstanceAs<T> (t.FullName);
        }

        /// <summary>
        /// Gets all registered types for a given interface or base type.
        /// </summary>
        /// <typeparam name="T">The interface or base type.</typeparam>
        /// <returns>List of registered types</returns>
        public IEnumerable<Type> GetTypesOf<T> () where T : class
        {
            return GetTypesOf (typeof(T));
        }

        /// <summary>
        /// Gets all registered types for a given interface or base type.
        /// </summary>
        /// <param name="type">The desired type or base type or interface.</param>
        /// <returns>List of registered types</returns>
        public IEnumerable<Type> GetTypesOf (Type type)
        {
            List<Type> list;
            // load interface implementations
            if (!exportedTypesByBaseType.TryGetValue (type.FullName, out list))
            {
                // if none was found, it was not initilized yet...
                SearchForImplementations (new[] { type });
                // after the interface initilization, check again...
                if (!exportedTypesByBaseType.TryGetValue (type.FullName, out list))
                    yield break;
            }
            // return implementations
            for (int i = 0; i < list.Count; i++)
                yield return list[i];
        }

        /// <summary>
        /// Gets the constructor for the desired type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public Func<object> GetConstructor (Type type)
        {
            ModuleInfo module;
            // check type registration, if none was found, it was not initilized yet...
            if (!exportedModules.TryGetValue (type.FullName, out module))
            {
                SearchForImplementations (new[] { type });
                exportedModules.TryGetValue (type.FullName, out module);
            }
            // if we have the type, lets get it
            if (module != null)
            {
                if (module.Factory == null)
                {
                    module.Factory = CreateFactory (module.TypeInfo);
                }
                return module.Factory;
            }
            return null;
        }

        /// <summary>
        /// Gets the constructor for the desired type.
        /// </summary>
        /// <param name="fullTypeName">Full name of the type.</param>
        /// <returns></returns>
        public Func<object> GetConstructor (string fullTypeName)
        {
            ModuleInfo module;
            // check type registration, if none was found, it was not initilized yet...
            if (!exportedModules.TryGetValue (fullTypeName, out module))
            {
                SearchForImplementations (new[] { fullTypeName });
                exportedModules.TryGetValue (fullTypeName, out module);
            }
            // if we have the type, lets get it
            if (module != null)
            {
                if (module.Factory == null)
                {
                    module.Factory = CreateFactory (module.TypeInfo);
                }
                return module.Factory;
            }
            return null;
        }
    }
}
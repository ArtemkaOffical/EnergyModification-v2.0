using Energy.Attributes;
using Energy.Plugins;
using Microsoft.CodeAnalysis;
using System.Reflection;

namespace Energy
{
    public class EnergyCore
    {
        private Dictionary<string, BaseLibrary> _ibrariesLoaders;
        private Dictionary<string, HashSet<Plugin>> _subscribedHooks;
        private Dictionary<string, (Plugin plugin, MethodInfo method)> _pluginCommands;
        private  Dictionary<string, Plugin> _plugins;
        private  FileSystemWatcher _watcher;

        private  string _LibrariesPath = "Libraries";

        public void Init()
         {
            Interface.InjectCore(this);

            if (!Directory.Exists(_LibrariesPath))
                Directory.CreateDirectory(_LibrariesPath);

            if (!Directory.Exists(BaseLibrary.PLUGIN_PATH))
                Directory.CreateDirectory(BaseLibrary.PLUGIN_PATH);

            _ibrariesLoaders = new Dictionary<string, BaseLibrary>();
            _subscribedHooks = new Dictionary<string, HashSet<Plugin>>();
            _pluginCommands = new Dictionary<string, (Plugin plugin, MethodInfo method)>();
            _plugins = new Dictionary<string, Plugin>();
            _watcher = new FileSystemWatcher(BaseLibrary.PLUGIN_PATH, "*.cs");
            _watcher.EnableRaisingEvents = true;
            _watcher.Created += _watcher_Created;
            _watcher.Deleted += _watcher_Deleted;

            InitLibraries(_ibrariesLoaders);

            while (true)
            {
                var cmd = Console.ReadLine();
                Interface.CallCommand(cmd);
                Interface.CallHook("Logger");
            }
         }

        public Dictionary<string, HashSet<Plugin>> GetSubHooks() => _subscribedHooks;
        public Dictionary<string, (Plugin plugin, MethodInfo method)> GetPluginCommands() => _pluginCommands;

        public void AddLibrary(BaseLibrary library)
        {
            if (library == null)
            {
                Console.WriteLine("This Library is null");
                return;
            }

            if (_ibrariesLoaders.ContainsKey(library.Name))
            {
                Console.WriteLine($"The Library {library.Name} has been added");
                return;
            }

            _ibrariesLoaders.Add(library.Name, library);
        }

        public Plugin GetPlugin(string name)
        {
            if (_plugins.TryGetValue(name, out Plugin plugin))
                return plugin;
            return null;
        }

        public Plugin GetPlugin(Plugin plugin)
        {
            var pl = _plugins.First(x => x.Value == plugin).Value;
            if (pl != null) return pl;
            return null;
        }

        private void _watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            var pluginName = Path.GetFileNameWithoutExtension(e.Name);

            var plugin = GetPlugin(pluginName);

            foreach (var library in _ibrariesLoaders)
            {
                library.Value.RemovePlugin(plugin);
                _plugins.Remove(pluginName);
                UnSubscribePluginHooks(plugin);
                Console.WriteLine($"The {plugin} has been unload ");
            }

        }

        private void _watcher_Created(object sender, FileSystemEventArgs e)
        {
            var pluginName = Path.GetFileNameWithoutExtension(e.Name);

            foreach (var library in _ibrariesLoaders)
            {
                var plugin = library.Value.LoadPlugin(e.FullPath);
                _plugins.Add(plugin.GetType().Name, plugin);
                SubscribePluginHooks(plugin);
                Console.WriteLine($"The {plugin} has been load ");
            }
        }

        private void UnSubscribePluginHooks(Plugin plugin)
        {
            foreach (var hooks in _subscribedHooks)
            {
                if (hooks.Value.Contains(plugin))
                    hooks.Value.Remove(plugin);
            }
            var pl = _pluginCommands.FirstOrDefault(x => x.Value.Item1 == (plugin));
            _pluginCommands.Remove(pl.Key);

        }

        private void SubscribePluginHooks(Plugin plugin)
        {
            var hooks = plugin.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                 .Where(x => x.GetCustomAttribute(typeof(OnExecuteAttribute), false) != null);

            var commandHooks = plugin.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.GetCustomAttribute(typeof(CommandAttribute), false) != null);

            foreach (var hook in hooks)
            {
                if (!_subscribedHooks.ContainsKey(hook.Name))
                {
                    _subscribedHooks.Add(hook.Name, new HashSet<Plugin>() { plugin });
                    continue;
                }
                _subscribedHooks[hook.Name].Add(plugin);
            }

            foreach (var hook in commandHooks)
            {
                var command = hook.GetCustomAttribute<CommandAttribute>(false).Command;
                if (!_pluginCommands.ContainsKey(command))
                {
                    _pluginCommands.Add(command, (plugin, hook));
                    continue;
                }
                Console.WriteLine($"this command - {command} is already used in the plugin {plugin.GetType().Name}");
            }

        }

        private void LoadPluginsOfLibrary(BaseLibrary library, Dictionary<string, Plugin> mainCollectionOfPlugins)
        {
            foreach (var plugin in library.GetPlugins())
            {
                mainCollectionOfPlugins.Add(plugin.GetType().Name, plugin);
                SubscribePluginHooks(plugin);
            }

        }

        private void InitLibraries(Dictionary<string, BaseLibrary> _libraryLoaders)
        {
            var files = Directory.GetFiles(_LibrariesPath, "*.dll");

            foreach (var file in files)
            {
                Assembly assembly = Assembly.LoadFrom(file);

                if (assembly.GetExportedTypes()[0].BaseType == typeof(BaseLibrary))
                {
                    var types = assembly.GetExportedTypes();
                    Type FileClass = assembly.GetType($"{types[0].FullName}");
                    // object obj = FileClass.GetConstructor(new Type[0]).Invoke(new object[0]);
                    var library = Activator.CreateInstance(FileClass) as BaseLibrary;
                   if(library.IsLoaded)
                    {
                        AddLibrary(library);
                        Console.WriteLine($"The Library - {library.Name} has been loaded!");
                    }
                    if (library.IsMessenger)
                    {
                        library.LoadAll();
                        LoadPluginsOfLibrary(library, _plugins);
                    }
                }

            }
        }

        public static class Interface
        {
            public static EnergyCore EnergyCore { get; private set; }
            public static void InjectCore(EnergyCore core) => EnergyCore = core;
            public static bool IsInit { get {  return EnergyCore != null; } }
            public static void CallHook(string hookName, params object[] args)
            {
                if (!IsInit)
                {
                    Console.WriteLine($"CallHook {hookName} not working. EnergyCore is not init. Use InjectLibrary");
                    return;
                }

                var plugins = EnergyCore.GetSubHooks().TryGetValue(hookName, out var listOfPlugins);

                if (plugins && listOfPlugins != null && listOfPlugins.Count > 0)

                    foreach (var plugin in listOfPlugins)
                        plugin.CallHook(hookName, args);
            }

            public static object CallCommand(string command, params object[] args)
            {
                if (!IsInit)
                {
                    Console.WriteLine($"CallCommand {command} not working. EnergyCore is not init. Use InjectLibrary");
                    return null;
                }

                var plugins = EnergyCore.GetPluginCommands().TryGetValue(command, out var data);

                if (plugins && data.method != null && data.plugin != null)
                    return data.plugin.CallHook(data.method.Name, args);

                return null;
            }
        }
    }

   
}
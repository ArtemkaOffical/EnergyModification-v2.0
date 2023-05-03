using Energy.Plugins;

namespace Energy
{
    public abstract class BaseLibrary
    {
        public const string PLUGIN_PATH = "Plugins";
        public string Name { get => GetType().Name; }
        public bool IsMessenger { get; private set; }
        public bool IsLoaded { get; private set; }
        private HashSet<Plugin> _plugins = new HashSet<Plugin>();
        private Type _pluginType = typeof(Plugin);

        public BaseLibrary()
        {
            Init();
            if (_pluginType == typeof(Plugin))
            {
                Console.WriteLine("PluginType is default. Load is failed. Use InjectPluginType int Init();");
                IsMessenger = false;
                return;
            }
            if (_pluginType == null)
                IsMessenger = false;
            IsMessenger = true;
            IsLoaded = true;
        }

        public HashSet<Plugin> GetPlugins() => _plugins;   

        public virtual Plugin LoadPlugin(string filePath)
        {

            var plugin = Compiler.Compile(filePath);

            if (plugin == null || plugin.GetType().BaseType != _pluginType) return null;

            plugin.InjectLibrary(this);
            _plugins.Add(plugin);

            Console.WriteLine($"The {plugin} has been load. Library - {Name}");

            return plugin;

        }

        public void RemovePlugin(Plugin plugin)
        {
            if (_plugins.Contains(plugin))
                _plugins.Remove(plugin);
        }
        
        public void InjectPluginType(Type type)=>_pluginType = type;

        public virtual void LoadAll()
        {
            var files = Directory.GetFiles(PLUGIN_PATH, "*.cs");

            foreach (var file in files) 
                LoadPlugin(Path.GetFullPath(file));
                        
        }

        public abstract void Init();

    }
}

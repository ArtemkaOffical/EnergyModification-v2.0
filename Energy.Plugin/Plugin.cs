using Energy.Attributes;

namespace Energy.Plugins
{
    [Info("BasePlugin", "Artemka", "1")]
    public abstract class Plugin
    {
        public readonly PluginInfo Info;
        public  object Library ;
        public Plugin()
        {
            var attributes = GetType().GetCustomAttributes(typeof(InfoAttribute),false);

            if (attributes.Length != 0)
            {
                var info = (InfoAttribute)attributes[0];
                Info = new PluginInfo(info.Title, info.Author, info.Version);
            }

        }
        public void InjectLibrary<T>(T library) where T : class
        {
            Library = library;
        }
        public virtual object? CallHook(string hookName, params object?[]? args) { return null; }
        
        public override string ToString()
        {
            return $"Plugin: {Info.Title}. Author: {Info.Author}. Version: {Info.Version}";
        }

    }
    public class PluginInfo
    {
        public string Title { get; }
        public string Author { get; }
        public string Version { get; }

        public PluginInfo(string title, string author, string version)
        {
            Title = title;
            Version = version;
            Author = author;
        }
    }
}

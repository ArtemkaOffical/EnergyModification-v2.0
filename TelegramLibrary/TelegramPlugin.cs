using Energy.Attributes;
using Energy.Plugins;
using System.Reflection;


namespace Energy.Telegram
{
    //Default info if it is not in the plugin
    [Info("TelegramPlugin","Artemka")]
    public class TelegramPlugin : Plugin
    {
        public string Author { get => Info.Author; }
        public TelegramLibrary Library { get => base.Library as TelegramLibrary; }

        public override object? CallHook(string hookName, params object?[]? args)
        {
            return GetType()?.GetMethod(hookName, BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(this, args);
        }

        //write logic plugin for telegram here and use it in plugins
        //For example
        public bool KickUser(string userId)
        {
            var groupId = Library.GetCurrentIdGroup();
            //kick user. For example - GetGroup(groupId).Kick(userId);
            // if(error) return false;
            // return true;
            return true;
        }

        

    }
}

using Energy.Plugins;
using Energy.Telegram;
using static Energy.EnergyCore;

namespace Energy
{
    public class TelegramLibrary : BaseLibrary
    {
        

        public override void Init()
        {
           InjectPluginType(typeof(TelegramPlugin));                     
        }
        public object GetCurrentIdGroup()
        {
            return null;
        }
        public void GetMessage()
        {
            Interface.CallHook("Logger");
        }

    }
}

using System;
using System.IO;
using System.Linq;
using System.Media;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using Styx;
using Styx.Logic;
using Styx.Helpers;
using Styx.Logic.POI;
using Styx.WoWInternals;
using Styx.Logic.Profiles;
using Styx.Logic.BehaviorTree;
using Styx.Plugins.PluginClass;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;


namespace WhisperNotify
{
    public class WhisperNotify : HBPlugin
    {
		public override string Name { get { return "WhisperNotify"; } }
        public override string Author { get { return "Melodie"; } }
        public override Version Version { get { return new Version(1, 0); } }
        public override bool WantButton { get { return false; } }
       
	    private readonly LocalPlayer Me = ObjectManager.Me;
		
		public bool init;
		
		
		public static void ilog(string format, params object[] args)
        { 
			Logging.Write(Color.Yellow, "[WhisperNotify]: " + format, args); 
		}
		
		private static void PlaySound(string soundFile)
        {
            new SoundPlayer(Path.Combine(Logging.ApplicationPath, @"Plugins\WhisperNotify\") + soundFile).Play();
        }
		
		

		public override void Pulse()
        {
			if (init == null || init == false)
            {
                Initialize();
                init = true;
            }
		}
		
		public override void Initialize()
        {
			if (init == null || init == false)
            {
				Lua.Events.AttachEvent("CHAT_MSG_WHISPER", HandleIncomingWhisper);
				Lua.Events.AttachEvent("CHAT_MSG_BN_WHISPER", HandleIncomingWhisper);
				ilog("v. " + Version + " by Melodie");
				init = true;
			}
            
        }

        public override void Dispose()
        {
            Lua.Events.DetachEvent("CHAT_MSG_WHISPER", HandleIncomingWhisper);
			Lua.Events.DetachEvent("CHAT_MSG_BN_WHISPER", HandleIncomingWhisper);
        }
		
		
		private void HandleIncomingWhisper(object sender, LuaEventArgs e)
        {
            ilog("Someone whispered us -> Playing sound.");
            PlaySound("WhisperNotifySound.wav");
        }
		
		
	}
}
		
		
		
		
		
		
		
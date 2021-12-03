﻿namespace ml_ahr
{
    public class Main : MelonLoader.MelonMod
    {
        HeightAdjuster m_localAdjuster = null;

        public override void OnApplicationStart()
        {
            Settings.Load();

            VRChatUtilityKit.Utilities.NetworkEvents.OnRoomJoined += this.OnRoomJoined;
            VRChatUtilityKit.Utilities.NetworkEvents.OnRoomLeft += this.OnRoomLeft;
            VRChatUtilityKit.Utilities.NetworkEvents.OnAvatarInstantiated += this.OnAvatarInstantiated;
        }

        public override void OnPreferencesSaved()
        {
            bool l_oldState = Settings.Enabled;
            Settings.Reload();

            if((m_localAdjuster != null) && (l_oldState != Settings.Enabled))
            {
                if(Settings.Enabled)
                {
                    float l_height = Utils.GetTrackingHeight();
                    m_localAdjuster.ChangeHeight(l_height, l_height * 0.5f);
                }
                else
                {
                    m_localAdjuster.ChangeHeight(1.65f, 0.85f); // Default VRChat values
                }
            }
        }

        void OnRoomJoined()
        {
            MelonLoader.MelonCoroutines.Start(CreateLocalAdjuster());
        }
        System.Collections.IEnumerator CreateLocalAdjuster()
        {
            while(Utils.GetLocalPlayer() == null) yield return null;
            m_localAdjuster = Utils.GetLocalPlayer().gameObject.AddComponent<HeightAdjuster>();
        }

        void OnRoomLeft()
        {
            m_localAdjuster = null;
        }

        void OnAvatarInstantiated(VRCAvatarManager f_manager, VRC.Core.ApiAvatar f_apiAvatar, UnityEngine.GameObject f_avatarObject)
        {
            if(Settings.Enabled)
            {
                var l_player = f_avatarObject.transform.root.GetComponent<VRCPlayer>();
                if((l_player != null) && (l_player == Utils.GetLocalPlayer()) && (m_localAdjuster != null))
                {
                    float l_height = Utils.GetTrackingHeight();
                    m_localAdjuster.ChangeHeight(l_height, l_height * 0.5f);
                }
            }
        }
    }
}

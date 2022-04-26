using System;
using UIExpansionKit.API;
using UIExpansionKit.API.Controls;
using VRC;
using Object = UnityEngine.Object;

namespace ml_alg
{
    public class AvatarLimbsGrabber : MelonLoader.MelonMod
    {
        bool m_quit = false;

        object m_menuSettings = null;
        object m_menuLabelWorld = null;
        object m_buttonPlayerAllow = null;

        bool m_update = false;
        LiftedPlayer m_localLiftedPlayer = null;
        Player m_selectedPlayer = null;

        public override void OnApplicationStart()
        {
            MethodsResolver.ResolveMethods();
            Settings.LoadSettings();

            VRChatUtilityKit.Utilities.VRCUtils.OnUiManagerInit += OnUiManagerInit;
            VRChatUtilityKit.Utilities.NetworkEvents.OnRoomJoined += OnJoinedRoom;
            VRChatUtilityKit.Utilities.NetworkEvents.OnRoomLeft += OnLeftRoom;
            VRChatUtilityKit.Utilities.NetworkEvents.OnPlayerJoined += OnPlayerJoined;
            VRChatUtilityKit.Utilities.NetworkEvents.OnFriended += OnFriended;
            VRChatUtilityKit.Utilities.NetworkEvents.OnUnfriended += OnUnfriended;
        }

        public override void OnApplicationQuit()
        {
            m_quit = true;
        }

        public override void OnPreferencesSaved()
        {
            if(!m_quit) // This is not a joke
            {
                Settings.ReloadSettings();

                if(m_update && (m_localLiftedPlayer != null) && Settings.IsAnyEntryUpdated())
                {
                    if(Settings.IsFriendsEntryUpdated())
                    {
                        if(!Settings.AllowFriends)
                        {
                            OnDisallowAll();
                            //add component on friends 
                            foreach(Player l_remotePlayer in Utils.GetFriendsInInstance())
                            {
                                LifterPlayer l_component = l_remotePlayer.GetComponent<LifterPlayer>();
                                if(l_component != null)
                                {
                                    l_component = l_remotePlayer.gameObject.AddComponent<LifterPlayer>();
                                    l_component.AddLifted(m_localLiftedPlayer);
                                }
                                goto IL_BC;
                            }
                        }
                        //add component on everyone 
                        foreach (Player l_remotePlayer in Utils.GetPlayers())
						{
                            LifterPlayer l_component = l_remotePlayer.GetComponent<LifterPlayer>();
                            if (l_component != null)
                            {
                                l_component = l_remotePlayer.gameObject.AddComponent<LifterPlayer>();
                                l_component.AddLifted(m_localLiftedPlayer);
                            }
                        }
                    }
                    IL_BC:
                    m_localLiftedPlayer.AllowPull = Settings.AllowPull;
                    m_localLiftedPlayer.AllowHeadPull = Settings.AllowHeadPull;
                    m_localLiftedPlayer.AllowHandsPull = Settings.AllowHandsPull;
                    m_localLiftedPlayer.AllowHipsPull = Settings.AllowHipsPull;
                    m_localLiftedPlayer.AllowLegsPull = Settings.AllowLegsPull;
                    m_localLiftedPlayer.GrabDistance = Settings.GrabDistance;
                    m_localLiftedPlayer.SavePose = Settings.SavePose;
                    m_localLiftedPlayer.UseVelocity = Settings.UseVelocity;
                    m_localLiftedPlayer.VelocityMultiplier = Settings.VelocityMultiplier;
                    m_localLiftedPlayer.AverageVelocity = Settings.UseAverageVelocity;
                    m_localLiftedPlayer.DistanceScale = Settings.DistanceScale;
                    m_localLiftedPlayer.ReapplyPermissions();
                }
            }
        }

        public override void OnUpdate()
        {
            if(m_update && ((IMenuToggle)m_buttonPlayerAllow).CurrentInstance.activeInHierarchy)
            {
                Player l_selectedPlayer = Utils.GetPlayerQM();
                if((l_selectedPlayer != null) && (m_selectedPlayer != l_selectedPlayer))
                {
                    m_selectedPlayer = l_selectedPlayer;
                    ((IMenuToggle)m_buttonPlayerAllow).Selected = m_selectedPlayer.GetComponent<LifterPlayer>() != null;
                }
            }
        }

        private void OnUiManagerInit()
		{
			m_menuSettings = ExpansionKitApi.CreateCustomQuickMenuPage(new LayoutDescription?(LayoutDescription.WideSlimList));
            m_menuLabelWorld = ((ICustomShowableLayoutedMenu)m_menuSettings).AddLabel("");
			((ICustomShowableLayoutedMenu)m_menuSettings).AddSimpleButton("Reset manipulated pose", new Action(OnPoseReset));
			((ICustomShowableLayoutedMenu)m_menuSettings).AddSimpleButton("Disallow manipulation for everyone in room", new Action(OnDisallowAll));
			((ICustomShowableLayoutedMenu)m_menuSettings).AddSimpleButton("Allow manipulation for everyone in room", new Action(OnAllowAll));
			((ICustomShowableLayoutedMenu)m_menuSettings).AddSimpleButton("Allow manipulation for friends in room", new Action(OnAllowFriends));
			((ICustomShowableLayoutedMenu)m_menuSettings).AddSimpleButton("Close", new Action(OnMenuClose));
			ExpansionKitApi.GetExpandedMenu(ExpandedMenu.QuickMenu).AddSimpleButton("Avatar limbs grabber", new Action(OnMenuShow));
			m_buttonPlayerAllow = ExpansionKitApi.GetExpandedMenu(ExpandedMenu.UserQuickMenu).AddToggleButton("Limbs manipulation", new Action<bool>(OnManipulationToggle), null);
		}

        void OnJoinedRoom()
        {
            m_update = true;
            MelonLoader.MelonCoroutines.Start(CreateLocalLifted());
        }
        System.Collections.IEnumerator CreateLocalLifted()
        {
            while(Utils.GetLocalPlayer() == null)
                yield return null;

            m_localLiftedPlayer = Utils.GetLocalPlayer().gameObject.AddComponent<LiftedPlayer>();
            m_localLiftedPlayer.AllowPull = Settings.AllowPull;
            m_localLiftedPlayer.AllowHeadPull = Settings.AllowHeadPull;
            m_localLiftedPlayer.AllowHandsPull = Settings.AllowHandsPull;
            m_localLiftedPlayer.AllowHipsPull = Settings.AllowHipsPull;
            m_localLiftedPlayer.AllowLegsPull = Settings.AllowLegsPull;
            m_localLiftedPlayer.GrabDistance = Settings.GrabDistance;
            m_localLiftedPlayer.SavePose = Settings.SavePose;
            m_localLiftedPlayer.UseVelocity = Settings.UseVelocity;
            m_localLiftedPlayer.VelocityMultiplier = Settings.VelocityMultiplier;
            m_localLiftedPlayer.AverageVelocity = Settings.UseAverageVelocity;
            m_localLiftedPlayer.DistanceScale = Settings.DistanceScale;

            ((IMenuLabel)m_menuLabelWorld).SetText("World pull permission: <color=#" + (VRChatUtilityKit.Utilities.VRCUtils.AreRiskyFunctionsAllowed ? "00FF00>Allowed" : "FF0000>Also Allowed") + "</color>");
        }

        void OnLeftRoom()
        {
            m_update = false;
            m_localLiftedPlayer = null;
            m_selectedPlayer = null;
        }

        void OnPlayerJoined(Player p_player)
        {
            if(Settings.AllowFriends && Utils.IsFriend(p_player))
                MelonLoader.MelonCoroutines.Start(CreateLifterOnJoin(p_player));
        }
        System.Collections.IEnumerator CreateLifterOnJoin(Player p_player)
        {
            while(m_localLiftedPlayer == null)
                yield return null;

            if(p_player != null)
            {
                LifterPlayer l_component = p_player.gameObject.AddComponent<LifterPlayer>();
                l_component.AddLifted(m_localLiftedPlayer);
            }
        }

        void OnFriended(VRC.Core.APIUser p_apiPlayer)
        {
            if(m_update && (m_localLiftedPlayer != null) && Settings.AllowFriends)
            {
                Player l_remotePlayer = Utils.GetPlayerWithId(p_apiPlayer.id);
                if(l_remotePlayer != null)
                {
                    LifterPlayer l_component = l_remotePlayer.GetComponent<LifterPlayer>();
                    if(l_component == null)
                    {
                        l_component = l_remotePlayer.gameObject.AddComponent<LifterPlayer>();
                        l_component.AddLifted(m_localLiftedPlayer);
                    }
                }
            }
        }

        void OnUnfriended(string p_id)
        {
            if(m_update && (m_localLiftedPlayer != null) && Settings.AllowFriends)
            {
                Player l_player = Utils.GetPlayerWithId(p_id);
                if(l_player != null)
                {
                    LifterPlayer l_component = l_player.GetComponent<LifterPlayer>();
                    if(l_component != null)
                        Object.Destroy(l_component);
                }
            }
        }

        void OnManipulationToggle(bool p_state)
        {
            if(m_update && (m_localLiftedPlayer != null))
            {
                Player l_remotePlayer = Utils.GetPlayerQM();
                if(l_remotePlayer != null)
                {
                    LifterPlayer l_component = l_remotePlayer.GetComponent<LifterPlayer>();
                    if((l_component == null) && p_state)
                    {
                        l_component = l_remotePlayer.gameObject.AddComponent<LifterPlayer>();
                        l_component.AddLifted(m_localLiftedPlayer);
                    }
                    else if((l_component != null) && !p_state)
                    {
                        l_component.RemoveLifted(m_localLiftedPlayer);
                        Object.Destroy(l_component);
                    }
                }
            }
        }

        void OnMenuShow()
        {
            if(m_update && (m_menuSettings != null))
                ((ICustomShowableLayoutedMenu)m_menuSettings).Show();
        }

        void OnPoseReset()
        {
            if(m_update && (m_localLiftedPlayer != null) && Settings.SavePose)
                m_localLiftedPlayer.ClearSavedPose();
        }

        void OnDisallowAll()
        {
            if(m_update && (m_localLiftedPlayer != null))
            {
                var l_remotePlayers = Utils.GetPlayers();
                if(l_remotePlayers != null)
                {
                    foreach(Player l_remotePlayer in l_remotePlayers)
                    {
                        if(l_remotePlayer != null)
                        {
                            LifterPlayer l_component = l_remotePlayer.GetComponent<LifterPlayer>();
                            if(l_component != null)
                                Object.Destroy(l_component);
                        }
                    }
                }
            }
        }
        private void OnAllowAll()
		{
			if (m_update && m_localLiftedPlayer != null)
			{
				Il2CppSystem.Collections.Generic.List<Player> players = Utils.GetPlayers();
				if (players != null)
				{
					foreach (Player player in players)
					{
						if (player != null)
						{
							player.gameObject.AddComponent<LifterPlayer>().AddLifted(m_localLiftedPlayer);
						}
					}
				}
			}
		}

		private void OnAllowFriends()
		{
			OnDisallowAll();
			foreach (Player player in Utils.GetFriendsInInstance())
			{
				player.gameObject.AddComponent<LifterPlayer>().AddLifted(m_localLiftedPlayer);
			}
		}
        void OnMenuClose()
        {
            if(m_update && (m_menuSettings != null))
                ((ICustomShowableLayoutedMenu)m_menuSettings).Hide();
        }
    }
}

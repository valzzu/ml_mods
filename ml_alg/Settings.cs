using MelonLoader;

namespace ml_alg
{
    static class Settings
    {
        static MelonPreferences_Entry<bool> ms_friendsEntry = null;

        static bool ms_settingsUpdated = false;
        static bool ms_setingsUpdatedFriends = false; // Separated because components aren't that cheap

        static float ms_grabDistance = 0.25f;
        static bool ms_allowFriends = false;
        static bool ms_savePose = false;
        static bool ms_useVelocity = false;
        static float ms_velocityMultiplier = 5f;
        static bool ms_useAverageVelocity = true;
        static bool ms_allowPull = true;
        static bool ms_allowHeadPull = true;
        static bool ms_allowHandsPull = true;
        static bool ms_allowHipsPull = true;
        static bool ms_allowLegsPull = true;
        static bool ms_distanceScale = true;

		public static void LoadSettings()
		{
			MelonPreferences.CreateCategory("ALG", "Avatar Limbs Grabber");
			MelonPreferences.CreateEntry<float>("ALG", "GrabRadius", ms_grabDistance, "Maximal distance to limbs", null, false, false, null).OnValueChanged += OnAnyEntryUpdate<float>;
			
            ms_friendsEntry = MelonPreferences.CreateEntry<bool>("ALG", "AllowFriends", ms_allowFriends, "Allow Everyone to manipulate you", null, false, false, null);
			ms_friendsEntry.OnValueChanged += OnAnyEntryUpdate<bool>;
			ms_friendsEntry.OnValueChanged += OnFriendsEntryUpdate;
            
			MelonPreferences.CreateEntry<bool>("ALG", "AllowPull", ms_allowPull, "Allow pull", null, false, false, null).OnValueChanged += OnAnyEntryUpdate<bool>;
			MelonPreferences.CreateEntry<bool>("ALG", "AllowHeadPull", ms_allowHeadPull, "Allow head pull", null, false, false, null).OnValueChanged += OnAnyEntryUpdate<bool>;
			MelonPreferences.CreateEntry<bool>("ALG", "AllowHandsPull", ms_allowHandsPull, "Allow hands pull", null, false, false, null).OnValueChanged += OnAnyEntryUpdate<bool>;
			MelonPreferences.CreateEntry<bool>("ALG", "AllowHipsPull", ms_allowHipsPull, "Allow hips pull", null, false, false, null).OnValueChanged += OnAnyEntryUpdate<bool>;
			MelonPreferences.CreateEntry<bool>("ALG", "AllowLegsPull", ms_allowLegsPull, "Allow legs pull", null, false, false, null).OnValueChanged += OnAnyEntryUpdate<bool>;
			MelonPreferences.CreateEntry<bool>("ALG", "SavePose", ms_savePose, "Preserve manipulated pose", null, false, false, null).OnValueChanged += OnAnyEntryUpdate<bool>;
			MelonPreferences.CreateEntry<bool>("ALG", "Velocity", ms_useVelocity, "Apply velocity on pull", null, false, false, null).OnValueChanged += OnAnyEntryUpdate<bool>;
			MelonPreferences.CreateEntry<float>("ALG", "VelocityMultiplier", ms_velocityMultiplier, "Velocity multiplier (0-100)", null, false, false, null).OnValueChanged += OnAnyEntryUpdate<float>;
			MelonPreferences.CreateEntry<bool>("ALG", "AverageVelocity", ms_useAverageVelocity, "Use average velocity", null, false, false, null).OnValueChanged += OnAnyEntryUpdate<bool>;
			MelonPreferences.CreateEntry<bool>("ALG", "DistanceScale", ms_distanceScale, "Use avatar scale for grabbing", null, false, false, null).OnValueChanged += OnAnyEntryUpdate<bool>;
			
            ReloadSettings();
		}

        public static void ReloadSettings()
        {
            ms_grabDistance = MelonPreferences.GetEntryValue<float>("ALG", "GrabRadius");
            ms_allowFriends = ms_friendsEntry.Value;
            ms_allowPull = MelonPreferences.GetEntryValue<bool>("ALG", "AllowPull");
            ms_allowHeadPull = MelonPreferences.GetEntryValue<bool>("ALG", "AllowHeadPull");
            ms_allowHandsPull = MelonPreferences.GetEntryValue<bool>("ALG", "AllowHandsPull");
            ms_allowHipsPull = MelonPreferences.GetEntryValue<bool>("ALG", "AllowHipsPull");
            ms_allowLegsPull = MelonPreferences.GetEntryValue<bool>("ALG", "AllowLegsPull");
            ms_savePose = MelonPreferences.GetEntryValue<bool>("ALG", "SavePose");
            ms_useVelocity = MelonPreferences.GetEntryValue<bool>("ALG", "Velocity");
            ms_velocityMultiplier = UnityEngine.Mathf.Clamp(MelonLoader.MelonPreferences.GetEntryValue<float>("ALG", "VelocityMultiplier"), 0f, 100f);
            MelonPreferences.SetEntryValue("ALG", "VelocityMultiplier", ms_velocityMultiplier);
            ms_useAverageVelocity = MelonPreferences.GetEntryValue<bool>("ALG", "AverageVelocity");
            ms_distanceScale = MelonPreferences.GetEntryValue<bool>("ALG", "DistanceScale");
        }

        static void OnAnyEntryUpdate<T>(T p_oldValue, T p_newValue) => ms_settingsUpdated = true;
        public static bool IsAnyEntryUpdated()
        {
            bool l_result = ms_settingsUpdated;
            ms_settingsUpdated = false;
            return l_result;
        }

        static void OnFriendsEntryUpdate(bool p_oldValue, bool p_newValue) => ms_setingsUpdatedFriends = true;
        public static bool IsFriendsEntryUpdated()
        {
            bool l_result = ms_setingsUpdatedFriends;
            ms_setingsUpdatedFriends = false;
            return l_result;
        }

        public static float GrabDistance
        {
            get => ms_grabDistance;
        }
        public static bool AllowFriends
        {
            get => ms_allowFriends;
        }
        public static bool AllowPull
        {
            get => ms_allowPull;
        }
        public static bool AllowHeadPull
        {
            get => ms_allowHeadPull;
        }
        public static bool AllowHandsPull
        {
            get => ms_allowHandsPull;
        }
        public static bool AllowHipsPull
        {
            get => ms_allowHipsPull;
        }
        public static bool AllowLegsPull
        {
            get => ms_allowLegsPull;
        }
        public static bool SavePose
        {
            get => ms_savePose;
        }
        public static bool UseVelocity
        {
            get => ms_useVelocity;
        }
        public static float VelocityMultiplier
        {
            get => ms_velocityMultiplier;
        }
        public static bool UseAverageVelocity
        {
            get => ms_useAverageVelocity;
        }
        public static bool DistanceScale
        {
            get => ms_distanceScale;
        }
    }
}

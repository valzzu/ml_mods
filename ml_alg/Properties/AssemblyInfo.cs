﻿using System.Reflection;

[assembly: AssemblyTitle("AvatarLimbsGrabber")]
[assembly: AssemblyVersion("1.1.9")]
[assembly: AssemblyFileVersion("1.1.9")]

[assembly: MelonLoader.MelonInfo(typeof(ml_alg.Main), "AvatarLimbsGrabber", "1.1.9", "SDraw", "https://github.com/SDraw/ml_mods")]
[assembly: MelonLoader.MelonGame("VRChat", "VRChat")]
[assembly: MelonLoader.MelonAdditionalDependencies("UIExpansionKit", "VRChatUtilityKit")]
[assembly: MelonLoader.MelonOptionalDependencies("IKTweaks")]
[assembly: MelonLoader.MelonPlatform(MelonLoader.MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonLoader.MelonPlatformDomain(MelonLoader.MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]

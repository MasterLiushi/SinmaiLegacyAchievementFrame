using MelonLoader;
using SinmaiLegacyAchievementFrame;

[assembly: MelonInfo(typeof(LegacyAchievementFrameMod), "SinmaiLegacyAchievementFrame", "1.0.0", "violetc")]
[assembly: MelonGame("sega-interactive", "Sinmai")]

namespace SinmaiLegacyAchievementFrame;

public class LegacyAchievementFrameMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        LoggerInstance.Msg("LegacyAchievementFrameMod Initialized.");
    }
}
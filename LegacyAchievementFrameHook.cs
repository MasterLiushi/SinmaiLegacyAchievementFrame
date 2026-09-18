using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using Timeline;
using UnityEngine;
using UnityEngine.UI;

namespace SinmaiLegacyAchievementFrame;

[HarmonyPatch]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class LegacyAchievementFrameHook
{
    private const string AchievementPanelName = "UI_Achivement";
    private const string AchievementRootName = "UI_MusicAchevement";

    private static readonly HashSet<int> Applied = [];
    private static readonly HashSet<int> Diagnosed = [];

    private static readonly FieldInfo FontDatasField = AccessTools.Field(typeof(AchievementCounterObject), "_fontDatas");

    private static readonly FieldInfo ParsentImageField = AccessTools.Field(typeof(AchievementCounterObject), "_parsentImage");

    private static readonly HashSet<string> SheetReports = new(StringComparer.Ordinal);

    private static readonly float[] IntegerScales = [0.9f, 0.9f, 0.9f, 0.8f];
    private static readonly float[] IntegerRelX = [55f, 18f, -20f, -77f];
    private static readonly float[] Decimal12Scales = [0.9f, 0.9f];
    private static readonly float[] Decimal12RelX = [0f, -40f];
    private static readonly float[] Decimal34Scales = [0.9f, 0.9f];
    private static readonly float[] Decimal34RelX = [0f, -10f];

    [HarmonyPatch(typeof(AchievementCounterObject), "UpdateScore")]
    [HarmonyPrefix]
    private static void UpdateScorePrefix(AchievementCounterObject __instance)
    {
        ClearNewVersionFontData(__instance);
        Apply(__instance);
    }

    [HarmonyPatch(typeof(AchievementCounterObject), "ChangeCounter")]
    [HarmonyPostfix]
    private static void ChangeCounterPostfix(AchievementCounterObject __instance)
    {
        ApplyPercentColor(__instance);
    }

    private static void ClearNewVersionFontData(AchievementCounterObject instance)
    {
        if (FontDatasField?.GetValue(instance) is not Array fontDatas || fontDatas.Length < 4)
        {
            return;
        }

        if (fontDatas.GetValue(3) != null)
        {
            fontDatas.SetValue(null, 3);
        }
    }

    [HarmonyPatch(typeof(CommonPrefab), "GetAchieveIntSprites")]
    [HarmonyPostfix]
    private static void GetAchieveIntSpritesPostfix(int difficulty, ref Sprite[] __result)
    {
        if (LegacyAchievementAssets.TryRebuildSheet(__result, true, difficulty, out var rebuilt))
        {
            __result = rebuilt;
            ReportSheet("int", difficulty, true);
        }
        else
        {
            ReportSheet("int", difficulty, false);
        }
    }

    [HarmonyPatch(typeof(CommonPrefab), "GetAchieveDecimalSprites")]
    [HarmonyPostfix]
    private static void GetAchieveDecimalSpritesPostfix(int difficulty, ref Sprite[] __result)
    {
        if (LegacyAchievementAssets.TryRebuildSheet(__result, false, difficulty, out var rebuilt))
        {
            __result = rebuilt;
            ReportSheet("decimal", difficulty, true);
        }
        else
        {
            ReportSheet("decimal", difficulty, false);
        }
    }

    private static void ReportSheet(string kind, int state, bool ok)
    {
        var key = kind + state + (ok ? ":ok" : ":fail");
        if (SheetReports.Add(key))
        {
            MelonLogger.Msg($"[LegacyAchievement] achieve {kind} sheet state={state} rebuilt={ok}");
        }
    }

    private static void ApplyPercentColor(AchievementCounterObject instance)
    {
        if (instance == null)
        {
            return;
        }

        var image = FindPercentImage(instance);
        var sprite = FindGamePercentSprite(instance);
        if (image != null && sprite != null && image.sprite != sprite)
        {
            image.sprite = sprite;
        }
    }

    private static Image FindPercentImage(AchievementCounterObject instance)
    {
        var per = instance.transform.Find("Per");
        return per != null ? per.GetComponent<Image>() : null;
    }

    private static Sprite FindGamePercentSprite(AchievementCounterObject instance)
    {
        if (ParsentImageField?.GetValue(instance) is Image source && source.sprite != null)
        {
            return source.sprite;
        }

        var legacy = instance.transform.Find("IMG_Parsent");
        if (legacy != null)
        {
            var image = legacy.GetComponent<Image>();
            if (image != null && image.sprite != null)
            {
                return image.sprite;
            }
        }

        return null;
    }

    private static void Apply(AchievementCounterObject instance)
    {
        if (instance == null)
        {
            return;
        }

        var counter = instance.transform;
        var player = counter.parent != null ? counter.parent.parent : null;
        var panel = player != null ? player.parent : null;
        var instanceId = instance.GetInstanceID();

        if (Diagnosed.Count > 256)
        {
            Diagnosed.Clear();
            Applied.Clear();
        }

        var matched = panel != null && NameIs(panel, AchievementPanelName) && panel.parent != null && NameIs(panel.parent, AchievementRootName);

        if (!matched)
        {
            if (Diagnosed.Add(instanceId))
            {
                MelonLogger.Msg($"[LegacyAchievement] chain not matched: {DescribeChain(counter)}");
            }

            return;
        }

        if (Diagnosed.Add(instanceId))
        {
            MelonLogger.Msg($"[LegacyAchievement] applying legacy layout: {DescribeChain(counter)}");
        }

        ReplaceLegacySprite(panel.GetComponent<Image>());
        ApplyLegacyPercentSign(counter, player);

        if (!Applied.Add(instanceId))
        {
            return;
        }

        SetRect(panel, 764f, 296f, 133f, -66.7f);
        SetRect(panel.Find("Lv"), 48f, 60f, 243f, 75f);
        SetRect(panel.Find("Modechange"), 116f, 42f, 160f, 68f);
        SetRect(panel.Find("Modechange_Utage"), 148f, 56f, -60f, 67f);
        SetRect(panel.Find("UI_Name"), 650f, 30f, -135.1f, 83.2f);

        if (NameIs(player, "UI_1P"))
        {
            SetRect(player, 100f, 100f, 0f, 0f);
            SetRect(player.Find("Box_01"), 515f, 96f, -258f, -75f, 0f);
            var text1 = player.Find("Text_01");
            SetText(text1, 128f, 20f, -182f, -16f);
            EnsureLabelBackground(player, "Text_Achievement_00", 145.79f, 24f, -256f, -16f, text1);
            SetFrames(player.Find("Null_UI_AchivementCounter/UI_AchivementCounter/UI_Number/NUM_Original_Integer"), IntegerScales, IntegerRelX);
            SetFrames(player.Find("Null_UI_AchivementCounter/UI_AchivementCounter/UI_Number/Num_Original_Integer12"), Decimal12Scales, Decimal12RelX);
            SetFrames(player.Find("Null_UI_AchivementCounter/UI_AchivementCounter/UI_Number/NUM_Original_Integer34"), Decimal34Scales, Decimal34RelX);
            SetRect(player.Find("Null_UI_AchivementCounter/UI_AchivementCounter/UI_Number/NUM_Original_Integer"), 400f, 126f, -200f, -6f);
            SetRect(player.Find("Null_UI_AchivementCounter/UI_AchivementCounter/UI_Number/Num_Original_Integer12"), 190f, 108f, -19f, -13f);
            SetRect(player.Find("Null_UI_AchivementCounter/UI_AchivementCounter/UI_Number/NUM_Original_Integer34"), 130f, 108f, 77f, -13f);
            return;
        }

        if (NameIs(player, "UI_2P"))
        {
            SetRect(player, 100f, 100f, 0f, 0f);
            SetRect(player.Find("Box_01"), 428f, 94f, -362f, -75f, 0f);
            SetRect(player.Find("NormalMode"), 100f, 100f, 0f, 0f);
            SetRect(player.Find("NormalMode/Box_02"), 170f, 94f, 70.47f, -75f, 0f);
            SetRect(player.Find("NormalMode/Box_03"), 107f, 94f, 240f, -75f, 0f);
            SetRect(player.Find("TournamentMode/Box_03"), 277f, 94f, 70f, -75f, 0f);
            SetRect(player.Find("TournamentMode/UI_Rank"), 300f, 270f, -518f, -12f);
            var text1 = player.Find("Text_01");
            SetText(text1, 128f, 20f, -290f, -16f);
            EnsureLabelBackground(player, "Text_Achievement", 144f, 24f, -362f, -16f, text1);

            var normal = player.Find("NormalMode");
            var text2 = player.Find("NormalMode/Text_02");
            SetText(text2, 128f, 20f, 132.5f, -16f);
            EnsureLabelBackground(normal, "Text_MaxSync", 117f, 24f, 74f, -16f, text2);

            var number = player.Find("Null_UI_Achievement/UI_AchivementCounter/UI_Number");
            SetRect(number, 130f, 70f, 103f, -5f);
            SetScale(number, 1.2f);
            SetFrames(player.Find("Null_UI_Achievement/UI_AchivementCounter/UI_Number/NUM_Original_Integer"), IntegerScales, IntegerRelX);
            SetFrames(player.Find("Null_UI_Achievement/UI_AchivementCounter/UI_Number/Num_Original_Integer12"), Decimal12Scales, Decimal12RelX);
            SetFrames(player.Find("Null_UI_Achievement/UI_AchivementCounter/UI_Number/NUM_Original_Integer34"), Decimal34Scales, Decimal34RelX);
            SetRect(player.Find("Null_UI_Achievement/UI_AchivementCounter/UI_Number/NUM_Original_Integer"), 400f, 126f, -200f, -6f);
            SetRect(player.Find("Null_UI_Achievement/UI_AchivementCounter/UI_Number/Num_Original_Integer12"), 190f, 108f, -19f, -13f);
            SetRect(player.Find("Null_UI_Achievement/UI_AchivementCounter/UI_Number/NUM_Original_Integer34"), 130f, 108f, 77f, -13f);
        }
    }

    private static void SetText(Transform target, float width, float height, float x, float y)
    {
        SetRect(target, width, height, x, y);
        if (target != null)
        {
            ReplaceLegacySprite(target.GetComponent<Image>());
        }
    }

    private static void EnsureLabelBackground(Transform parent, string name, float width, float height, float x, float y, Transform before)
    {
        if (parent == null)
        {
            return;
        }

        var target = parent.Find(name);
        if (target == null)
        {
            var created = new GameObject(name, typeof(RectTransform), typeof(Image));
            target = created.transform;
            target.SetParent(parent, false);
            if (before != null)
            {
                target.SetSiblingIndex(before.GetSiblingIndex());
            }
        }

        if (target is RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
            rect.localScale = Vector3.one;
        }

        var image = target.GetComponent<Image>();
        if (image == null)
        {
            image = target.gameObject.AddComponent<Image>();
        }

        if (LegacyAchievementAssets.TryGetSprite("UI_UPE_Box_02", out var sprite) && image.sprite != sprite)
        {
            image.sprite = sprite;
        }

        image.type = Image.Type.Sliced;
        image.raycastTarget = false;
        image.color = Color.white;
    }

    private static void SetRect(Transform target, float width, float height, float x, float y, float pivotX = 0.5f, float pivotY = 0.5f)
    {
        if (target is RectTransform rect)
        {
            rect.pivot = new Vector2(pivotX, pivotY);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
        }
    }

    private static void SetScale(Transform target, float scale)
    {
        if (target != null)
        {
            target.localScale = new Vector3(scale, scale, 1f);
        }
    }

    private static void SetFrames(Transform target, float[] scales, float[] relativeX)
    {
        if (target == null)
        {
            return;
        }

        var counter = target.GetComponent<SpriteCounter>();
        if (counter == null || counter.FrameList == null)
        {
            return;
        }

        var frames = counter.FrameList;
        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames[i];
            if (frame == null)
            {
                continue;
            }

            if (i < scales.Length)
            {
                frame.Scale = scales[i];
            }

            if (i < relativeX.Length)
            {
                var position = frame.RelativePosition;
                frame.RelativePosition = new Vector2(relativeX[i], position.y);
            }
        }

        counter.SetAllDirty();
    }

    private static void ApplyLegacyPercentSign(Transform counter, Transform player)
    {
        var per = counter.Find("Per");
        if (per == null)
        {
            return;
        }

        SetRect(per, 68f, 64f, NameIs(player, "UI_2P") ? 288f : 294f, -38f);
        SetScale(per, 1.2f);
    }

    private static void ReplaceLegacySprite(Image image)
    {
        if (image == null)
        {
            return;
        }

        var current = image.sprite;
        if (current == null || LegacyAchievementAssets.IsLegacySprite(current))
        {
            return;
        }

        var name = current.name;
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        if (LegacyAchievementAssets.TryGetSprite(name, out var legacy))
        {
            image.sprite = legacy;
        }
    }

    private static bool NameIs(Transform target, string expected)
    {
        if (target == null)
        {
            return false;
        }

        var name = target.name;
        if (string.Equals(name, expected, StringComparison.Ordinal))
        {
            return true;
        }

        return name.Length > expected.Length && name.StartsWith(expected, StringComparison.Ordinal) && name[expected.Length] == '(';
    }

    private static string DescribeChain(Transform target)
    {
        var parts = new List<string>();
        var current = target;
        for (var i = 0; i < 6 && current != null; i++)
        {
            parts.Add(current.name);
            current = current.parent;
        }

        return string.Join(" < ", parts);
    }
}
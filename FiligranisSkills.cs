using System.ComponentModel;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using MelonLoader.TinyJSON;
using ModData;
using UnityEngine;

namespace FiligranisSkills
{
    public class FiligranisSkills : MelonMod
	{
		public static FiligranisSkills Instance { get; private set;}
        public override void OnEarlyInitializeMelon()
        {
			Instance = this;
			
			customSkillOffset = -1;
			foreach (var st in Enum.GetValues<SkillType>())
			if ((int) st > customSkillOffset) customSkillOffset = (int) st;
        }

        public override void OnInitializeMelon()
        {

			uConsole.RegisterCommand("fskills_add_point", new Action(() => {
				var @params = uConsole.GetAllParameters();
				if (@params == null) return;
				if (@params.Count < 2)
				{
					uConsole.Log("[skillname, points]");
				}
				
				if (int.TryParse(@params[1], out int points))
					IncreaseSkillPoints(@params[0], points);
				else
				{
					uConsole.Log("points must be a number");
				}
			}));
            uConsole.RegisterCommand("fskills_set_point", new Action(() => {
				var @params = uConsole.GetAllParameters();
				if (@params == null) return;
				if (@params.Count < 2)
				{
                    uConsole.Log("[skillname, points]");
				}
				Skill skill = null;
				if (skills.TryGetValue(@params[0], out int index))
                    skill = GameManager.GetSkillsManager().m_Skills[customSkillOffset + index];
				else
				{
                    uConsole.Log("skill not found");
					return;
				}

				if (int.TryParse(@params[1], out int points))
                    skill.SetPoints(points, SkillsManager.PointAssignmentMode.AssignInAnyMode);
				else
				{
                    uConsole.Log("points must be a number");
				}
			}));
        }

        internal ModDataManager ModSave { get; private set; } = new ModDataManager(nameof(FiligranisSkills));
		internal MelonLogger.Instance? Logger { get; private set; }
		internal static Dictionary<string, int> skills = new Dictionary<string, int>();
		internal static List<SkillDefinition> defs = new List<SkillDefinition>();
		internal static int customSkillOffset;

		public static bool IsRegistered (string skillName) => skills.ContainsKey(skillName) || defs.Any(d => d.Name == skillName);
		/// <summary>
		/// Get skill points and the corresponding tiers.
		/// </summary>
		/// <param name="points"></param>
		/// <param name="skillName"></param>
        public static (int points, int tier) GetPointsAndTiers(string skillName)
		{
			var skill = GameManager.GetSkillsManager().m_Skills[customSkillOffset + skills[skillName]];
            return (skill.GetPoints(), skill.GetCurrentTierNumber());
		}
		/// <summary>
		/// Increase skill points.
		/// </summary>
		/// <param name="points"></param>
		/// <param name="skillName"></param>
        public static void IncreaseSkillPoints(string skillName, int points)
        {
            SkillsManager skillsManager = GameManager.GetSkillsManager();
            var skill = skillsManager.m_Skills[customSkillOffset + skills[skillName]];
			Instance.Logger?.Error($"IncreaseCustomSkillPoints: {skillName} ({skill.GetPoints()}p) (t{skill.GetCurrentTierNumber()}) + {points}... { skill.GetTierPoints(0) }/{ skill.GetTierPoints(1) }/{ skill.GetTierPoints(2) }/{ skill.GetTierPoints(3) }/{ skill.GetTierPoints(4) }");
			Instance.Logger?.Error($"PreviousTier: {skill.GetCurrentTierNumber()}");
			// // Set the tier points again because somehow the array's fucked up and points to somewher else every now and then
			// for (int i = 0; i < skill.Def.TierThresholds.Length; i++) skill.m_TierPoints[i] = skill.Def.TierThresholds[i];
            int PreviousTier = skill.GetCurrentTierNumber();
            skill.IncrementPoints(points, SkillsManager.PointAssignmentMode.AssignOnlyInSandbox);
            int CurrentTier = skill.GetCurrentTierNumber();
			Instance.Logger?.Error($"CurrentTier: {skill.GetCurrentTierNumber()}");
            if (PreviousTier != CurrentTier)
            {
                GameManager.GetSkillNotify().MaybeShowLevelUp(skill.m_SkillIcon, skill.m_DisplayName, skillsManager.GetTierName(CurrentTier), CurrentTier+1);
            } else
            {
                GameManager.GetSkillNotify().MaybeShowPointIncrease(skill.m_SkillIcon);
            }
			Instance.Logger?.Error($"New tiers: {CurrentTier} points: {skill.GetPoints()} / {skill.GetTierPoints(CurrentTier)}");
        }

		/// <summary>
		/// Register a new custom skill.
		/// </summary>
		/// <param name="def"></param>
        public static void RegisterSkill(SkillDefinition def)
        {
			if (IsRegistered(def.Name))
			{
				MelonLogger.Error($"Some mod is trying to regsiter a skill {def.Name} which the name is already used, it won't be registered, please report to the mod authors.");
				return;
			}
			defs.Add(def);
        }
	}

	[HarmonyLib.HarmonyPatch(typeof(Panel_Log), nameof(Panel_Log.RefreshSelectedSkillDescriptionView))]
	public static class Panel_Log_Initialize
	{
		public static void Postfix(Panel_Log __instance)
		{
			SkillsManager skillsMgr = GameManager.GetSkillsManager();
			for (int i = 0; i < skillsMgr.m_Skills.Count; i++)
			{
                Skill skill = skillsMgr.m_Skills[i];
                FiligranisSkills.Instance.Logger?.Msg($"#{i} Skill: {skill?.name}");
					FiligranisSkills.Instance.Logger?.Error($"RefreshSelectedSkillDescriptionView: {skill.name} ({skill.GetPoints()}p) (t{skill.GetCurrentTierNumber()})... { skill.GetTierPoints(0) }/{ skill.GetTierPoints(1) }/{ skill.GetTierPoints(2) }/{ skill.GetTierPoints(3) }/{ skill.GetTierPoints(4) }");
				// if (skill is CustomSkill cs)
				// {
				// 	FiligranisSkills.Instance.Logger?.Msg($"CustomSkill {cs.Def.Name} {cs.Def.TierThresholds[0]}/{cs.Def.TierThresholds[1]}/{cs.Def.TierThresholds[2]}/{cs.Def.TierThresholds[3]}/{cs.Def.TierThresholds[4]}");
				// }
			}

			if (__instance.m_SkillImageLarge.mainTexture == null)
			{
                SkillListItem skillListItem = __instance.m_SkillsDisplayList[__instance.m_SkillListSelectedIndex];
                var skill = skillListItem.m_Skill;
				FiligranisSkills.Instance.Logger?.Msg($"RefreshSelectedSkillDescriptionView - replacing image for: {skill?.name}");
				if (skill == null) return;
				var sName = skill.name.Substring("Skill_".Length);
				FiligranisSkills.Instance.Logger?.Error($"RefreshSelectedSkillDescriptionView: {skill.name} ({skill.GetPoints()}p) (t{skill.GetCurrentTierNumber()})... { skill.GetTierPoints(0) }/{ skill.GetTierPoints(1) }/{ skill.GetTierPoints(2) }/{ skill.GetTierPoints(3) }/{ skill.GetTierPoints(4) }");
                Texture2D? image = FiligranisSkills.defs[FiligranisSkills.skills[sName]].Image;
                if (image != null) __instance.m_SkillImageLarge.mainTexture = image;
			}
		}
	}
	
    [HarmonyPatch(nameof(SkillsManager), "Awake")]
	public static class SkillsManagerInitialize
	{
		public static void Postfix(SkillsManager __instance)
		{
			FiligranisSkills.Instance.Logger?.Msg($"FSkill: SkillsManager.Awake---");
			FiligranisSkills.skills.Clear();
			for (int i = 0; i < FiligranisSkills.defs.Count; i++)
			{
				var def = FiligranisSkills.defs[i];
				GameObject GO = new GameObject();
				GO.name = $"Skill_{def.Name}";
				Skill_IceFishing skill = GO.AddComponent<Skill_IceFishing>();
				skill.m_LocalizedDisplayName = def.DisplayNameLocalized;
				skill.m_SkillIcon = def.IconId;
				skill.m_SkillIconBackground = def.IconBackgroundId;
				skill.m_SkillImage = def.ImageId;
				skill.m_SkillType = SkillType.None;
				skill.m_TierLocalizedDescriptions = def.TiersDescriptionLocalized;
				skill.m_TierLocalizedBenefits = def.TiersBenefitsLocalized;
				skill.m_TierPoints = def.TierThresholds;

				FiligranisSkills.skills[def.Name] = i;
				__instance.m_Skills.Add(skill);
				FiligranisSkills.Instance.Logger?.Msg($"Registered CustomSkill#{__instance.m_Skills.Count - 1}: {def.Name} {def.TierThresholds[0]}/{def.TierThresholds[1]}/{def.TierThresholds[2]}/{def.TierThresholds[3]}/{def.TierThresholds[4]}");
			}
		}
	}

    [HarmonyPatch(nameof(SaveGameSystem), nameof(SaveGameSystem.RestoreGlobalData))]
	internal static class RestoreGlobalData
	{
    	[HarmonyPriority(Priority.High)]
		internal static void Postfix (string name)
		{
			FiligranisSkills.Instance.Logger?.Msg($"FSkill: SaveGameSystem.RestoreGlobalData---");
			SkillsManager skillsMgr = GameManager.GetSkillsManager();
			for (int i = FiligranisSkills.customSkillOffset; i < skillsMgr.m_Skills.Count; i++)
			{
				var skill = skillsMgr.m_Skills[i];
				var sName = skill.name.Substring("Skill_".Length);
				var saved = FiligranisSkills.Instance.ModSave.Load($"skillpoint_{sName}");
				if (saved == null) continue;
				// // Set the tier points again because somehow the array's fucked up and points to somewher else every now and then
				// for (int j = 0; j < skill.Def.TierThresholds.Length; j++) skill.m_TierPoints[j] = skill.Def.TierThresholds[j];
				if (int.TryParse(saved, out int points))
                	skill.SetPoints(points, SkillsManager.PointAssignmentMode.AssignInAnyMode);
            }

			for (int i = 0; i < skillsMgr.m_Skills.Count; i++)
			{
                Skill skill = skillsMgr.m_Skills[i];
                FiligranisSkills.Instance.Logger?.Msg($"#{i} Skill: {skill?.name}");
				FiligranisSkills.Instance.Logger?.Error($"IncreaseCustomSkillPoints: {skill.name} ({skill.GetPoints()}p) (t{skill.GetCurrentTierNumber()})... { skill.GetTierPoints(0) }/{ skill.GetTierPoints(1) }/{ skill.GetTierPoints(2) }/{ skill.GetTierPoints(3) }/{ skill.GetTierPoints(4) }");
				// if (skill is CustomSkill cs) FiligranisSkills.Instance.Logger?.Msg($"CustomSkill {cs.Def.Name} {cs.Def.TierThresholds[0]}/{cs.Def.TierThresholds[1]}/{cs.Def.TierThresholds[2]}/{cs.Def.TierThresholds[3]}/{cs.Def.TierThresholds[4]}");
			}
		}
	}

    [HarmonyPatch(nameof(SaveGameSystem), nameof(SaveGameSystem.SaveGlobalData))]
	internal static class SaveGlobalData
	{
    	[HarmonyPriority(Priority.High)]
		internal static void Postfix (SlotData slot)
		{
			SkillsManager skillsMgr = GameManager.GetSkillsManager();
			FiligranisSkills.Instance.Logger?.Msg($"FSkill: SaveGameSystem.SaveGlobalData--- ({skillsMgr.m_Skills.Count} skills)");
			for (int i = FiligranisSkills.customSkillOffset; i < skillsMgr.m_Skills.Count; i++)
			{
				var skill = skillsMgr.m_Skills[i];
				var name = skill.name.Substring("Skill_".Length);
				FiligranisSkills.Instance.Logger?.Msg($"FSkill: Saving skill#{i} {name} {skill.GetPoints()}p");
				FiligranisSkills.Instance.ModSave.Save(skill.GetPoints().ToString(), $"skillpoint_{name}");
				// // Set the tier points again because somehow the array's fucked up and points to somewher else every now and then
				// for (int j = 0; j < skill.Def.TierThresholds.Length; j++) skill.m_TierPoints[j] = skill.Def.TierThresholds[j];
				FiligranisSkills.Instance.Logger?.Error($"CustomSkill: {skill.name} ({skill.GetPoints()}p) (t{skill.GetCurrentTierNumber()})... { skill.GetTierPoints(0) }/{ skill.GetTierPoints(1) }/{ skill.GetTierPoints(2) }/{ skill.GetTierPoints(3) }/{ skill.GetTierPoints(4) }");
				// if (skill is CustomSkill cs) FiligranisSkills.Instance.Logger?.Msg($"CustomSkill Def {cs.Def.Name} {cs.Def.TierThresholds[0]}/{cs.Def.TierThresholds[1]}/{cs.Def.TierThresholds[2]}/{cs.Def.TierThresholds[3]}/{cs.Def.TierThresholds[4]}");
			}
		}
	}

    [HarmonyPatch(nameof(GameManager), nameof(GameManager.HandlePlayerDeath))] // ModData workaround
	internal static class HandlePlayerDeath
	{
		internal static void Postfix ()
		{
			SkillsManager skillsMgr = GameManager.GetSkillsManager();
			for (int i = FiligranisSkills.customSkillOffset; i < skillsMgr.m_Skills.Count; i++)
			{
				var skill = skillsMgr.m_Skills[i];
				var name = skill.name.Substring("Skill_".Length);
				FiligranisSkills.Instance.ModSave.Save("", $"skillpoint_{name}");
			}
		}
	}

	public class ReadOnlySkillHandle
	{
		Skill skill;
		public int Tier => skill.GetCurrentTierNumber();
		public int Points => skill.GetPoints();
	}


    public class SkillDefinition
    {
		/// <summary>
		/// Suggestion: use a qualified name to prevent conflicts, for exmaple: [modname.skill]
		/// </summary>
        public string Name { get; set; }

        public LocalizedString DisplayNameLocalized { get; set; }

		/// <summary>
		/// The Id of the resource to load as the skill icon.
		/// </summary>
        public string IconId { get; set; }

		/// <summary>
		/// The Id of the resource to load as the skill icon background.
		/// </summary>
        public string IconBackgroundId { get; set; }

		/// <summary>
		/// The Id of the resource to load as the skill description background image.
		/// </summary>
        public string ImageId { get; set; }

		/// <summary>
		/// Descriptions for each tier. The length is required to be 5.
		/// </summary>
        public LocalizedString[] TiersDescriptionLocalized { get; set; }

		/// <summary>
		/// Benefit descriptions for each tier. Noted that the game expect every tier comes with 1 addtional benefits and each line to be separated with `\n` (linebreak), so tier 3 need to be benefit1\nbenefit2\nbenefit3. The length is required to be 5.
		/// </summary>
        public LocalizedString[] TiersBenefitsLocalized { get; set; }

		/// <summary>
		/// The point threshold for each tier. Start with 0 because it's the level we start with. The length is required to be 5.
		/// </summary>
        public int[] TierThresholds { get; set; }

		/// <summary>
		/// The texture of the skill icon. Supply texture loaded from your bundle. Not required when using TLD resource.
		/// </summary>
        public Texture2D? Icon { get; set; }

		/// <summary>
		/// The texture of the skill icon background. Supply texture loaded from your bundle. Not required when using TLD resource.
		/// </summary>
        public Texture2D? IconBackground { get; set; }

		/// <summary>
		/// The texture of the skill description background image. Supply texture loaded from your bundle. Not required when using TLD resource.
		/// </summary>
        public Texture2D? Image { get; set; }
    }

	// DOES NOT WORK PROPERLY (Melon Loader bug)
    // [RegisterTypeInIl2Cpp]
	// public class CustomSkill : Skill
	// {
	// 	public CustomSkill(IntPtr ptr) : base(ptr) {}
	// 	public CustomSkill() : base(ClassInjector.DerivedConstructorPointer<CustomSkill>()) => ClassInjector.DerivedConstructorBody(this);

	// 	internal ISkillDefiniton Def { get; set; }
	// }
}

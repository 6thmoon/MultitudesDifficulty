extern alias Legacy;
using DifficultyAPI = R2API.DifficultyAPI;
using LegacyAPI = Legacy::R2API.DifficultyAPI;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using RoR2;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using UnityEngine;
using Resources = MultitudesDifficulty.Properties.Resources;

[assembly: AssemblyVersion(Local.Difficulty.Multitudes.Setup.versionNumber)]
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace Local.Difficulty.Multitudes
{
	[BepInPlugin("local.difficulty.multitudes", "MultitudesDifficulty", versionNumber)]
	public class Setup : BaseUnityPlugin
	{
		public const string versionNumber = "0.4.0";

		public static DifficultyIndex multitudesIndex = DifficultyIndex.Invalid;
		public static Color colorTheme;

		private static DifficultyDef multitudesDifficulty;

		public void Awake()
		{
			Settings.Load(Config);

			Color drizzle = ColorCatalog.GetColor(ColorCatalog.ColorIndex.EasyDifficulty);
			colorTheme = new Color(r: drizzle.r, g: drizzle.b, b: drizzle.g);

			multitudesDifficulty = new DifficultyDef(
					scalingValue: 
						DifficultyCatalog.GetDifficultyDef(DifficultyIndex.Hard).scalingValue,
					nameToken: "Multitudes",
					iconPath: null,
					descriptionToken: Settings.BuildDescription(verbose: true),
					color: colorTheme,
					serverTag: RoR2ServerTags.mod,
					countsAsHardMode: true
				);

			Texture2D texture = new Texture2D(0, 0);

			multitudesDifficulty.foundIconSprite = ImageConversion.LoadImage(
					texture, Session.eclipseMode ? Resources.eclipse : Resources.icon);
			multitudesDifficulty.iconSprite = Sprite.Create(
					texture, new Rect(0, 0, texture.width, texture.height),
					pivot: new Vector2(texture.width / 2, texture.height / 2)
				);

			if ( !Session.forceEnable )
				Harmony.CreateAndPatchAll(typeof(Setup));

			Run.onRunStartGlobal += Session.Begin;
			Run.onRunDestroyGlobal += Session.End;
		}

		private static RuleDef DifficultyRule => RuleCatalog.allRuleDefs.First();

		[HarmonyPatch(typeof(RuleCatalog), nameof(RuleCatalog.Init))]
		[HarmonyPostfix]
		private static void AddDifficulty()
		{
			RuleChoiceDef ruleChoice =
					DifficultyRule.AddChoice(multitudesDifficulty.nameToken);

			// Arbitrary value - should match on host/client, large enough to prevent conflicts.
			multitudesIndex = DifficultyIndex.Invalid + ( Session.eclipseMode ? 1 : -1 ) * 0x7F;

			ruleChoice.difficultyIndex = multitudesIndex;
			ruleChoice.tooltipNameToken = multitudesDifficulty.nameToken;
			ruleChoice.tooltipNameColor = multitudesDifficulty.color;
			ruleChoice.tooltipBodyToken = multitudesDifficulty.descriptionToken;
			ruleChoice.serverTag = multitudesDifficulty.serverTag;
			ruleChoice.sprite = multitudesDifficulty.iconSprite;
			ruleChoice.globalIndex = RuleCatalog.allChoicesDefs.Count;

			RuleCatalog.allChoicesDefs.Add(ruleChoice);
			RuleCatalog.ruleChoiceDefsByGlobalName[ruleChoice.globalName] = ruleChoice;

			if ( Chainloader.PluginInfos.ContainsKey(submodule) ) SupportAPI();
			else if ( Chainloader.PluginInfos.TryGetValue(r2api, out PluginInfo plugin)
					&& plugin.Metadata.Version <= Version.Parse(oldVersion)
				) LegacySupport();
		}

		private const string oldVersion = Legacy::R2API.R2API.PluginVersion,
				submodule = DifficultyAPI.PluginGUID, r2api = Legacy::R2API.R2API.PluginGUID;

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void SupportAPI()
				=> DifficultyAPI.difficultyDefinitions[multitudesIndex] = multitudesDifficulty;

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void LegacySupport()
		{
			if ( LegacyAPI.Loaded )
				LegacyAPI.difficultyDefinitions[multitudesIndex] = multitudesDifficulty;
		}

		[HarmonyPatch(typeof(DifficultyCatalog), nameof(DifficultyCatalog.GetDifficultyDef))]
		[HarmonyPrefix]
		private static bool GetDifficulty(DifficultyIndex difficultyIndex, 
				ref DifficultyDef __result)
		{
			if ( difficultyIndex == multitudesIndex )
			{
				__result = multitudesDifficulty;
				return false;		// Skip original method execution.
			}

			return true;
		}

		private static DifficultyIndex BaseDifficulty => Session.eclipseMode ?
				DifficultyIndex.Eclipse8 : DifficultyIndex.Hard;

		[HarmonyPatch(typeof(NetworkExtensions), nameof(NetworkExtensions.Write),
				new Type[] { typeof(UnityEngine.Networking.NetworkWriter), typeof(RuleBook) })]
		[HarmonyPrefix]
		private static void AdjustRuleBook(ref RuleBook src)
		{
			if ( src.FindDifficulty() == multitudesIndex )
			{
				RuleBook ruleBook = new RuleBook();
				ruleBook.Copy(src);

				ruleBook.ApplyChoice(
						DifficultyRule.FindChoice(BaseDifficulty.ToString())
					);
				src = ruleBook;
			}
		}

		[HarmonyPatch(typeof(Run), nameof(Run.OnSerialize))]
		[HarmonyPrefix]
		private static void SendBaseDifficulty(Run __instance, ref int __state)
		{
			__state = __instance.selectedDifficultyInternal;

			if ( __instance.selectedDifficulty == multitudesIndex )
				__instance.selectedDifficultyInternal = (int) BaseDifficulty;
		}

		[HarmonyPatch(typeof(Run), nameof(Run.OnSerialize))]
		[HarmonyPostfix]
		private static void RestoreDifficulty(Run __instance, int __state)
				=> __instance.selectedDifficultyInternal = __state;
	}
}

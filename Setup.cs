extern alias Legacy;
global using HarmonyLib;
global using RoR2;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using UnityEngine;
using BepInEx;
using BepInEx.Bootstrap;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using UnityEngine.SceneManagement;
using DifficultyAPI = R2API.DifficultyAPI;
using LegacyAPI = Legacy::R2API.DifficultyAPI;
using Resources = MultitudesDifficulty.Properties.Resources;

[assembly: AssemblyVersion(Local.Difficulty.Multitudes.Setup.version)]
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace Local.Difficulty.Multitudes;

[BepInPlugin(identifier, "MultitudesDifficulty", version)]
public class Setup : BaseUnityPlugin
{
	public const string identifier = "local.difficulty.multitudes";
	public const string version = "0.5.1";

	public static DifficultyIndex index;
	public static Color theme;

	private static DifficultyDef difficulty;
	private static RuleChoiceDef other, choice;

	public static bool eclipseMode, lobbyPlayerCount, forceEnable;

	public void Awake()
	{
		Settings.Load(Config, out eclipseMode);
		SceneManager.sceneUnloaded += _ =>
		{
			Settings.Load(Config);

			choice.tooltipBodyToken = difficulty.descriptionToken = Settings.BuildDescription();
			choice.excludeByDefault = forceEnable;
		};

		Color drizzle = ColorCatalog.GetColor(ColorCatalog.ColorIndex.EasyDifficulty);
		theme = new Color(r: drizzle.r, g: drizzle.b, b: drizzle.g);

		index = (DifficultyIndex)( eclipseMode ? sbyte.MaxValue : sbyte.MinValue );
		difficulty = new DifficultyDef(
				DifficultyCatalog.GetDifficultyDef(DifficultyIndex.Hard).scalingValue,
				nameToken: "Multitudes", iconPath: null, descriptionToken: null,
				color: theme, serverTag: RoR2ServerTags.mod, countsAsHardMode: true
			);

		const int size = 256, scale = 2;
		Texture2D texture = new(size, size, TextureFormat.ARGB32, scale + 1, linear: false);

		difficulty.foundIconSprite = ImageConversion.LoadImage(
				texture, eclipseMode ? Resources.eclipse : Resources.icon);
		difficulty.iconSprite = Sprite.Create(
				texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);

		Harmony.CreateAndPatchAll(typeof(Setup));

		Run.onRunStartGlobal += Session.Begin;
		Run.onRunDestroyGlobal += Session.End;
	}

	[HarmonyPatch(typeof(Run), nameof(Run.participatingPlayerCount), MethodType.Getter)]
	[HarmonyPrefix]
	public static bool GetPlayerCount(out int __result)
	{
		var players = PlayerCharacterMasterController.instances;
		__result = lobbyPlayerCount ? players.Count :
				players.Where( player => player.isConnected ).Count();

		return false;
	}

	[HarmonyPatch(typeof(RuleCatalog), nameof(RuleCatalog.Init))]
	[HarmonyPostfix]
	private static void AddDifficulty()
	{
		RuleDef difficulties = RuleCatalog.allRuleDefs.First();
		choice = difficulties.AddChoice(difficulty.nameToken);

		choice.difficultyIndex = index;
		choice.tooltipNameToken = difficulty.nameToken;
		choice.tooltipNameColor = difficulty.color;
		choice.serverTag = difficulty.serverTag;
		choice.sprite = difficulty.iconSprite;
		choice.globalIndex = RuleCatalog.allChoicesDefs.Count;

		RuleCatalog.allChoicesDefs.Add(choice);
		RuleCatalog.ruleChoiceDefsByGlobalName[choice.globalName] = choice;

		foreach ( RuleChoiceDef definition in difficulties.choices )
			if ( definition.difficultyIndex == ( eclipseMode ?
					DifficultyIndex.Eclipse8 : DifficultyIndex.Hard ))
			{
				other = definition;
				break;
			}

		CheckCompatibility(Chainloader.PluginInfos);
	}

	private static void CheckCompatibility(Dictionary<string, PluginInfo> info)
	{
		const string version = Legacy::R2API.R2API.PluginVersion,
				api = Legacy::R2API.R2API.PluginGUID, module = DifficultyAPI.PluginGUID;

		if ( info.ContainsKey(module) ) SupportAPI();
		else if ( info.TryGetValue(api, out PluginInfo plugin) &&
				plugin.Metadata.Version <= Version.Parse(version)
			) LegacySupport();

		[MethodImpl(MethodImplOptions.NoInlining)]
		static void SupportAPI()
				=> DifficultyAPI.difficultyDefinitions[index] = difficulty;

		[MethodImpl(MethodImplOptions.NoInlining)]
		static void LegacySupport()
		{
			if ( LegacyAPI.Loaded )
				LegacyAPI.difficultyDefinitions[index] = difficulty;
		}
	}

	[HarmonyPatch(typeof(DifficultyCatalog), nameof(DifficultyCatalog.GetDifficultyDef))]
	[HarmonyPrefix]
	private static bool GetDifficulty(
			DifficultyIndex difficultyIndex, ref DifficultyDef __result)
	{
		if ( difficultyIndex == index )
		{
			__result = difficulty;
			return false;
		}

		return true;
	}

	[HarmonyPatch(typeof(NetworkExtensions), nameof(NetworkExtensions.Write),
			new Type[] { typeof(UnityEngine.Networking.NetworkWriter), typeof(RuleBook) })]
	[HarmonyPrefix]
	private static void AdjustRuleBook(ref RuleBook src)
	{
		if ( src.FindDifficulty() == index )
		{
			var ruleBook = new RuleBook();

			ruleBook.Copy(src);
			ruleBook.ApplyChoice(other);

			src = ruleBook;
		}
	}

	[HarmonyPatch(typeof(Run), nameof(Run.OnSerialize))]
	[HarmonyPrefix]
	private static void SendBaseIndex(Run __instance, ref int __state)
	{
		__state = __instance.selectedDifficultyInternal;

		if ( index == __instance.selectedDifficulty )
			__instance.selectedDifficultyInternal = (int) other.difficultyIndex;
	}

	[HarmonyPatch(typeof(Run), nameof(Run.OnSerialize))]
	[HarmonyPostfix]
	private static void RestoreIndex(Run __instance, int __state)
			=> __instance.selectedDifficultyInternal = __state;
}

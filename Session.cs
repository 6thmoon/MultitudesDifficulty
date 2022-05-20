using HarmonyLib;
using RoR2;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine.Networking;
using Acrid = EntityStates.Croco;
using Console = System.Console;

namespace Local.Difficulty.Multitudes
{
	public static class Session
	{
		public static byte additionalPlayers;
		public static bool eclipseMode;
		public static decimal interactableScale;
		public static bool extraRewards;
		public static decimal teleporterChargeRate;
		public static bool forceEnable;

		private static Harmony harmonyInstance = null;

		public static void Begin(Run thisRun)
		{
			if ( harmonyInstance is null && NetworkServer.active &&
					thisRun.selectedDifficulty == Setup.multitudesIndex | forceEnable )
			{
				harmonyInstance = Harmony.CreateAndPatchAll(typeof(Session));

				SceneDirector.onPrePopulateSceneServer += AdjustInteractableCredits;
				BossGroup.onBossGroupStartServer += AdjustBossRewards;
			}
		}

		public static void End(Run _)
		{
			SceneDirector.onPrePopulateSceneServer -= AdjustInteractableCredits;
			BossGroup.onBossGroupStartServer -= AdjustBossRewards;

			harmonyInstance?.UnpatchSelf();
			harmonyInstance = null;
		}

		[HarmonyPatch(typeof(SurvivorPodController), 
				nameof(SurvivorPodController.OnPassengerExit))]
		[HarmonyPatch(typeof(Acrid.WakeUp),	nameof(Acrid.WakeUp.OnExit))]	// Unique case.
		[HarmonyPostfix]
		private static void GreetUser()
		{
			void sendMessage(string message) =>
				Chat.SendBroadcastChat(new Chat.SimpleChatMessage {
						baseToken = "<color=#"
							+ UnityEngine.ColorUtility.ToHtmlStringRGB(Setup.colorTheme)
							+ $">" + message + "</color>"
					});

			if ( RoR2Application.isInMultiPlayer || forceEnable )
				sendMessage("Multitudes Enabled\n" + Setup.BuildDescription(flavorText: false));
			else if ( eclipseMode )
				sendMessage("Good luck.");
		}

		[HarmonyPatch(typeof(InfiniteTowerRun), nameof(InfiniteTowerRun.OnSafeWardActivated))]
		[HarmonyPostfix]
		private static void SimilacrumBegin(InfiniteTowerRun __instance)
		{
			if ( __instance.waveIndex <= 1 )
				GreetUser();
		}

		[HarmonyPatch(typeof(Run), nameof(Run.livingPlayerCount), MethodType.Getter)]
		[HarmonyPatch(typeof(Run), nameof(Run.participatingPlayerCount), MethodType.Getter)]
		[HarmonyPostfix]
		private static int AdjustPlayerCount(int realPlayerCount)
				=> realPlayerCount + additionalPlayers;

		public static int GetRealPlayerCount(int adjustedPlayerCount)
				=> adjustedPlayerCount - additionalPlayers;

//		[HarmonyPatch(typeof(SceneDirector), nameof(SceneDirector.PopulateScene))]
//		[HarmonyPrefix]
		private static void AdjustInteractableCredits(SceneDirector __instance)
		{
			int bonusCredits = 0;
			foreach ( ClassicStageInfo.BonusInteractibleCreditObject bonusObject
					in ClassicStageInfo.instance?.bonusInteractibleCreditObjects
							?? new ClassicStageInfo.BonusInteractibleCreditObject[0] )
			{
				if ( bonusObject.objectThatGrantsPointsIfEnabled?.activeSelf == true )
					bonusCredits += bonusObject.points;
			}

			decimal extraCredits = ( __instance.interactableCredit - bonusCredits ) *
					additionalPlayers / (decimal)( Run.instance.participatingPlayerCount + 1 );

			if ( "artifactworld" == SceneInfo.instance?.sceneDef?.baseSceneName
					&& !extraRewards )
				Console.WriteLine("Prevent extra interactables in artifact portal.");
			else extraCredits *= 1 - interactableScale;
			extraCredits = Math.Round(extraCredits, MidpointRounding.AwayFromZero);

			__instance.interactableCredit -= (int) extraCredits;
			Console.WriteLine($"...removed { extraCredits } credits.");

			Run.instance.RecalculateDifficultyCoefficent();     // Fix initial purchase cost.
		}

//		[HarmonyPatch(typeof(BossGroup), nameof(BossGroup.DropRewards))]
//		[HarmonyPrefix]
		private static void AdjustBossRewards(BossGroup __instance)
		{
			if ( !extraRewards )
			{
				int realPlayerCount = GetRealPlayerCount(Run.instance.participatingPlayerCount);
				int originalRewards = ( 1 + __instance.bonusRewardCount ) * 
						Run.instance.participatingPlayerCount;

				__instance.scaleRewardsByPlayerCount = false;

				// Increase rewards for multiplayer games...
				__instance.bonusRewardCount *= realPlayerCount;	// i.e. `Shrine of the Mountain`
				__instance.bonusRewardCount += realPlayerCount - 1;	// & base rewards.

				originalRewards -= 1 + __instance.bonusRewardCount;
				Console.WriteLine(
						$"Adjusted boss event to drop { originalRewards } less item(s).");
			}
		}

		[HarmonyPatch(typeof(HoldoutZoneController), nameof(HoldoutZoneController.OnEnable))]
		[HarmonyPrefix]
		private static void AdjustChargeRate(HoldoutZoneController __instance)
		{
			if ( __instance.chargingTeam == TeamIndex.Player )
			{
				int realPlayerCount = GetRealPlayerCount(Run.instance.participatingPlayerCount);

				decimal multiplier = realPlayerCount / 
						( realPlayerCount + teleporterChargeRate * additionalPlayers );
				__instance.calcChargeRate +=
						( ref float chargeRate ) => chargeRate *= (float) multiplier;

				Console.WriteLine("Charge rate reduced by " +
						 ( 1 - multiplier ).ToString("0.#%") + " for holdout zone.");
			}
		}

		private static readonly MethodInfo getRealPlayerCount =
				typeof(Session).GetMethod(nameof(GetRealPlayerCount));

		[HarmonyPatch(typeof(ArenaMissionController), nameof(ArenaMissionController.EndRound))]
		[HarmonyPatch(typeof(InfiniteTowerWaveController),
				nameof(InfiniteTowerWaveController.DropRewards))]
		[HarmonyTranspiler]		// Adjust rewards for `Void Fields` & `Simulacrum`.
		private static IEnumerable<CodeInstruction> IgnoreParticipatingPlayerAdjustment(
				IEnumerable<CodeInstruction> instructionList)
		{
			MethodInfo getParticipatingPlayerCount =
					typeof(Run).GetProperty(nameof(Run.participatingPlayerCount)).GetMethod;

			foreach ( CodeInstruction instruction in instructionList )
			{
				yield return instruction;

				if ( instruction.Calls(getParticipatingPlayerCount) 
						&& !extraRewards )
					yield return new CodeInstruction(OpCodes.Call, getRealPlayerCount);
			}
		}

		[HarmonyPatch(typeof(AllPlayersTrigger), nameof(AllPlayersTrigger.FixedUpdate))]
		[HarmonyPatch(typeof(MultiBodyTrigger), nameof(MultiBodyTrigger.FixedUpdate))]
		[HarmonyTranspiler]		// Ensure final boss event triggers on appropriate player count.
		private static IEnumerable<CodeInstruction> IgnoreLivingPlayerAdjustment(
				IEnumerable<CodeInstruction> instructionList)
		{
			MethodInfo getLivingPlayerCount =
					typeof(Run).GetProperty(nameof(Run.livingPlayerCount)).GetMethod;

			foreach ( CodeInstruction instruction in instructionList )
			{
				yield return instruction;

				if ( instruction.Calls(getLivingPlayerCount) )
					yield return new CodeInstruction(OpCodes.Call, getRealPlayerCount);
			}
		}
	}
}
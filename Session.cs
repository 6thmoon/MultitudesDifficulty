using HarmonyLib;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
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
		public static decimal incomePenalty;
		public static decimal bonusHealth;
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

		public static void End(object _)
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
				sendMessage(Setup.BuildDescription(verbose: false));
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

		[HarmonyPatch(typeof(Run), nameof(Run.participatingPlayerCount), MethodType.Getter)]
		[HarmonyPostfix]
		private static int AdjustPlayerCount(int playerCount)
				=> playerCount > 0 ? playerCount + additionalPlayers : playerCount;

		private static int RealPlayerCount => RoR2Application.isInMultiPlayer ?
				PlatformSystems.lobbyManager.calculatedTotalPlayerCount :
				PlayerCharacterMasterController.instances.Count;

//		[HarmonyPatch(typeof(SceneDirector), nameof(SceneDirector.PopulateScene))]
//		[HarmonyPrefix]
		private static void AdjustInteractableCredits(SceneDirector __instance)
		{
			int bonus = ClassicStageInfo.instance?.bonusInteractibleCreditObjects?.Where(
					obj => obj.objectThatGrantsPointsIfEnabled?.activeSelf is true
				).Sum( obj => obj.points ) ?? 0;

			decimal extraCredits = ( __instance.interactableCredit - bonus ) *
					additionalPlayers / (decimal)( Run.instance.participatingPlayerCount + 1 );

			SceneDef currentScene = SceneInfo.instance?.sceneDef;
			string sceneName = currentScene?.baseSceneName;
			bool hiddenRealms = sceneName == "arena" || sceneName == "voidstage" ||
					currentScene?.sceneType == SceneType.Intermission;

			if ( hiddenRealms && ! extraRewards )
				Console.WriteLine("Prevent extra interactables in hidden realms.");
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
				int originalRewards = ( 1 + __instance.bonusRewardCount ) * 
						Run.instance.participatingPlayerCount;

				__instance.scaleRewardsByPlayerCount = false;

				// Increase rewards for multiplayer games...
				__instance.bonusRewardCount *= RealPlayerCount;	// i.e. `Shrine of the Mountain`
				__instance.bonusRewardCount += RealPlayerCount - 1;	// & base rewards.

				originalRewards -= 1 + __instance.bonusRewardCount;
				Console.WriteLine(
						$"Adjusted boss event to drop { originalRewards } less item(s).");
			}
		}

		[HarmonyPatch(typeof(TeamManager), nameof(TeamManager.GiveTeamMoney))]
		[HarmonyPrefix]
		private static void AdjustPlayerIncome(ref uint money)
		{
			decimal extraIncome = money * additionalPlayers;
			extraIncome /= Run.instance.participatingPlayerCount;

			money -= (uint)( incomePenalty * extraIncome );
		}

		[HarmonyPatch(typeof(CombatDirector), nameof(CombatDirector.Spawn))]
		[HarmonyPatch(typeof(ScriptedCombatEncounter), nameof(ScriptedCombatEncounter.Spawn))]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> InsertHook(
				MethodBase __originalMethod, IEnumerable<CodeInstruction> instructionList)
		{
			MethodInfo directorSpawn =
					typeof(DirectorCore).GetMethod(nameof(DirectorCore.TrySpawnObject));

			foreach ( CodeInstruction instruction in instructionList )
			{
				if ( instruction.Calls(directorSpawn) && bonusHealth > 0 )
				{
					yield return new CodeInstruction(OpCodes.Dup);
					yield return new CodeInstruction(OpCodes.Ldarg_0);

					if ( typeof(CombatDirector) == __originalMethod.DeclaringType )
						yield return new CodeInstruction(OpCodes.Ldarg_2);
					else yield return new CodeInstruction(OpCodes.Ldnull);

					yield return CodeInstruction.Call(typeof(Session), nameof(AddBonusHealth));
				}

				yield return instruction;
			}
		}

		private static void AddBonusHealth(
				DirectorSpawnRequest request, object instance, EliteDef elite)
		{
			request.onSpawnedServer += ( SpawnCard.SpawnResult result ) =>
			{
				if ( result.success && result.spawnedInstance )
				{
					double health = 0;

					if ( instance is CombatDirector director )
					{
						if ( director.combatSquad?.grantBonusHealthInMultiplayer is true )
						{
							health = elite ? elite.healthBoostCoefficient : 1;
							health *= additionalPlayers;
						}
					}
					else
					{
						health = Run.instance.difficultyCoefficient * 2 / 5;
						health *= Math.Sqrt(Run.instance.livingPlayerCount + additionalPlayers)
								- Math.Sqrt(Run.instance.livingPlayerCount);
					}

					health *= (double) bonusHealth * 10;
					result.spawnedInstance.GetComponent<Inventory>().GiveItem(
							RoR2Content.Items.BoostHp, (int) Math.Round(health)
						);
				}
			};
		}

		[HarmonyPatch(typeof(HoldoutZoneController), nameof(HoldoutZoneController.OnEnable))]
		[HarmonyPrefix]
		private static void AdjustChargeRate(HoldoutZoneController __instance)
		{
			if ( __instance.chargingTeam == TeamIndex.Player &&
					__instance.playerCountScaling != 0 )
			{
				decimal multiplier = RealPlayerCount /
						( RealPlayerCount + teleporterChargeRate * additionalPlayers );
				__instance.calcChargeRate +=
						( ref float chargeRate ) => chargeRate *= (float) multiplier;

				Console.WriteLine("Charge rate reduced by " +
						 ( 1 - multiplier ).ToString("0.#%") + " for holdout zone.");
			}
		}

		[HarmonyPatch(typeof(EscapeSequenceController),
				nameof(EscapeSequenceController.BeginEscapeSequence))]
		[HarmonyPrefix]
		private static void IncreaseCountdown(EscapeSequenceController __instance)
		{
			__instance.countdownDuration *= (float)( 1 + 
					teleporterChargeRate * additionalPlayers );
		}

		[HarmonyPatch(typeof(ArenaMissionController), nameof(ArenaMissionController.EndRound))]
		[HarmonyPatch(typeof(InfiniteTowerWaveController),
				nameof(InfiniteTowerWaveController.DropRewards))]
		[HarmonyTranspiler]		// Adjust rewards for `Void Fields` & `Simulacrum`.
		private static IEnumerable<CodeInstruction> IgnorePlayerAdjustment(
				IEnumerable<CodeInstruction> instructionList)
		{
			MethodInfo getPlayerCount =
					typeof(Run).GetProperty(nameof(Run.participatingPlayerCount)).GetMethod;

			foreach ( CodeInstruction instruction in instructionList )
			{
				if ( instruction.Calls(getPlayerCount) && ! extraRewards )
				{
					yield return new CodeInstruction(OpCodes.Pop);
					yield return Transpilers.EmitDelegate<Func<int>>(( ) => RealPlayerCount );
				}
				else yield return instruction;
			}
		}
	}
}
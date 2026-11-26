# P19 source snapshot and integration checklist

These files were prepared outside Assets while P18 was validated. After the
accepted P18 checkpoint 439ef80, 25 sources were copied into Assets with the
required core hooks. Compilation and validation are now in progress; P19 is
not accepted. ACTIVATION_MANIFEST.json records that transfer.

The implementation under Assets is now authoritative and may contain fixes newer
than this source snapshot. Do not copy these files over it again without comparing
the changes. Existing component metas/GUIDs were preserved.
The runtime files named World* belong under Runtime/World; WorldMechanismTests.cs
belongs under Tests/EditMode. IEntityRestoreComplete belongs
under Runtime/Persistence; IEnemyRestorePresentation under Runtime/Rendering.
MissionAcousticsTests.cs also belongs under Tests/EditMode, never Runtime.

Original integration checklist (hooks applied; verify behavior in tests and play):

- EnemyBrain: expose SavePhaseRemaining and RestoreCombatPhase, preserving phase,
  windup/pain remaining time, and silent presentation restoration. Reset lastKnown
  to spawn. Preserve all current public methods for the old campaign.
- MissionContext: call IEntityRestoreComplete.FinishRestore after all ordered
  entity restores, including rollback, while Restoring is still true.
- Make MissionContext partial and add MissionContext.Restore.cs. Its coroutine
  applies the world, waits two navigation updates, restores actors/references,
  and rolls back on failure. Keep the synchronous method for EditMode/legacy use.
- EnemyModelView: implement IEnemyRestorePresentation and sample dead actors at
  the final pose; reset the rig before changing from a dead state to a living one.
  The candidate replacement here preserves the existing component's meta/GUID
  when copied over its source. It presents preparation during windup and reacts
  to actual attack counts, so its animation never decides when damage happens.
- MissionSnapshot/session: persist keys and weapon cooldown in addition to gear.
- WorldSaveController needs MissionSnapshot.keys/weaponCooldown and
  WeaponController.RemainingCooldown/RestoreCooldown. Its player restore/error
  handling and save UI must be validated during integration before acceptance.
- Generate explicit identities, serialized references, collision/nav obstacles,
  actions, objective flags and actor/collectible registries through Unity API.
- WorldBarrelState uses the existing silent RestoreBarrel path. WorldEncounter
  releases existing, initialized actors rather than spawning duplicate actors;
  pending encounter members must be physically behind a closed access.
- UkubolaEvolutionSlice.cs is an Editor partial source. The original scene
  generator declaration must also become partial. Extend the manifest's explicit
  owned scene/bake paths to OutpostSlice; keep ArtRoom protected. The initial
  slice source still needs independent lighting, audio, visual polish, route and
  physics verification; its mere generation cannot satisfy P19.
  Its candidate bake detaches the copied room's LightingDataAsset, uses Clear
  for runtime lighting data only, and writes separate scene lightmaps/probes.
  Never use ClearLightingDataAsset on the copied room: that API deletes linked
  assets. Verify the ArtRoom hashes after generation and baking. API references:
  https://docs.unity.com/en-us/engine/6000.6/script-reference/unityeditor/lightmapping/clear
  https://docs.unity.com/en-us/engine/6000.6/script-reference/unityeditor/lightmapping/setlightingdataassetforscene
- WorldAction.requiredFlags must implement AND conditions without consuming a
  one-shot binding while unmet. Repeated WorldRelay interactions must toggle
  rather than repeatedly request the already active state, so lift call buttons
  emit each press. Preserve single-use behavior.
  Add IWorldActionParameters under Runtime/Persistence and call its ValidateAction
  from ActionBinding.ValidateBinding for destinations implementing it, so an
  invalid lift stop is rejected during generation/startup rather than after use.
- WorldAudio initially reuses original Phase09 cues through a bounded 12-voice
  pool and MissionContext references. Technical playback and actual listening
  remain unverified. Replacing/refining these cues belongs to the later library
  and sound phases; no audio quality acceptance is implied by this source.
- Adjust the generated death pose before accepting corpse restoration: rotating
  the whole rig 85 degrees while translating it down .55 m hides the body below
  the floor. Raise the final rig offset to support the prone body and hide or
  detach the held rifle on death. Check living -> dead -> saved dead -> living
  visually and physically. Clip/main-asset name warnings are cosmetic; if changing
  names, first verify that the Idle/Walk/Prepare/Attack/Pain/Death aliases survive
  scene serialization, reimport, domain reload and a player build.
- For mechanism/save QA, suspend brains rather than disabling EnemyBrain: its
  health/death subscriptions must stay active so hp and phase remain consistent.
  Add a declared scenario seed and initialize combat/particle randomness before
  reproducible encounters. Run real CharacterController, collisions, attacks and
  interaction paths; do not treat direct state assignment as proof of gameplay.
- INoisePropagation belongs under Runtime/Enemies. NoiseEmitter.CanHear must
  prefer an attached INoisePropagation provider, preserving SectorNoise and the
  old distance fallback for the existing campaign. MissionAcoustics derives
  connectivity from mission portals and current door poses; it needs open/sealed,
  range and multi-floor/ring tests before activation can be accepted.
- WorldRuntimeQa belongs in an opt-in QA location. Wire its callback into the
  scenario/report after isolated initialization and actor suspension. It checks
  actual door/lift/hazard components and v2 restoration, then restores its initial
  world. Its direct lethal-damage assertion is an actor-persistence check, not
  proof that weapon aiming, cadence or a complete combat route works.

Required checks include partially moved/reversed doors, multi-stop lifts,
occupancy/ceiling protection, actual passenger transport, per-actor hazard timing,
electric discharge caps, restoration grace, silent world restore, no repeated
pickup/secret/exit events, and restoring enemy rivals after all health states.

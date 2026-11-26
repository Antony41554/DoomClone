using System;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ukubola.Combat;
using Ukubola.Persistence;
using Ukubola.World;

namespace Ukubola.Tests
{
    public sealed class WorldMechanismTests
    {
        private sealed class RecordedCommands:Ukubola.Player.IPlayerCommands
        {
            public bool Active=>true;
            public Vector2 Move=>Vector2.up;
            public Vector2 Look=>Vector2.zero;
            public bool Run=>false;
            public bool FireHeld=>true;
            public bool UsePressed=>true;
            public int WeaponSlot=>3;
        }
        private Scene scene;
        [SetUp] public void SetUp(){scene=EditorSceneManager.NewPreviewScene();}
        [TearDown] public void TearDown(){EditorSceneManager.ClosePreviewScene(scene);}
        private GameObject Object(string name)
        {var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);return go;}
        private WorldMover Mover(float closeAfter=0)
        {
            var root=Object("Lift");var panel=Object("Rigid Panel");panel.transform.SetParent(root.transform,false);var box=panel.AddComponent<BoxCollider>();box.size=new Vector3(3,.25f,3);
            var mover=root.AddComponent<WorldMover>();mover.ConfigureIdentity("test","lift");mover.Configure(panel.transform,box,new[]{Vector3.zero,Vector3.up*2,Vector3.up*4},1,false,true,closeAfter:closeAfter);return mover;
        }
        private WorldHazard Hazard(WorldHazardKind kind)
        {
            var root=Object(kind.ToString());var box=root.AddComponent<BoxCollider>();box.size=new Vector3(4,.5f,4);
            var hazard=root.AddComponent<WorldHazard>();hazard.ConfigureIdentity("test",kind.ToString());hazard.Configure(kind,box,null,null);return hazard;
        }
        private Health Actor(string label){var hp=Object(label).AddComponent<Health>();hp.Configure(100,null);return hp;}
        [Test] public void RecordedCommandsCannotTakeOverProductionInput()
        {
            Assert.That(QaEnvironment.Active,Is.False,"EditMode guard test must not run with a Play QA marker.");
            var input=Object("Input guard").AddComponent<Ukubola.Player.PlayerInputReader>();
            Assert.Throws<InvalidOperationException>(()=>input.SetQaCommands(new RecordedCommands()));
            Assert.That(input.HasQaCommands,Is.False);Assert.DoesNotThrow(()=>input.SetQaCommands(null));
        }
        [Test] public void MovingLiftRestoresItsPositionAndDestinationWithoutArrivalSignal()
        {
            var lift=Mover();lift.MoveTo(2);lift.Tick(1.25f);var saved=lift.CaptureEntity();
            lift.Tick(4);int arrivals=0;lift.Signalled+=s=>{if(s==WorldSignal.Arrived)arrivals++;};
            lift.RestoreEntity(saved);Assert.That(lift.Panel.localPosition.y,Is.EqualTo(1.25f).Within(.001));Assert.That(lift.TargetStop,Is.EqualTo(2));Assert.That(arrivals,Is.Zero);
            lift.Tick(2.75f);Assert.That(lift.Panel.localPosition.y,Is.EqualTo(4).Within(.001));Assert.That(arrivals,Is.EqualTo(1));
        }
        [Test] public void ReversingMidwayKeepsTheCurrentRigidPose()
        {
            var lift=Mover();lift.MoveTo(2);lift.Tick(1);lift.MoveTo(0);
            Assert.That(lift.Panel.localPosition.y,Is.EqualTo(1).Within(.001));var saved=lift.CaptureEntity();lift.Tick(.4f);lift.RestoreEntity(saved);lift.Tick(.5f);
            Assert.That(lift.Panel.localPosition.y,Is.EqualTo(.5f).Within(.001));Assert.That(lift.Panel.localScale,Is.EqualTo(Vector3.one));
        }
        [Test] public void RemovingPowerPausesTravelAndPreservesTheSnapshot()
        {
            var lift=Mover();lift.MoveTo(1);lift.Tick(.5f);lift.SetPower(false);var saved=lift.CaptureEntity();lift.Tick(5);
            Assert.That(lift.Panel.localPosition.y,Is.EqualTo(.5f).Within(.001));lift.SetPower(true);lift.Tick(1);lift.RestoreEntity(saved);
            Assert.That(lift.Powered,Is.False);Assert.That(lift.Panel.localPosition.y,Is.EqualTo(.5f).Within(.001));
        }
        [Test] public void ToxicExposureUsesIndependentActorCooldowns()
        {
            var hazard=Hazard(WorldHazardKind.Toxic);var a=Actor("player");var b=Actor("enemy");string id=PersistentBehaviour.StableId("test","enemy");
            Assert.That(hazard.ApplyToActor(a,"player"),Is.True);Assert.That(hazard.ApplyToActor(b,id),Is.True);Assert.That(hazard.ApplyToActor(a,"player"),Is.False);
            hazard.Tick(.5f);Assert.That(hazard.ApplyToActor(a,"player"),Is.False);hazard.Tick(.5f);Assert.That(hazard.ApplyToActor(a,"player"),Is.True);
            Assert.That(a.Current,Is.EqualTo(80));Assert.That(b.Current,Is.EqualTo(90));
        }
        [Test] public void DrainResumesFromItsSavedLevelAndCompletesOnce()
        {
            var hazard=Hazard(WorldHazardKind.Toxic);hazard.Execute(WorldVerb.Drain,0);hazard.Tick(1.5f);var saved=hazard.CaptureEntity();hazard.Tick(3);int arrivals=0;
            hazard.Signalled+=s=>{if(s==WorldSignal.Arrived)arrivals++;};hazard.RestoreEntity(saved);Assert.That(hazard.Level,Is.EqualTo(.625f).Within(.001));Assert.That(arrivals,Is.Zero);
            hazard.Tick(2.5f);Assert.That(hazard.Safe,Is.True);Assert.That(arrivals,Is.EqualTo(1));hazard.Execute(WorldVerb.Drain,0);hazard.Tick(4);Assert.That(arrivals,Is.EqualTo(1));
        }
        [Test] public void ElectricityCannotDamageTwiceInOneDischargeOrImmediatelyAfterRestore()
        {
            var hazard=Hazard(WorldHazardKind.Electricity);var hp=Actor("player");Assert.That(hazard.ApplyToActor(hp,"player"),Is.False);
            hazard.Tick(.75f);Assert.That(hazard.ApplyToActor(hp,"player"),Is.True);hazard.Tick(.2f);Assert.That(hazard.ApplyToActor(hp,"player"),Is.False);
            var saved=hazard.CaptureEntity();hazard.RestoreEntity(saved);Assert.That(hazard.Phase,Is.EqualTo(WorldHazardPhase.Warning));Assert.That(hazard.ApplyToActor(hp,"player"),Is.False);
            hazard.Tick(.75f);Assert.That(hazard.ApplyToActor(hp,"player"),Is.False);hazard.Tick(2.8f);
            Assert.That(hazard.ApplyToActor(hp,"player"),Is.True);Assert.That(hp.Current,Is.EqualTo(70));
        }
        [Test] public void InvulnerabilityProtectsAgainstEnvironmentalDamage()
        {
            var hazard=Hazard(WorldHazardKind.HotMetal);var hp=Actor("player");hp.gameObject.AddComponent<Powerup>().Grant(PowerupKind.Invulnerability,10);
            Assert.That(hazard.ApplyToActor(hp,"player"),Is.False);Assert.That(hp.Current,Is.EqualTo(100));
        }
        [Test] public void InvalidMoverSnapshotDoesNotMutateThePanel()
        {
            var lift=Mover();lift.MoveTo(1);lift.Tick(.25f);var saved=lift.CaptureEntity();saved.json="{\"from\":{\"x\":99,\"y\":0,\"z\":0},\"target\":1,\"fraction\":0.5,\"hold\":0,\"powered\":true}";
            Assert.Throws<InvalidOperationException>(()=>lift.RestoreEntity(saved));Assert.That(lift.Panel.localPosition.y,Is.EqualTo(.25f).Within(.001));
        }
        [Test] public void ActionStopParametersAreValidatedBeforeMovement()
        {
            var lift=Mover();Assert.Throws<InvalidOperationException>(()=>lift.ValidateAction(WorldVerb.MoveToStop,1.25f));
            Assert.Throws<InvalidOperationException>(()=>lift.Execute(WorldVerb.MoveToStop,3));
            Assert.That(lift.Panel.localPosition,Is.EqualTo(Vector3.zero));Assert.That(lift.Moving,Is.False);
        }
        [Test] public void SecretRestorationDoesNotReplayEvents()
        {
            var secret=Object("Secret").AddComponent<WorldSecret>();secret.ConfigureIdentity("test","secret");secret.Discover();var saved=secret.CaptureEntity();int events=0;secret.Discovered+=()=>events++;
            secret.RestoreEntity(saved);Assert.That(secret.Discover(),Is.False);Assert.That(events,Is.Zero);
        }
    }
}

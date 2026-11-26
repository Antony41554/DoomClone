using System;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ukubola.World;

namespace Ukubola.Tests
{
    public sealed class MissionAcousticsTests
    {
        private Scene scene;
        private MissionDefinition definition;
        private MissionContext context;
        [SetUp] public void SetUp()
        {
            scene=EditorSceneManager.NewPreviewScene();definition=ScriptableObject.CreateInstance<MissionDefinition>();
            definition.missionId="sound-test";definition.spawn=Vector3.zero;
            definition.sectors=new[]{new MissionSector{key="a",center=Vector2.zero,size=new Vector2(20,4)},new MissionSector{key="b",center=new Vector2(0,6),size=new Vector2(20,4)}};
            definition.portals=new[]{new MissionPortal{key="access",from="a",to="b",position=new Vector3(0,0,3)}};
            context=New("Context").AddComponent<MissionContext>();context.Configure(definition);
        }
        [TearDown] public void TearDown(){EditorSceneManager.ClosePreviewScene(scene);UnityEngine.Object.DestroyImmediate(definition);}
        private GameObject New(string label){var root=new GameObject(label);SceneManager.MoveGameObjectToScene(root,scene);return root;}
        private WorldMover Door()
        {
            var root=New("Door");var panel=New("Panel");panel.transform.SetParent(root.transform,false);var collision=panel.AddComponent<BoxCollider>();
            var door=root.AddComponent<WorldMover>();door.Configure(panel.transform,collision,new[]{Vector3.zero,Vector3.up*2},1,false,true);return door;
        }
        [Test] public void SealedAndMovingDoorsBlockSoundUntilFullyOpen()
        {
            var door=Door();var sound=New("Sound").AddComponent<MissionAcoustics>();sound.Configure(context,new[]{new AcousticLink{portal="access",door=door}});
            Assert.That(sound.CanReach(Vector3.zero,new Vector3(0,0,6),10),Is.False);
            door.MoveTo(1);door.Tick(.5f);Assert.That(sound.CanReach(Vector3.zero,new Vector3(0,0,6),10),Is.False);
            door.Tick(1.5f);Assert.That(sound.CanReach(Vector3.zero,new Vector3(0,0,6),10),Is.True);
            door.MoveTo(0);Assert.That(sound.CanReach(Vector3.zero,new Vector3(0,0,6),10),Is.False);
        }
        [Test] public void PortalRouteLengthLimitsSoundAroundCorners()
        {
            definition.portals[0].position=new Vector3(8,0,3);
            var sound=New("Sound").AddComponent<MissionAcoustics>();sound.Configure(context,Array.Empty<AcousticLink>());
            Assert.That(sound.CanReach(Vector3.zero,new Vector3(0,0,6),10),Is.False);
            Assert.That(sound.CanReach(Vector3.zero,new Vector3(0,0,6),18),Is.True);
        }
        [Test] public void DampeningReducesReachWithoutDependingOnPhysicsQueries()
        {
            var sound=New("Sound").AddComponent<MissionAcoustics>();sound.Configure(context,new[]{new AcousticLink{portal="access",dampened=true}});
            Assert.That(sound.CanReach(Vector3.zero,new Vector3(0,0,6),7),Is.False);
            Assert.That(sound.CanReach(Vector3.zero,new Vector3(0,0,6),10),Is.True);
        }
        [Test] public void UnknownPortalReferencesAreRejected()
        {
            var sound=New("Sound").AddComponent<MissionAcoustics>();
            Assert.Throws<InvalidOperationException>(()=>sound.Configure(context,new[]{new AcousticLink{portal="missing"}}));
        }
    }
}

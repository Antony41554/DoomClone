using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using Ukubola.Campaign;
using Ukubola.Combat;
using Ukubola.Persistence;
using Ukubola.UI;

namespace Ukubola
{
    [Serializable]
    public sealed class QaPose { public Vector3 position; public float yaw; }
    [Serializable]
    public sealed class QaScenario
    {
        public string mode = "capture";
        public string scene;
        public int warmupFrames = 60;
        public int sampleFrames = 300;
        public float[] yaw = { 0, 60, 120, 180, 240, 300 };
        public bool freezeActors;
        public QaPose[] poses;
        public int selectedWeapon=-1;
        public bool verifyArtRoom;
        public int quality=-1;
        public bool classicAspect;
        public int captureWidth, captureHeight;
        public bool verifyMission;
        public bool verifyRoute;
        public int seed=1600;
    }
    [Serializable]
    public sealed class QaRunResult
    {
        public bool success;
        public string mode, scene, error, device, unity, utc;
        public string captureMethod, timingKind;
        public string environment;
        public int frames;
        public int outputWidth,outputHeight,windowWidth,windowHeight,internalWidth,internalHeight,quality;
        public float averageMs, p95Ms, p99Ms;
        public long allocatedBytes;
        public string[] captures;
        public bool snapshotRoundTrip;
        public string[] behaviorChecks;
    }

}

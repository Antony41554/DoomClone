using System;
using UnityEditor;
using UnityEngine;
using Ukubola.Rendering;

namespace Ukubola.EditorTools
{
    public static class UkubolaRetroTests
    {
        [MenuItem("Tools/UKUBOLA/Phase 02/Run Retro Self-Tests", false, 33)]
        public static void Run()
        {
            if (!UkubolaRetroSetup.CanRun()) return;
            int count = 0;
            void Assert(bool value, string label)
            {
                if (!value) throw new InvalidOperationException(label);
                count++;
            }
            try
            {
                // Test centers, both sides of every sector, and periodicity.
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * 45f;
                    Assert(DirectionalSprite.IndexFromAngle(angle) == i, "center " + i);
                    Assert(DirectionalSprite.IndexFromAngle(angle - 22.4f) == i, "left " + i);
                    Assert(DirectionalSprite.IndexFromAngle(angle + 22.4f) == i, "right " + i);
                    Assert(DirectionalSprite.IndexFromAngle(angle + 22.6f) == (i + 1) % 8, "transition " + i);
                    Assert(DirectionalSprite.IndexFromAngle(angle - 360f) == i, "periodicity - " + i);
                    Assert(DirectionalSprite.IndexFromAngle(angle + 360f) == i, "periodicity + " + i);
                }
                foreach (float aspect in new[] { 4f/3f, 16f/9f, 16f/10f, 21f/9f, 1f, 9f/16f })
                {
                    Rect r = RetroDisplay.Fit(aspect, RetroDisplay.DisplayAspect);
                    Assert(Mathf.Abs(r.center.x - .5f) < .0001f && Mathf.Abs(r.center.y - .5f) < .0001f, "centered rect " + aspect);
                    Assert(r.xMin >= -.0001f && r.yMin >= -.0001f && r.xMax <= 1.0001f && r.yMax <= 1.0001f, "fits screen " + aspect);
                    Assert(Mathf.Abs(aspect * r.width / r.height - 4f/3f) < .0001f, "4:3 content " + aspect);
                    Assert(Mathf.Abs(r.width - 1f) < .0001f || Mathf.Abs(r.height - 1f) < .0001f, "maximal fit " + aspect);
                }
                for (int i = 0; i < 8; i++)
                {
                    Vector3 direction = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
                    Quaternion rot = Billboard.FacingRotation(direction + Vector3.up * 5f, Quaternion.identity);
                    Assert(Vector3.Dot(rot * Vector3.back, direction) > .9999f, "billboard facing " + i);
                    Assert(Vector3.Dot(rot * Vector3.up, Vector3.up) > .9999f, "billboard vertical " + i);
                    Quaternion actorRotation = Quaternion.Euler(0f, 137f, 0f);
                    Vector3 local = Quaternion.Inverse(actorRotation) * (actorRotation * direction);
                    Assert(DirectionalSprite.IndexFromAngle(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg) == i, "actor-relative angle " + i);
                }
                Quaternion fallback = Quaternion.Euler(0f, 40f, 0f);
                Assert(Quaternion.Angle(Billboard.FacingRotation(Vector3.up, fallback), fallback) < .01f, "coincident horizontal positions");
                Assert(RetroDisplay.Fit(0f, 4f/3f) == new Rect(0,0,1,1), "invalid aspect fallback");
                Debug.Log("[UKUBOLA P02] RETRO SELF-TESTS PASSED | " + count + " checks.");
            }
            catch (Exception ex) { Debug.LogError("[UKUBOLA P02] RETRO SELF-TESTS FAILED after " + count + " checks: " + ex.Message); }
        }
    }
}

using System;
using System.IO;
using Ukubola.Persistence;
using Ukubola.UI;
using UnityEngine;

namespace Ukubola.EditorTools
{
    public static class UkubolaEvolutionTests
    {
        public static string Run()
        {
            int count = 0;
            void Assert(bool value, string message) { if (!value) throw new InvalidOperationException(message); count++; }
            string productionPath = SaveStore.FilePath;
            string hash = File.Exists(productionPath) ? UkubolaAutomation.HashFile(productionPath) : null;
            float preference = PlayerPrefs.GetFloat("UKUBOLA.P10.Music", 1);
            string root = Path.Combine(UkubolaAutomation.Project, "Temp", "UkubolaInfrastructureTests", Guid.NewGuid().ToString("N"));
            using (QaEnvironment.Isolate(root))
            {
                Assert(QaEnvironment.Active && SaveStore.FilePath.StartsWith(root, StringComparison.OrdinalIgnoreCase), "QA save path is not isolated.");
                Assert(MenuPreferences.Load().Music == 1, "Isolated defaults leaked production preferences.");
                var options = MenuPreferences.Defaults; options.Music = .25f; options.Save();
                Assert(File.Exists(Path.Combine(root, "preferences.json")), "QA preferences were not written locally.");
                Assert(MenuPreferences.Load().Music == .25f, "QA preference round trip failed.");
                using (QaEnvironment.Isolate(Path.Combine(root, "nested"))) Assert(MenuPreferences.Load().Music == 1, "Nested QA scope leaked values.");
                Assert(MenuPreferences.Load().Music == .25f, "QA scope did not restore state.");
                var previous = QaEnvironment.Root;
                bool rejected = false; try { using (QaEnvironment.Isolate(Application.persistentDataPath)) { } } catch (ArgumentException) { rejected = true; }
                Assert(rejected && QaEnvironment.Root == previous, "Production storage accepted as QA target.");
            }
            Assert(SaveStore.FilePath == productionPath, "QA path leaked outside its scope.");
            Assert(PlayerPrefs.GetFloat("UKUBOLA.P10.Music", 1) == preference, "Production PlayerPrefs changed.");
            Assert((File.Exists(productionPath) ? UkubolaAutomation.HashFile(productionPath) : null) == hash, "Production save changed.");
            return count + " infrastructure assertions passed. Production save/preferences unchanged.";
        }
    }
}

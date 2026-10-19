using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Byrniee.UnityConfiguration.Internal;
using NUnit.Framework;

namespace Byrniee.UnityConfiguration.Editor.Tests
{
    public class ConfigFactoryTests
    {
        private string directory;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "UnityConfigurationTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(directory, true);
        }

        [Test]
        public void BaseOnly_LoadsConfigJson()
        {
            WriteFile("config.json", @"{ ""Server"": { ""Host"": ""base"", ""Port"": 80 } }");

            ConfigFactory factory = CreateFactory(null);
            ServerSettings server = factory.Create<ServerSettings>("Server").Value;

            Assert.That(factory.EnvironmentName, Is.Null);
            Assert.That(server.Host, Is.EqualTo("base"));
            Assert.That(server.Port, Is.EqualTo(80));
            Assert.That(FileNames(factory), Is.EqualTo(new[] { "config.json" }));
        }

        [Test]
        public void MissingSection_ReturnsNewInstance()
        {
            WriteFile("config.json", "{}");

            ServerSettings server = CreateFactory(null).Create<ServerSettings>("Server").Value;

            Assert.That(server, Is.Not.Null);
            Assert.That(server.Host, Is.Null);
        }

        [Test]
        public void OneLayer_OverridesOnlyValuesItContains()
        {
            WriteFile("config.json", @"{ ""Server"": { ""Host"": ""base"", ""Port"": 80 }, ""Other"": { ""Name"": ""base"" } }");
            WriteFile("config.Staging.json", @"{ ""Server"": { ""Host"": ""staging"" } }");

            ConfigFactory factory = CreateFactory("Staging");
            ServerSettings server = factory.Create<ServerSettings>("Server").Value;

            Assert.That(server.Host, Is.EqualTo("staging"));
            Assert.That(server.Port, Is.EqualTo(80), "Keys missing from the layer inherit the base value.");
            Assert.That(factory.Create<OtherSettings>("Other").Value.Name, Is.EqualTo("base"));
            Assert.That(FileNames(factory), Is.EqualTo(new[] { "config.json", "config.Staging.json" }));
        }

        [Test]
        public void MultipleLayers_AppliedInOrder()
        {
            WriteFile("config.json", @"{ ""Server"": { ""Host"": ""base"", ""Port"": 80, ""Name"": ""base"" } }");
            WriteFile("config.Manchester.json", @"{ ""Server"": { ""Host"": ""manchester"", ""Port"": 81 } }");
            WriteFile("config.Manchester.Strongman.json", @"{ ""Server"": { ""Port"": 82 } }");

            ConfigFactory factory = CreateFactory("Manchester.Strongman");
            ServerSettings server = factory.Create<ServerSettings>("Server").Value;

            Assert.That(server.Name, Is.EqualTo("base"));
            Assert.That(server.Host, Is.EqualTo("manchester"));
            Assert.That(server.Port, Is.EqualTo(82));
            Assert.That(FileNames(factory), Is.EqualTo(new[] { "config.json", "config.Manchester.json", "config.Manchester.Strongman.json" }));
        }

        [Test]
        public void MultipleLayers_MissingMiddleLayerIsSkipped()
        {
            WriteFile("config.json", @"{ ""Server"": { ""Host"": ""base"", ""Port"": 80 } }");
            WriteFile("config.Manchester.Strongman.json", @"{ ""Server"": { ""Port"": 82 } }");

            ConfigFactory factory = CreateFactory("Manchester.Strongman");
            ServerSettings server = factory.Create<ServerSettings>("Server").Value;

            Assert.That(server.Host, Is.EqualTo("base"));
            Assert.That(server.Port, Is.EqualTo(82));
            Assert.That(FileNames(factory), Is.EqualTo(new[] { "config.json", "config.Manchester.Strongman.json" }));
            Assert.That(factory.LoadedFiles.All(Path.IsPathRooted), Is.True);
        }

        [Test]
        public void MissingBaseFile_LayerStillApplied()
        {
            WriteFile("config.Staging.json", @"{ ""Server"": { ""Host"": ""staging"" } }");

            ConfigFactory factory = CreateFactory("Staging");

            Assert.That(factory.Create<ServerSettings>("Server").Value.Host, Is.EqualTo("staging"));
            Assert.That(FileNames(factory), Is.EqualTo(new[] { "config.Staging.json" }));
        }

        [Test]
        public void NestedObjects_AreDeepMerged()
        {
            WriteFile("config.json", @"{ ""Nested"": { ""Inner"": { ""A"": ""base-a"", ""B"": ""base-b"", ""Deeper"": { ""X"": 1, ""Y"": 2 } } } }");
            WriteFile("config.Staging.json", @"{ ""Nested"": { ""Inner"": { ""B"": ""layer-b"", ""Deeper"": { ""Y"": 20 } } } }");

            NestedSettings nested = CreateFactory("Staging").Create<NestedSettings>("Nested").Value;

            Assert.That(nested.Inner.A, Is.EqualTo("base-a"));
            Assert.That(nested.Inner.B, Is.EqualTo("layer-b"));
            Assert.That(nested.Inner.Deeper.X, Is.EqualTo(1));
            Assert.That(nested.Inner.Deeper.Y, Is.EqualTo(20));
        }

        [Test]
        public void Arrays_AreReplacedNotAppended()
        {
            WriteFile("config.json", @"{ ""Dmx"": { ""Connections"": [ ""a"", ""b"", ""c"" ], ""Universe"": 1 } }");
            WriteFile("config.Staging.json", @"{ ""Dmx"": { ""Connections"": [ ""d"" ] } }");

            DmxSettings dmx = CreateFactory("Staging").Create<DmxSettings>("Dmx").Value;

            Assert.That(dmx.Connections, Is.EqualTo(new[] { "d" }));
            Assert.That(dmx.Universe, Is.EqualTo(1));
        }

        [Test]
        public void NullInLayer_OverwritesBaseValue()
        {
            WriteFile("config.json", @"{ ""Server"": { ""Host"": ""base"", ""Port"": 80 }, ""Other"": { ""Name"": ""base"" } }");
            WriteFile("config.Staging.json", @"{ ""Server"": { ""Host"": null }, ""Other"": null }");

            ConfigFactory factory = CreateFactory("Staging");
            ServerSettings server = factory.Create<ServerSettings>("Server").Value;
            OtherSettings other = factory.Create<OtherSettings>("Other").Value;

            Assert.That(server.Host, Is.Null);
            Assert.That(server.Port, Is.EqualTo(80));
            Assert.That(other, Is.Not.Null, "A null section is treated like a missing section.");
            Assert.That(other.Name, Is.Null);
        }

        [Test]
        public void CommentsAndUnquotedKeys_StillParse()
        {
            WriteFile("config.json", @"{
  // Line comment
  Server: { Host: ""base"", Port: 80 }, /* block comment */
  MultiCanvasSettings: {
    LaneIndexByCanvasIndex: { 0: 2, 1: 3 } // trailing comment
  }
}");
            WriteFile("config.Staging.json", @"{
  // Only the port changes
  Server: { Port: 81 }
}");

            ConfigFactory factory = CreateFactory("Staging");
            ServerSettings server = factory.Create<ServerSettings>("Server").Value;
            MultiCanvasSettings canvas = factory.Create<MultiCanvasSettings>("MultiCanvasSettings").Value;

            Assert.That(server.Host, Is.EqualTo("base"));
            Assert.That(server.Port, Is.EqualTo(81));
            Assert.That(canvas.LaneIndexByCanvasIndex[0], Is.EqualTo(2));
            Assert.That(canvas.LaneIndexByCanvasIndex[1], Is.EqualTo(3));
        }

        [Test]
        public void DateStrings_AreNotReformatted()
        {
            WriteFile("config.json", @"{ ""Other"": { ""Name"": ""2023-06-27T10:00:00Z"" } }");

            OtherSettings other = CreateFactory(null).Create<OtherSettings>("Other").Value;

            Assert.That(other.Name, Is.EqualTo("2023-06-27T10:00:00Z"));
        }

        [Test]
        public void EnvironmentVariable_TakesPrecedenceOverEnvFile()
        {
            WriteBaseAndEnvironmentLayers();
            WriteFile(".env", "UNITY_ENVIRONMENT=FromFile");

            ConfigFactory factory = CreateFactory("FromVariable");

            Assert.That(factory.EnvironmentName, Is.EqualTo("FromVariable"));
            Assert.That(factory.Create<ServerSettings>("Server").Value.Host, Is.EqualTo("variable"));
        }

        [Test]
        public void EnvFile_UsedWhenEnvironmentVariableNotSet()
        {
            WriteBaseAndEnvironmentLayers();
            WriteFile(".env", "# comment\nOTHER=1\nUNITY_ENVIRONMENT = \"FromFile\"\n");

            ConfigFactory factory = CreateFactory(null);

            Assert.That(factory.EnvironmentName, Is.EqualTo("FromFile"));
            Assert.That(factory.Create<ServerSettings>("Server").Value.Host, Is.EqualTo("file"));
        }

        [Test]
        public void ProcessEnvironmentVariable_TakesPrecedenceOverEnvFile()
        {
            WriteBaseAndEnvironmentLayers();
            WriteFile(".env", "UNITY_ENVIRONMENT=FromFile");

            string previous = Environment.GetEnvironmentVariable(ConfigEnvironment.VariableName);
            Environment.SetEnvironmentVariable(ConfigEnvironment.VariableName, "FromVariable");
            try
            {
                ConfigFactory factory = new ConfigFactory(directory);

                Assert.That(factory.Create<ServerSettings>("Server").Value.Host, Is.EqualTo("variable"));
            }
            finally
            {
                Environment.SetEnvironmentVariable(ConfigEnvironment.VariableName, previous);
            }
        }

        [Test]
        public void UnrequestedSections_AreReported()
        {
            WriteFile("config.json", @"{ ""Server"": {}, ""Other"": {}, ""Stale"": {} }");
            WriteFile("config.Staging.json", @"{ ""LayerOnly"": {} }");

            ConfigFactory factory = CreateFactory("Staging");
            factory.Create<ServerSettings>("Server");
            factory.Create<OtherSettings>("Other");
            factory.Create<OtherSettings>("NotInConfig");

            Assert.That(factory.GetUnrequestedSections(), Is.EquivalentTo(new[] { "Stale", "LayerOnly" }));
        }

        [Test]
        public void GetLayerFileNames_SplitsOnDots()
        {
            Assert.That(ConfigEnvironment.GetLayerFileNames(null), Is.EqualTo(new[] { "config.json" }));
            Assert.That(ConfigEnvironment.GetLayerFileNames("Staging"), Is.EqualTo(new[] { "config.json", "config.Staging.json" }));
            Assert.That(
                ConfigEnvironment.GetLayerFileNames("Manchester.Strongman"),
                Is.EqualTo(new[] { "config.json", "config.Manchester.json", "config.Manchester.Strongman.json" }));
        }

        private ConfigFactory CreateFactory(string environmentVariable)
        {
            return new ConfigFactory(directory, name => name == ConfigEnvironment.VariableName ? environmentVariable : null);
        }

        private void WriteBaseAndEnvironmentLayers()
        {
            WriteFile("config.json", @"{ ""Server"": { ""Host"": ""base"" } }");
            WriteFile("config.FromFile.json", @"{ ""Server"": { ""Host"": ""file"" } }");
            WriteFile("config.FromVariable.json", @"{ ""Server"": { ""Host"": ""variable"" } }");
        }

        private void WriteFile(string fileName, string contents)
        {
            File.WriteAllText(Path.Combine(directory, fileName), contents);
        }

        private static string[] FileNames(ConfigFactory factory)
        {
            return factory.LoadedFiles.Select(Path.GetFileName).ToArray();
        }

        private class ServerSettings
        {
            public string Host { get; set; }

            public int Port { get; set; }

            public string Name { get; set; }
        }

        private class OtherSettings
        {
            public string Name { get; set; }
        }

        private class NestedSettings
        {
            public InnerSettings Inner { get; set; }
        }

        private class InnerSettings
        {
            public string A { get; set; }

            public string B { get; set; }

            public DeeperSettings Deeper { get; set; }
        }

        private class DeeperSettings
        {
            public int X { get; set; }

            public int Y { get; set; }
        }

        private class DmxSettings
        {
            public List<string> Connections { get; set; }

            public int Universe { get; set; }
        }

        private class MultiCanvasSettings
        {
            public Dictionary<int, int> LaneIndexByCanvasIndex { get; set; }
        }
    }
}

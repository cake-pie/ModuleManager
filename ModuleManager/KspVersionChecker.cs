using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ModuleManager.Extensions;
using ModuleManager.Logging;
using ModuleManager.Progress;
using NodeStack = ModuleManager.Collections.ImmutableStack<ConfigNode>;

namespace ModuleManager
{
    public interface IKspVersionChecker
    {
        bool CheckKspVersion(string version);
        bool CheckKspVersionExpression(string kspVersionString);
        void CheckKspVersionRecursive(ConfigNode node, UrlDir.UrlConfig urlConfig);
    }

    public class KspVersionChecker : IKspVersionChecker
    {
        private readonly IPatchProgress progress;
        private readonly IBasicLogger logger;
        private readonly KspVersion kspVersion;
        private static readonly Regex versionParser = new Regex(@"^(?<comparator>[<>]?≈?)?(?<major>\d+)(?:\.(?<minor>\d+)(?:\.(?<revision>\d+))?)?$");

        public KspVersionChecker(IPatchProgress progress, IBasicLogger logger)
        {
            this.progress = progress ?? throw new ArgumentNullException(nameof(progress));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.kspVersion = new KspVersion(Versioning.version_major, Versioning.version_minor, Versioning.Revision);
        }

        public bool CheckKspVersion(string version)
        {
            Match m = versionParser.Match(version);

            if (!m.Success) throw new ArgumentException("malformed version string", nameof(version));

            KspVersion v = new KspVersion(
                int.Parse(m.Groups["major"].Value),
                String.IsNullOrEmpty(m.Groups["minor"].Value) ? null : int.Parse(m.Groups["minor"].Value),
                String.IsNullOrEmpty(m.Groups["revision"].Value) ? null : int.Parse(m.Groups["revision"].Value)
            );

            switch (m.Groups["comparator"].Value)
            {
                case ">":
                    return (kspVersion.CompareTo(v) > 0);
                case ">≈":
                    return (kspVersion.CompareTo(v) >= 0);
                case "<":
                    return (kspVersion.CompareTo(v) < 0);
                case "<≈":
                    return (kspVersion.CompareTo(v) <= 0);
                case "≈":
                default:
                    return (kspVersion.CompareTo(v) == 0);
            }
        }

        public bool CheckKspVersionExpression(string kspVersionExpression)
        {
            if (kspVersionExpression == null) throw new ArgumentNullException(nameof(kspVersionExpression));
            if (kspVersionExpression == string.Empty) throw new ArgumentException("can't be empty", nameof(kspVersionExpression));

            foreach (string andDependencies in kspVersionExpression.Split(',', '&'))
            {
                bool orMatch = false;
                foreach (string orDependency in andDependencies.Split('|'))
                {
                    if (orDependency.Length == 0)
                        continue;

                    bool not = orDependency[0] == '!';
                    string toTest = not ? orDependency.Substring(1) : orDependency;

                    bool tested = CheckKspVersion(toTest);

                    if (not == !tested)
                    {
                        orMatch = true;
                        break;
                    }
                }
                if (!orMatch)
                    return false;
            }

            return true;
        }

        public void CheckKspVersionRecursive(ConfigNode node, UrlDir.UrlConfig urlConfig)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (urlConfig == null) throw new ArgumentNullException(nameof(urlConfig));
            CheckKspVersionRecursive(new NodeStack(node), urlConfig);
        }

        private bool CheckKspVersionName(ref string name)
        {
            if (name == null)
                return true;

            int idxStart = name.IndexOf(":KSP_VERSION[", StringComparison.OrdinalIgnoreCase);
            if (idxStart < 0)
                return true;
            int idxEnd = name.IndexOf(']', idxStart + 7);
            string kspVersionString = name.Substring(idxStart + 7, idxEnd - idxStart - 7);

            name = name.Substring(0, idxStart) + name.Substring(idxEnd + 1);

            return CheckKspVersionExpression(kspVersionString);
        }

        private void CheckKspVersionRecursive(NodeStack nodeStack, UrlDir.UrlConfig urlConfig)
        {
            ConfigNode original = nodeStack.value;
            for (int i = 0; i < original.values.Count; ++i)
            {
                ConfigNode.Value val = original.values[i];
                string valname = val.name;
                try
                {
                    if (CheckKspVersionName(ref valname))
                    {
                        val.name = valname;
                    }
                    else
                    {
                        original.values.Remove(val);
                        i--;
                        progress.KspVersionUnsatisfiedValue(urlConfig, nodeStack.GetPath() + '/' + val.name);
                    }
                }
                catch (ArgumentOutOfRangeException e)
                {
                    progress.Exception("ArgumentOutOfRangeException in CheckKspVersion for value \"" + val.name + "\"", e);
                    throw;
                }
                catch (Exception e)
                {
                    progress.Exception("General Exception in CheckKspVersion for value \"" + val.name + "\"", e);
                    throw;
                }
            }

            for (int i = 0; i < original.nodes.Count; ++i)
            {
                ConfigNode node = original.nodes[i];
                string nodeName = node.name;

                if (nodeName == null)
                {
                    progress.Error(urlConfig, "Error - Node in file " + urlConfig.SafeUrl() + " subnode: " + nodeStack.GetPath() + " has config.name == null");
                }

                try
                {
                    if (CheckKspVersionName(ref nodeName))
                    {
                        node.name = nodeName;
                        CheckKspVersionRecursive(nodeStack.Push(node), urlConfig);
                    }
                    else
                    {
                        original.nodes.Remove(node);
                        i--;
                        progress.KspVersionUnsatisfiedNode(urlConfig, nodeStack.GetPath() + '/' + node.name);
                    }
                }
                catch (ArgumentOutOfRangeException e)
                {
                    progress.Exception("ArgumentOutOfRangeException in CheckKspVersion for node \"" + node.name + "\"", e);
                    throw;
                }
                catch (Exception e)
                {
                    progress.Exception("General Exception " + e.GetType().Name + " for node \"" + node.name + "\"", e);
                    throw;
                }
            }
        }

        private class KspVersion : IComparable
        {
            public int Major { get; }
            public int? Minor { get; }
            public int? Revision { get; }

            public KspVersion(int major, int? minor = null, int? revision = null)
            {
                this.Major = major;
                this.Minor = minor;
                this.Revision = revision;
            }

            public int CompareTo(object obj)
            {
                if (obj == null) return 1;
                KspVersion other = obj as KspVersion;
                if (other != null)
                {
                    int diff = this.Major - other.Major;
                    if (diff != 0 || !this.Minor.HasValue || !other.Minor.HasValue) return diff;
                    diff = this.Minor - other.Minor;
                    if (diff != 0 || !this.Revision.HasValue || !other.Revision.HasValue) return diff;
                    return this.Revision - other.Revision;
                }
                else throw new ArgumentException("not a KspVersion", nameof(obj));
            }
        }
    }
}


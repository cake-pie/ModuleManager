using System;
using ModuleManager.Patches.PassSpecifiers;

namespace ModuleManager.Patches
{
    public class ProtoPatch
    {
        public readonly UrlDir.UrlConfig urlConfig;
        public readonly Command command;
        public readonly string nodeType;
        public readonly string nodeName;
        public readonly string kspVersion = null;
        public readonly string needs = null;
        public readonly string has = null;
        public readonly IPassSpecifier passSpecifier;

        public ProtoPatch(UrlDir.UrlConfig urlConfig, Command command, string nodeType, string nodeName, string kspVersion, string needs, string has, IPassSpecifier passSpecifier)
        {
            this.urlConfig = urlConfig;
            this.command = command;
            this.nodeType = nodeType;
            this.nodeName = nodeName;
            this.kspVersion = kspVersion;
            this.needs = needs;
            this.has = has;
            this.passSpecifier = passSpecifier;
        }
    }
}

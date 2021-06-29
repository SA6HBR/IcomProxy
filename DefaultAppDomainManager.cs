using System;
using System.Security;
using System.Security.Policy;

namespace ICOM_PROXY
{
    /// <summary>
    /// A least-evil (?) way of customizing the default location of the application's user.config files.
    /// </summary>
    public class CustomEvidenceHostSecurityManager : HostSecurityManager
    {
        public override HostSecurityManagerOptions Flags
        {
            get
            {
                return HostSecurityManagerOptions.HostAssemblyEvidence;
            }
        }

        public override Evidence ProvideAssemblyEvidence(System.Reflection.Assembly loadedAssembly, Evidence inputEvidence)
        {
            if (!loadedAssembly.Location.EndsWith("ICOM_PROXY.exe"))
                return base.ProvideAssemblyEvidence(loadedAssembly, inputEvidence);

            // override the full Url used in Evidence to just "YourAppName.exe" so it remains the same no matter where the exe is located
            var zoneEvidence = inputEvidence.GetHostEvidence<Zone>();
            return new Evidence(new EvidenceBase[] { zoneEvidence, new Url("ICOM_PROXY.exe") }, null);
        }
    }

    public class DefaultAppDomainManager : AppDomainManager
    {
        private CustomEvidenceHostSecurityManager hostSecurityManager;
        public override void InitializeNewDomain(AppDomainSetup appDomainInfo)
        {
            base.InitializeNewDomain(appDomainInfo);

            hostSecurityManager = new CustomEvidenceHostSecurityManager();
        }

        public override HostSecurityManager HostSecurityManager
        {
            get
            {
                return hostSecurityManager;
            }
        }
    }
}
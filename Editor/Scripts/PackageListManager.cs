
using System.Collections.Generic;
using System.Linq;

namespace ActionFit.PackageInstaller
{
    internal class PackageListManager
    {
        private readonly List<string> _openUPMPackages = new List<string>
        {
            "jp.hadashikick.vcontainer",
            "com.cysharp.unitask",
            "com.coffee.softmask-for-ugui",
            "com.coffee.ui-effect",
            "com.coffee.ui-particle"
        };

        private readonly List<string> _gitPackages = new List<string>
        {
            "https://github.com/HuiSungz/UnityProjectCore.git"
        };
        
        public List<string> GetAllPackages()
        {
            var allPackages = _openUPMPackages.Where(package => !string.IsNullOrEmpty(package)).ToList();
            allPackages.AddRange(from package in _gitPackages where !string.IsNullOrEmpty(package) select "git:" + package);

            return allPackages;
        }
        
        public List<string> GetOpenUPMPackages()
        {
            return new List<string>(_openUPMPackages);
        }
    }
}
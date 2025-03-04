
using UnityEditor;

namespace ActionFit.PackageInstaller
{
    internal class InstallationStateManager
    {
        private const string IsInstallingKey = "ActFit_IsInstalling";
        private const string CurrentPackageIndexKey = "ActFit_CurrentPackageIndex";
        private const string TotalPackagesKey = "ActFit_TotalPackages";

        public bool IsInstalling
        {
            get => EditorPrefs.GetBool(IsInstallingKey, false);
            set => EditorPrefs.SetBool(IsInstallingKey, value);
        }

        public int CurrentPackageIndex
        {
            get => EditorPrefs.GetInt(CurrentPackageIndexKey, 0);
            set => EditorPrefs.SetInt(CurrentPackageIndexKey, value);
        }

        public int TotalPackages
        {
            get => EditorPrefs.GetInt(TotalPackagesKey, 0);
            set => EditorPrefs.SetInt(TotalPackagesKey, value);
        }

        public void Reset()
        {
            EditorPrefs.DeleteKey(IsInstallingKey);
            EditorPrefs.DeleteKey(CurrentPackageIndexKey);
            EditorPrefs.DeleteKey(TotalPackagesKey);
        }
    }
}
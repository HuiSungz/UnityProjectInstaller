
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace ActionFit.PackageInstaller
{
    public class OpenUPMInstaller
    {
        public async Task InstallPackage(string packageName)
        {
            try
            {
                // UnityEditor.PackageManager.Client는 비동기적으로 작동하므로 UniTask로 래핑
                await AddOpenUPMScopeAsync();
                await AddPackageAsync(packageName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"OpenUPM 패키지 설치 중 오류 발생: {ex.Message}");
                throw;
            }
        }

        private async Task AddOpenUPMScopeAsync()
        {
            var projectPath = Directory.GetCurrentDirectory();
            var npmrcPath = Path.Combine(projectPath, ".npmrc");

            if (!File.Exists(npmrcPath))
            {
                File.WriteAllText(npmrcPath, "registry=https://package.openupm.com\n");
                AssetDatabase.Refresh();
            }
            else
            {
                // 이미 존재하면 OpenUPM 레지스트리가 추가되어 있는지 확인
                var content = await File.ReadAllTextAsync(npmrcPath);
                if (!content.Contains("registry=https://package.openupm.com"))
                {
                    await File.AppendAllTextAsync(npmrcPath, "\nregistry=https://package.openupm.com\n");
                    AssetDatabase.Refresh();
                }
            }

            await Task.Yield();
        }

        private async Task AddPackageAsync(string packageName)
        {
            var addRequest = UnityEditor.PackageManager.Client.Add(packageName);
            while (!addRequest.IsCompleted)
            {
                await Task.Yield();
            }

            if (addRequest.Status == UnityEditor.PackageManager.StatusCode.Failure)
            {
                throw new Exception($"패키지 설치 실패: {addRequest.Error.message}");
            }
        }
    }
}

using System;
using System.Threading.Tasks;
using UnityEngine;

namespace ActionFit.PackageInstaller
{
    public class GitPackageInstaller
    {
        public async Task InstallPackage(string gitUrl)
        {
            try
            {
                await AddPackageAsync(gitUrl);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Git 패키지 설치 중 오류 발생: {ex.Message}");
                throw;
            }
        }

        private async Task AddPackageAsync(string gitUrl)
        {
            var addRequest = UnityEditor.PackageManager.Client.Add(gitUrl);
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
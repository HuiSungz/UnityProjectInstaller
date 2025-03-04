
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor.PackageManager;
using UnityEngine;

namespace ActionFit.PackageInstaller
{
    public class OpenUPMInstaller
    {
        private static readonly string ManifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");

        public async Task InstallPackage(string packageName)
        {
            try
            {
                AddPackage(packageName);
                await WaitForPackageInstallation(packageName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"OpenUPM 패키지 설치 중 오류 발생: {ex.Message}");
                throw;
            }
        }

        private void AddPackage(string packageName)
        {
            Debug.Log($"패키지 설치 중: {packageName}");
            Client.Add(packageName);
        }
        
        private async Task WaitForPackageInstallation(string packageName)
        {
            var timeout = TimeSpan.FromMinutes(2);
            var startTime = DateTime.Now;
            
            while (true)
            {
                await Task.Delay(500);
                
                if (IsPackageInstalled(packageName))
                {
                    break;
                }
                
                if (DateTime.Now - startTime > timeout)
                {
                    throw new TimeoutException($"패키지 {packageName} 설치 타임아웃");
                }
            }
        }
        
        private bool IsPackageInstalled(string packageName)
        {
            var request = Client.List(true);
            while (!request.IsCompleted)
            {
                System.Threading.Thread.Sleep(100);
            }
            
            if (request.Status == StatusCode.Success)
            {
                foreach (var package in request.Result)
                {
                    if (package.name == packageName)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
    }
}
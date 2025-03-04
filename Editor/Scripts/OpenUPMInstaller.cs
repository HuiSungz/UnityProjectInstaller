
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor.PackageManager;
#if INSTALL_NEWTON
using UnityEditor;
using Newtonsoft.Json.Linq;
#endif

namespace ActionFit.PackageInstaller
{
    public class OpenUPMInstaller
    {
        private static readonly string ManifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
        
        public async Task InstallPackage(string packageName)
        {
            try
            {
                // 패키지 레지스트리 추가
                EnsureOpenUPMRegistry(packageName);
                
                // 패키지 설치
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
            var timeout = TimeSpan.FromSeconds(20);
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
        
        private bool EnsureOpenUPMRegistry(string packageName)
        {
            if (!File.Exists(ManifestPath))
            {
                Debug.LogError("manifest.json 파일을 찾을 수 없습니다: " + ManifestPath);
                return false;
            }

            
#if INSTALL_NEWTON
            string json = File.ReadAllText(ManifestPath);
            JObject manifest = JObject.Parse(json);

            // scopedRegistries 객체 가져오기 또는 생성
            JArray scopedRegistries;
            if (manifest.ContainsKey("scopedRegistries"))
            {
                scopedRegistries = (JArray)manifest["scopedRegistries"];
            }
            else
            {
                scopedRegistries = new JArray();
                manifest["scopedRegistries"] = scopedRegistries;
            }

            // OpenUPM 레지스트리 찾기
            JObject openUPMRegistry = null;
            foreach (JObject registry in scopedRegistries)
            {
                if (registry["url"] != null && registry["url"].ToString() == "https://package.openupm.com")
                {
                    openUPMRegistry = registry;
                    break;
                }
            }

            // OpenUPM 레지스트리가 없으면 새로 생성
            if (openUPMRegistry == null)
            {
                openUPMRegistry = new JObject
                {
                    { "name", "OpenUPM" },
                    { "url", "https://package.openupm.com" },
                    { "scopes", new JArray(packageName) }
                };
                scopedRegistries.Add(openUPMRegistry);
            }
            // 레지스트리가 있으면 스코프 추가 확인
            else
            {
                JArray scopes = (JArray)openUPMRegistry["scopes"];
                bool scopeExists = false;
                
                foreach (var scope in scopes)
                {
                    if (scope.ToString() == packageName)
                    {
                        scopeExists = true;
                        break;
                    }
                }
                
                if (!scopeExists)
                {
                    scopes.Add(packageName);
                }
            }

            // 변경된 manifest.json 저장
            string newJson = manifest.ToString();
            File.WriteAllText(ManifestPath, newJson);
            AssetDatabase.Refresh();
            
            Debug.Log($"OpenUPM 레지스트리에 스코프 {packageName} 추가 완료");
#endif
            return true;
        }
    }
}

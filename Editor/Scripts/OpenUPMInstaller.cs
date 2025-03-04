using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace ActionFit.PackageInstaller
{
    public class OpenUPMInstaller
    {
        // manifest.json 파일 경로
        private static readonly string manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");

        public async Task InstallPackage(string packageName)
        {
            try
            {
                // OpenUPM 레지스트리 추가 및 패키지 설치
                EnsureOpenUPMRegistry(packageName);
                
                // 패키지 설치 상태 확인
                await WaitForPackageInstallation(packageName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"OpenUPM 패키지 설치 중 오류 발생: {ex.Message}");
                throw;
            }
        }

        private void EnsureOpenUPMRegistry(string packageName)
        {
            if (!File.Exists(manifestPath))
            {
                throw new FileNotFoundException("manifest.json 파일을 찾을 수 없습니다.");
            }

            // manifest.json 읽기
            string jsonText = File.ReadAllText(manifestPath);
            var manifest = JsonUtility.FromJson<ManifestJson>(jsonText);
            bool modified = false;

            // 직접 JSON 텍스트를 수정하는 방식으로 변경
            if (!jsonText.Contains("\"scopedRegistries\""))
            {
                // scopedRegistries가 없는 경우 새로 추가
                string scope = GetScopeFromPackageName(packageName);
                string registryContent = 
                    "  \"scopedRegistries\": [\n" +
                    "    {\n" +
                    "      \"name\": \"OpenUPM\",\n" +
                    "      \"url\": \"https://package.openupm.com\",\n" +
                    "      \"scopes\": [\n" +
                    $"        \"{scope}\"\n" +
                    "      ]\n" +
                    "    }\n" +
                    "  ],\n";
                
                jsonText = jsonText.Replace("{", "{\n" + registryContent);
                modified = true;
            }
            else if (!jsonText.Contains("\"url\": \"https://package.openupm.com\""))
            {
                // OpenUPM registry가 없는 경우 추가
                string scope = GetScopeFromPackageName(packageName);
                string registryEntry = 
                    "    {\n" +
                    "      \"name\": \"OpenUPM\",\n" +
                    "      \"url\": \"https://package.openupm.com\",\n" +
                    "      \"scopes\": [\n" +
                    $"        \"{scope}\"\n" +
                    "      ]\n" +
                    "    },\n";
                
                int index = jsonText.IndexOf("\"scopedRegistries\": [") + "\"scopedRegistries\": [".Length;
                jsonText = jsonText.Insert(index, "\n" + registryEntry);
                modified = true;
            }
            else
            {
                // OpenUPM registry가 있는 경우 scope 추가
                string scope = GetScopeFromPackageName(packageName);
                if (!jsonText.Contains($"\"{scope}\""))
                {
                    int startIndex = jsonText.IndexOf("\"url\": \"https://package.openupm.com\"");
                    int scopesIndex = jsonText.IndexOf("\"scopes\": [", startIndex);
                    int scopesEndIndex = jsonText.IndexOf("]", scopesIndex);
                    
                    // 마지막 scope 다음에 새 scope 추가
                    string scopeEntry = $",\n        \"{scope}\"";
                    jsonText = jsonText.Insert(scopesEndIndex, scopeEntry);
                    modified = true;
                }
            }

            // 변경된 경우 파일 저장
            if (modified)
            {
                File.WriteAllText(manifestPath, jsonText);
                Debug.Log("manifest.json을 업데이트했습니다.");
                AssetDatabase.Refresh();
            }
            
            // 패키지 설치
            Debug.Log($"패키지 설치 중: {packageName}");
            Client.Add(packageName);
        }
        
        // 패키지 이름에서 스코프 추출 (com.company.package -> com.company)
        private string GetScopeFromPackageName(string packageName)
        {
            int lastDotIndex = packageName.LastIndexOf('.');
            if (lastDotIndex > 0)
            {
                return packageName.Substring(0, lastDotIndex);
            }
            return packageName;
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
        
        // JSON 직렬화를 위한 클래스 (필요한 경우만 사용)
        [Serializable]
        private class ManifestJson
        {
            public DependenciesJson dependencies;
        }
        
        [Serializable]
        private class DependenciesJson
        {
            // 동적 필드를 위해 비워둠
        }
    }
}
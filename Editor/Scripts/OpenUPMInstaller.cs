using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ActionFit.PackageInstaller
{
    public class OpenUPMInstaller
    {
        public async Task InstallPackage(string packageName)
        {
            try
            {
                // 먼저 OpenUPM CLI를 통해 패키지 추가
                await AddPackageUsingCLI(packageName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"OpenUPM 패키지 설치 중 오류 발생: {ex.Message}");
                throw;
            }
        }

        private async Task AddPackageUsingCLI(string packageName)
        {
            var projectPath = Directory.GetCurrentDirectory();
            var scopedRegistryContent = $@"{{
  ""scopedRegistries"": [
    {{
      ""name"": ""package.openupm.com"",
      ""url"": ""https://package.openupm.com"",
      ""scopes"": [
        ""com.cysharp.unitask"",
        ""jp.hadashikick.vcontainer"",
        ""com.google.external-dependency-manager"",
        ""com.openupm""
      ]
    }}
  ],
  ""dependencies"": {{
    ""{packageName}"": ""latest""
  }}
}}";

            var manifestPath = Path.Combine(projectPath, "Packages", "manifest.json");
            
            if (!File.Exists(manifestPath))
            {
                throw new FileNotFoundException("manifest.json 파일을 찾을 수 없습니다.");
            }

            // 현재 manifest.json 읽기
            var manifestContent = File.ReadAllText(manifestPath);
            
            // 이미 scopedRegistries가 있는지 확인
            if (!manifestContent.Contains("\"scopedRegistries\""))
            {
                // scopedRegistries가 없으면 추가
                manifestContent = manifestContent.Replace("{", "{\n  \"scopedRegistries\": [\n    {\n      \"name\": \"package.openupm.com\",\n      \"url\": \"https://package.openupm.com\",\n      \"scopes\": [\n        \"com.cysharp.unitask\",\n        \"jp.hadashikick.vcontainer\",\n        \"com.google.external-dependency-manager\",\n        \"com.openupm\"\n      ]\n    }\n  ],");
            }
            else if (!manifestContent.Contains("\"url\": \"https://package.openupm.com\""))
            {
                // OpenUPM registry가 없으면 추가
                var scopedRegistriesIndex = manifestContent.IndexOf("\"scopedRegistries\"");
                var scopedRegistriesStartIndex = manifestContent.IndexOf("[", scopedRegistriesIndex);
                var insertPos = scopedRegistriesStartIndex + 1;
                
                var openUpmRegistry = "\n    {\n      \"name\": \"package.openupm.com\",\n      \"url\": \"https://package.openupm.com\",\n      \"scopes\": [\n        \"com.cysharp.unitask\",\n        \"jp.hadashikick.vcontainer\",\n        \"com.google.external-dependency-manager\",\n        \"com.openupm\"\n      ]\n    },";
                
                manifestContent = manifestContent.Insert(insertPos, openUpmRegistry);
            }
            
            // 패키지가 이미 있는지 확인
            if (!manifestContent.Contains($"\"{packageName}\""))
            {
                // dependencies에 패키지 추가
                var dependenciesIndex = manifestContent.IndexOf("\"dependencies\"");
                var dependenciesStartIndex = manifestContent.IndexOf("{", dependenciesIndex);
                var insertPos = dependenciesStartIndex + 1;
                
                var packageEntry = $"\n    \"{packageName}\": \"latest\",";
                
                manifestContent = manifestContent.Insert(insertPos, packageEntry);
            }
            
            // 수정된 manifest.json 저장
            File.WriteAllText(manifestPath, manifestContent);
            
            // Unity Package Manager 리프레시
            EditorUtility.DisplayProgressBar("패키지 인스톨러", $"{packageName} 설치 중... Unity Package Manager 새로고침", 0.7f);
            
            // 비동기 대기
            await Task.Delay(1000);
            
            AssetDatabase.Refresh();
            
            // 패키지가 추가될 때까지 대기
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
            var request = UnityEditor.PackageManager.Client.List(true);
            while (!request.IsCompleted)
            {
                System.Threading.Thread.Sleep(100);
            }
            
            if (request.Status == UnityEditor.PackageManager.StatusCode.Success)
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
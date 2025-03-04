
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace ActionFit.PackageInstaller
{
    public class SimpleOpenUPMInstaller
    {
        private static readonly string ManifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
        
        public async Task InstallPackage(string packageName)
        {
            try
            {
                // 패키지 레지스트리 추가
                EnsureOpenUPMRegistry(packageName);
                
                // 패키지 설치
                await AddPackageAsync(packageName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"OpenUPM 패키지 설치 중 오류 발생: {ex.Message}");
                throw;
            }
        }

        private async Task AddPackageAsync(string packageName)
        {
            Debug.Log($"패키지 설치 중: {packageName}");
            
            var request = Client.Add(packageName);
            var timeout = TimeSpan.FromSeconds(30);
            var startTime = DateTime.Now;
            
            while (!request.IsCompleted)
            {
                await Task.Delay(100);
                
                if (DateTime.Now - startTime > timeout)
                {
                    throw new TimeoutException($"패키지 {packageName} 설치 타임아웃");
                }
            }
            
            if (request.Status == StatusCode.Success)
            {
                Debug.Log($"패키지 {packageName} 설치 성공");
            }
            else
            {
                throw new Exception($"패키지 {packageName} 설치 실패: {request.Error?.message}");
            }
        }
        
        private bool EnsureOpenUPMRegistry(string packageName)
        {
            if (!File.Exists(ManifestPath))
            {
                Debug.LogError("manifest.json 파일을 찾을 수 없습니다: " + ManifestPath);
                return false;
            }

            try
            {
                string json = File.ReadAllText(ManifestPath);
                
                // 정규 표현식을 사용하여 OpenUPM 레지스트리 확인 및 추가
                if (!json.Contains("https://package.openupm.com"))
                {
                    // OpenUPM 레지스트리가 없으면 새로 추가
                    string registryEntry = 
                        "\"scopedRegistries\": [\n" +
                        "    {\n" +
                        "      \"name\": \"OpenUPM\",\n" +
                        "      \"url\": \"https://package.openupm.com\",\n" +
                        "      \"scopes\": [\n" +
                        $"        \"{packageName}\"\n" +
                        "      ]\n" +
                        "    }\n" +
                        "  ],";
                    
                    // dependencies 앞에 삽입
                    if (json.Contains("\"dependencies\":"))
                    {
                        json = Regex.Replace(json, "(\\s*\"dependencies\":\\s*\\{)", match => registryEntry + "\n" + match.Value);
                    }
                    else
                    {
                        json = json.Insert(1, registryEntry + "\n");
                    }
                }
                else
                {
                    // OpenUPM 레지스트리는 있지만 스코프 확인 및 추가
                    if (!json.Contains($"\"{packageName}\""))
                    {
                        // 기존 스코프 배열의 마지막에 새 스코프 추가
                        string pattern = "(\"scopes\":\\s*\\[[^\\]]*)(\\])";
                        string replacement = "$1" + (json.Contains("\"scopes\": [") ? ",\n        " : "") + $"\"{packageName}\"$2";
                        json = Regex.Replace(json, pattern, replacement);
                    }
                }
                
                // 변경된 manifest.json 저장
                File.WriteAllText(ManifestPath, json);
                AssetDatabase.Refresh();
                
                Debug.Log($"OpenUPM 레지스트리에 스코프 {packageName} 추가 완료");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"OpenUPM 레지스트리 설정 중 오류 발생: {e.Message}");
                return false;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ActionFit.PackageInstaller
{
    [InitializeOnLoad]
    public static class PackageInstallerManager
    {
        // 패키지 목록
        private static readonly List<string> _openUPMPackages = new List<string>
        {
            "jp.hadashikick.vcontainer",
            "com.cysharp.unitask",
            "com.coffee.softmask-for-ugui",
            "com.coffee.ui-effect",
            "com.coffee.ui-particle"
        };
        
        private static readonly List<string> _gitPackages = new List<string>
        {
            "https://github.com/HuiSungz/UnityProjectCore.git"
        };
        
        // EditorPrefs 키
        private const string IsInstallingKey = "ActFit_IsInstalling";
        private const string CurrentPackageIndexKey = "ActFit_CurrentPackageIndex";
        private const string TotalPackagesKey = "ActFit_TotalPackages";
        
        // 현재 설치 상태
        private static bool _isProcessing = false;
        private static Request _currentRequest;
        private static string _currentPackage;
        private static DateTime _installationStartTime;
        private static bool _isGitPackage;
        
        // 생성자 - EditorPrefs 값을 확인하여 설치 중이었다면 재개
        static PackageInstallerManager()
        {
            // Editor 시작 시 지연 실행
            EditorApplication.delayCall += () =>
            {
                bool isInstalling = EditorPrefs.GetBool(IsInstallingKey, false);
                if (isInstalling && !_isProcessing)
                {
                    Debug.Log("[패키지 설치] 이전 설치를 재개합니다...");
                    ResumeInstallation();
                }
            };
        }
        
        [MenuItem("ActFit/Project Initialize")]
        public static void RunInstaller()
        {
            if (_isProcessing)
            {
                Debug.Log("[패키지 설치] 이미 설치가 진행 중입니다.");
                return;
            }
            
            // 설치가 진행 중이었는지 확인
            bool wasInstalling = EditorPrefs.GetBool(IsInstallingKey, false);
            if (wasInstalling)
            {
                // 이전 설치 작업 재개
                ResumeInstallation();
            }
            else
            {
                // 새 설치 시작
                StartNewInstallation();
            }
        }
        
        private static void StartNewInstallation()
        {
            _isProcessing = true;
            
            // 모든 패키지 목록 준비
            List<string> allPackages = new List<string>();
            
            // OpenUPM 패키지 추가
            foreach (var package in _openUPMPackages)
            {
                if (!string.IsNullOrEmpty(package))
                {
                    allPackages.Add(package);
                }
            }
            
            // Git 패키지 추가 (git: 접두사 포함)
            foreach (var package in _gitPackages)
            {
                if (!string.IsNullOrEmpty(package))
                {
                    allPackages.Add("git:" + package);
                }
            }
            
            if (allPackages.Count == 0)
            {
                Debug.Log("[패키지 설치] 설치할 패키지가 없습니다.");
                _isProcessing = false;
                return;
            }
            
            // EditorPrefs에 설치 상태 저장
            EditorPrefs.SetBool(IsInstallingKey, true);
            EditorPrefs.SetInt(CurrentPackageIndexKey, 0);
            EditorPrefs.SetInt(TotalPackagesKey, allPackages.Count);
            
            Debug.Log($"[패키지 설치] 설치를 시작합니다. 총 {allPackages.Count}개 패키지");
            
            // OpenUPM 레지스트리 구성
            EnsureOpenUPMRegistryForAllPackages();
            
            // 첫 번째 패키지 설치 시작
            EditorApplication.delayCall += InstallCurrentPackage;
        }
        
        private static void ResumeInstallation()
        {
            _isProcessing = true;
            
            int index = EditorPrefs.GetInt(CurrentPackageIndexKey, 0);
            int total = EditorPrefs.GetInt(TotalPackagesKey, 0);
            
            Debug.Log($"[패키지 설치] 설치를 재개합니다. ({index+1}/{total})");
            
            // 현재 패키지 설치 시작
            EditorApplication.delayCall += InstallCurrentPackage;
        }
        
        private static void InstallCurrentPackage()
        {
            int index = EditorPrefs.GetInt(CurrentPackageIndexKey, 0);
            int total = EditorPrefs.GetInt(TotalPackagesKey, 0);
            
            // 모든 패키지 설치 완료 확인
            if (index >= total)
            {
                CompleteInstallation();
                return;
            }
            
            // 현재 패키지 정보 가져오기
            string nextPackage = GetPackageAtIndex(index);
            if (string.IsNullOrEmpty(nextPackage))
            {
                // 잘못된 인덱스이면 다음으로 진행
                EditorPrefs.SetInt(CurrentPackageIndexKey, index + 1);
                EditorApplication.delayCall += InstallCurrentPackage;
                return;
            }
            
            // Git 패키지 확인
            _isGitPackage = nextPackage.StartsWith("git:");
            if (_isGitPackage)
            {
                _currentPackage = nextPackage.Substring(4); // "git:" 접두사 제거
            }
            else
            {
                _currentPackage = nextPackage;
            }
            
            try
            {
                Debug.Log($"[패키지 설치] ({index+1}/{total}) {_currentPackage} 설치 중...");
                EditorUtility.DisplayProgressBar("패키지 설치", $"({index+1}/{total}) {_currentPackage} 설치 중...", (float)index / total);
                
                // 패키지 설치 요청
                _currentRequest = Client.Add(_currentPackage);
                _installationStartTime = DateTime.Now;
                
                // 설치 완료 모니터링 시작
                EditorApplication.update += MonitorInstallation;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[패키지 설치] {_currentPackage} 설치 요청 중 오류: {ex.Message}");
                
                // 다음 패키지로 진행
                EditorPrefs.SetInt(CurrentPackageIndexKey, index + 1);
                EditorApplication.delayCall += InstallCurrentPackage;
            }
        }
        
        private static void MonitorInstallation()
        {
            int index = EditorPrefs.GetInt(CurrentPackageIndexKey, 0);
            int total = EditorPrefs.GetInt(TotalPackagesKey, 0);
            
            // 타임아웃 확인
            float timeoutSeconds = 30f; // 모든 패키지 타임아웃 30초로 통일
            if ((DateTime.Now - _installationStartTime).TotalSeconds > timeoutSeconds)
            {
                Debug.LogWarning($"[패키지 설치] {_currentPackage} 설치 시간 초과");
                EditorApplication.update -= MonitorInstallation;
                
                // 다음 패키지로 진행
                EditorPrefs.SetInt(CurrentPackageIndexKey, index + 1);
                EditorApplication.delayCall += InstallCurrentPackage;
                return;
            }
            
            // 요청 완료 확인
            if (_currentRequest == null || !_currentRequest.IsCompleted)
                return;
            
            EditorApplication.update -= MonitorInstallation;
            
            if (_currentRequest.Status == StatusCode.Success)
            {
                Debug.Log($"[패키지 설치] {_currentPackage} 설치 성공");
            }
            else
            {
                Debug.LogError($"[패키지 설치] {_currentPackage} 설치 실패: {_currentRequest.Error?.message}");
            }
            
            // 다음 패키지로 진행하기 전에 지연 추가
            // Git 패키지는 더 긴 지연 적용
            int delayFrames = _isGitPackage ? 20 : 10;
            
            void DelayedNextPackage(int remainingFrames)
            {
                if (remainingFrames <= 0)
                {
                    // 다음 패키지로 진행
                    EditorPrefs.SetInt(CurrentPackageIndexKey, index + 1);
                    InstallCurrentPackage();
                    return;
                }
                
                EditorApplication.delayCall += () => DelayedNextPackage(remainingFrames - 1);
            }
            
            EditorApplication.delayCall += () => DelayedNextPackage(delayFrames);
        }
        
        private static void CompleteInstallation()
        {
            _isProcessing = false;
            
            // EditorPrefs 정리
            EditorPrefs.DeleteKey(IsInstallingKey);
            EditorPrefs.DeleteKey(CurrentPackageIndexKey);
            EditorPrefs.DeleteKey(TotalPackagesKey);
            
            EditorUtility.ClearProgressBar();
            Debug.Log("[패키지 설치] 모든 패키지 설치가 완료되었습니다.");
            
            // 인스톨러 패키지 자체 제거
            EditorApplication.delayCall += RemoveInstallerPackage;
        }
        
        private static void RemoveInstallerPackage()
        {
            Debug.Log("[패키지 설치] 인스톨러 패키지를 제거합니다...");
    
            try
            {
                // 직접 패키지 이름 지정 (인스톨러 패키지의 이름을 고정값으로 설정)
                string packageName = "com.actionfit.projectinstaller";
        
                Debug.Log($"[패키지 설치] 패키지 {packageName} 제거 중...");
        
                // 패키지 제거 요청
                var request = Client.Remove(packageName);
        
                // 제거 완료 모니터링
                DateTime startTime = DateTime.Now;
        
                EditorApplication.update += () =>
                {
                    // 타임아웃 확인
                    if ((DateTime.Now - startTime).TotalSeconds > 30)
                    {
                        Debug.LogWarning("[패키지 설치] 패키지 제거 시간 초과");
                        return;
                    }
            
                    if (!request.IsCompleted)
                        return;
            
                    if (request.Status == StatusCode.Success)
                    {
                        Debug.Log("[패키지 설치] 인스톨러 패키지 제거 완료");
                    }
                    else
                    {
                        Debug.LogError($"[패키지 설치] 인스톨러 패키지 제거 실패: {request.Error?.message}");
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[패키지 설치] 인스톨러 패키지 제거 중 오류: {ex.Message}");
            }
        }
        
        private static string GetPackageAtIndex(int index)
        {
            List<string> allPackages = new List<string>();
            
            // OpenUPM 패키지 추가
            foreach (var package in _openUPMPackages)
            {
                if (!string.IsNullOrEmpty(package))
                {
                    allPackages.Add(package);
                }
            }
            
            // Git 패키지 추가
            foreach (var package in _gitPackages)
            {
                if (!string.IsNullOrEmpty(package))
                {
                    allPackages.Add("git:" + package);
                }
            }
            
            if (index < 0 || index >= allPackages.Count)
                return null;
                
            return allPackages[index];
        }
        
        private static bool EnsureOpenUPMRegistryForAllPackages()
        {
            string manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
            
            if (!File.Exists(manifestPath))
            {
                Debug.LogError("[패키지 설치] manifest.json 파일을 찾을 수 없습니다");
                return false;
            }
            
            try
            {
                string json = File.ReadAllText(manifestPath);
                bool needsUpdate = false;
                
                // OpenUPM 패키지에서 고유한 스코프 목록 추출
                HashSet<string> scopes = new HashSet<string>();
                foreach (var package in _openUPMPackages)
                {
                    if (string.IsNullOrEmpty(package))
                        continue;
                    
                    // 패키지 이름에서 스코프 추출 (com.company.package -> com.company)
                    var parts = package.Split('.');
                    if (parts.Length >= 2)
                    {
                        scopes.Add($"{parts[0]}.{parts[1]}");
                    }
                    else
                    {
                        scopes.Add(package);
                    }
                }
                
                // 스코프가 없으면 작업 필요 없음
                if (scopes.Count == 0)
                    return true;
                
                // OpenUPM 레지스트리가 없는 경우 새로 추가
                if (!json.Contains("https://package.openupm.com"))
                {
                    // 스코프 문자열 구성
                    var scopesList = scopes.ToList();
                    string scopesJson = "";
                    for (int i = 0; i < scopesList.Count; i++)
                    {
                        scopesJson += $"        \"{scopesList[i]}\"";
                        if (i < scopesList.Count - 1)
                        {
                            scopesJson += ",\n";
                        }
                    }
                    
                    // OpenUPM 레지스트리 추가
                    string registryEntry = 
                        "\"scopedRegistries\": [\n" +
                        "    {\n" +
                        "      \"name\": \"OpenUPM\",\n" +
                        "      \"url\": \"https://package.openupm.com\",\n" +
                        "      \"scopes\": [\n" +
                        scopesJson + "\n" +
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
                    
                    needsUpdate = true;
                }
                // OpenUPM 레지스트리가 있는 경우 필요한 스코프 추가
                else
                {
                    foreach (var scope in scopes)
                    {
                        // 스코프가 없는 경우에만 추가
                        if (!json.Contains($"\"{scope}\""))
                        {
                            // 기존 스코프 배열의 마지막에 새 스코프 추가
                            string pattern = "(\"scopes\":\\s*\\[[^\\]]*)(\\])";
                            string replacement = "$1" + (json.Contains("\"scopes\": [") && !json.Contains("\"scopes\": []") ? ",\n        " : "") + $"\"{scope}\"$2";
                            json = Regex.Replace(json, pattern, replacement);
                            
                            needsUpdate = true;
                        }
                    }
                }
                
                if (needsUpdate)
                {
                    // 변경된 manifest.json 저장
                    File.WriteAllText(manifestPath, json);
                    Debug.Log("[패키지 설치] OpenUPM 레지스트리 구성이 업데이트되었습니다");
                }
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[패키지 설치] OpenUPM 레지스트리 설정 중 오류: {e.Message}");
                return false;
            }
        }
        
        private static string GetCurrentPackageName()
        {
            try
            {
                var packagePath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(
                    ScriptableObject.CreateInstance<DummyScriptableObject>()));
                    
                if (string.IsNullOrEmpty(packagePath))
                {
                    return null;
                }
                
                var scriptDirectory = Path.GetDirectoryName(packagePath);
                
                if (string.IsNullOrEmpty(scriptDirectory))
                {
                    return null;
                }
                
                var directory = new DirectoryInfo(Path.Combine(Application.dataPath, ".."));
                var packageJsonFiles = directory.GetFiles("package.json", SearchOption.AllDirectories);
                
                foreach (var file in packageJsonFiles)
                {
                    try
                    {
                        string json = File.ReadAllText(file.FullName);
                        
                        if (json.Contains("\"name\"") && file.FullName.Contains(scriptDirectory.Replace("/", "\\")))
                        {
                            var namePattern = "\"name\"\\s*:\\s*\"([^\"]+)\"";
                            var match = Regex.Match(json, namePattern);
                            
                            if (match.Success)
                            {
                                return match.Groups[1].Value;
                            }
                        }
                    }
                    catch
                    {
                        // 파일 읽기 오류 무시
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[패키지 설치] 패키지 이름 가져오기 오류: {ex.Message}");
                return null;
            }
        }
        
        [MenuItem("ActFit/Utilities/Reset Package Installation")]
        public static void ResetInstallation()
        {
            if (EditorUtility.DisplayDialog("패키지 설치 초기화", 
                "패키지 설치 상태를 초기화하시겠습니까?\n\n" +
                "이 작업은 현재 진행 중인 설치를 취소하고 처음부터 다시 시작할 수 있게 합니다.", 
                "초기화", "취소"))
            {
                EditorPrefs.DeleteKey(IsInstallingKey);
                EditorPrefs.DeleteKey(CurrentPackageIndexKey);
                EditorPrefs.DeleteKey(TotalPackagesKey);
                
                _isProcessing = false;
                
                Debug.Log("[패키지 설치] 패키지 설치 상태가 초기화되었습니다. ActFit/Project Initialize 메뉴를 통해 다시 시작하세요.");
            }
        }
        
        private class DummyScriptableObject : ScriptableObject { }
    }
}
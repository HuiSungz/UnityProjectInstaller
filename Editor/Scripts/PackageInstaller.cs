
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
    public static class PackageInstallerManager
    {
        private static List<string> _openUPMPackages = new List<string>
        {
            "jp.hadashikick.vcontainer",
            "com.cysharp.unitask",
            "com.coffee.softmask-for-ugui",
            "com.coffee.ui-effect",
            "com.coffee.ui-particle"
        };
        
        private static List<string> _gitPackages = new List<string>
        {
            "https://github.com/HuiSungz/UnityProjectCore.git"
        };
        
        private static Queue<string> _installQueue = new Queue<string>();
        private static bool _isInstalling = false;
        private static string _currentPackage = "";
        private static DateTime _installationStartTime;
        private static Request _currentRequest;
        private static int _totalPackages;
        private static int _installedPackages;
        private static bool _isGitPackage = false;
        
        [MenuItem("ActFit/Project Initialize")]
        public static void RunInstaller()
        {
            if (_isInstalling)
            {
                Debug.Log("[패키지 설치] 이미 설치가 진행 중입니다.");
                return;
            }
            
            // 필요한 폴더 생성
            EnsureRequiredFolders();
            
            // 설치 시작
            StartInstallation();
        }
        
        private static void EnsureRequiredFolders()
        {
            string[] folders = {
                "Plugins",
                "Plugins/Android",
                "Plugins/iOS"
            };
            
            foreach (var folder in folders)
            {
                string path = Path.Combine(Application.dataPath, folder);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
        }
        
        private static void StartInstallation()
        {
            _isInstalling = true;
            _installQueue.Clear();
            _installedPackages = 0;
            
            // OpenUPM 패키지 먼저 큐에 추가
            foreach (var package in _openUPMPackages)
            {
                if (!string.IsNullOrEmpty(package))
                {
                    _installQueue.Enqueue(package);
                }
            }
            
            // Git 패키지를 큐에 추가
            foreach (var package in _gitPackages)
            {
                if (!string.IsNullOrEmpty(package))
                {
                    _installQueue.Enqueue("git:" + package);
                }
            }
            
            _totalPackages = _installQueue.Count;
            
            if (_totalPackages == 0)
            {
                _isInstalling = false;
                Debug.Log("[패키지 설치] 설치할 패키지가 없습니다.");
                return;
            }
            
            Debug.Log($"[패키지 설치] 설치를 시작합니다. 총 {_totalPackages}개 패키지");
            
            // OpenUPM 레지스트리 구성
            EnsureOpenUPMRegistryForAllPackages();
            
            // 첫 번째 패키지 설치 시작
            EditorApplication.delayCall += ProcessNextPackage;
        }
        
        private static void ProcessNextPackage()
        {
            if (!_isInstalling || _installQueue.Count == 0)
            {
                if (_isInstalling)
                {
                    _isInstalling = false;
                    Debug.Log("[패키지 설치] 모든 패키지 설치가 완료되었습니다.");
                }
                return;
            }
            
            string nextPackage = _installQueue.Dequeue();
            
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
            
            _installedPackages++;
            
            try
            {
                Debug.Log($"[패키지 설치] ({_installedPackages}/{_totalPackages}) {_currentPackage} 설치 중...");
                
                // 패키지 설치 요청
                _currentRequest = Client.Add(_currentPackage);
                _installationStartTime = DateTime.Now;
                
                // 패키지 설치 완료를 위한 이벤트 핸들러
                EditorApplication.update += WaitForPackageInstallation;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[패키지 설치] {_currentPackage} 설치 요청 중 오류: {ex.Message}");
                
                // 오류가 발생하더라도 다음 패키지로 진행
                EditorApplication.delayCall += ProcessNextPackage;
            }
        }
        
        private static void WaitForPackageInstallation()
        {
            // 타임아웃 확인 (Git 패키지는 더 오래 걸릴 수 있음)
            float timeoutSeconds = _isGitPackage ? 180f : 60f;
            if ((DateTime.Now - _installationStartTime).TotalSeconds > timeoutSeconds)
            {
                Debug.LogWarning($"[패키지 설치] {_currentPackage} 설치 시간 초과");
                EditorApplication.update -= WaitForPackageInstallation;
                
                // 다음 패키지로 이동
                DelayedProcessNextPackage();
                return;
            }
            
            if (_currentRequest == null || !_currentRequest.IsCompleted)
                return;
            
            EditorApplication.update -= WaitForPackageInstallation;
            
            if (_currentRequest.Status == StatusCode.Success)
            {
                Debug.Log($"[패키지 설치] {_currentPackage} 설치 성공");
            }
            else
            {
                Debug.LogError($"[패키지 설치] {_currentPackage} 설치 실패: {_currentRequest.Error?.message}");
            }
            
            // 다음 패키지로 이동 (충분한 지연 후)
            DelayedProcessNextPackage();
        }
        
        private static void DelayedProcessNextPackage()
        {
            // Git 패키지는 더 오래 기다림
            int delayFrames = _isGitPackage ? 100 : 50;
            
            void DelayedCall(int remainingFrames)
            {
                if (remainingFrames <= 0)
                {
                    ProcessNextPackage();
                    return;
                }
                
                EditorApplication.delayCall += () => DelayedCall(remainingFrames - 1);
            }
            
            EditorApplication.delayCall += () => DelayedCall(delayFrames);
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
                    
                    // 여기서는 명시적으로 리프레시를 하지 않음
                    // 패키지 설치 과정에서 Unity가 자동으로 변경 사항을 감지
                }
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[패키지 설치] OpenUPM 레지스트리 설정 중 오류: {e.Message}");
                return false;
            }
        }
        
        [MenuItem("ActFit/Utilities/Create Required Folders")]
        public static void CreateRequiredFolders()
        {
            string[] folders = {
                "Plugins",
                "Plugins/Android",
                "Plugins/iOS",
                "Editor",
                "Editor/ProjectSettings"
            };
            
            bool foldersCreated = false;
            
            foreach (var folder in folders)
            {
                string path = Path.Combine(Application.dataPath, folder);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    Debug.Log($"[폴더 생성] {path}");
                    foldersCreated = true;
                }
            }
            
            if (foldersCreated)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                EditorUtility.DisplayDialog("완료", "필요한 폴더가 생성되었습니다.", "확인");
            }
            else
            {
                EditorUtility.DisplayDialog("알림", "모든 필요한 폴더가 이미 존재합니다.", "확인");
            }
        }
        
        [MenuItem("ActFit/Utilities/Fix Package Issues")]
        public static void FixPackageIssues()
        {
            if (EditorUtility.DisplayDialog("패키지 문제 해결", 
                "패키지 매니저 문제를 해결하시겠습니까? 이 작업은 다음을 수행합니다:\n\n" +
                "1. 필요한 폴더 생성\n" +
                "2. 패키지 캐시 정리\n" +
                "3. Unity 재시작\n\n" +
                "계속하시겠습니까?", 
                "실행", "취소"))
            {
                // 필요한 폴더 생성
                CreateRequiredFolders();
                
                // Temp 폴더 정리
                string tempPath = Path.Combine(Application.dataPath, "../Temp");
                if (Directory.Exists(tempPath))
                {
                    try
                    {
                        Directory.Delete(tempPath, true);
                        Debug.Log("[패키지 복구] Temp 폴더 삭제 완료");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[패키지 복구] Temp 폴더 삭제 중 오류: {ex.Message}");
                    }
                }
                
                // 에디터 재시작
                if (EditorUtility.DisplayDialog("Unity 재시작", 
                    "패키지 문제 해결을 위해 Unity를 재시작해야 합니다.\n\n" +
                    "지금 재시작하시겠습니까?", 
                    "재시작", "나중에"))
                {
                    string projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                    EditorApplication.OpenProject(projectPath);
                }
            }
        }
    }
}
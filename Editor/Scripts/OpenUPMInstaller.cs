// SequentialPackageInstaller.cs
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
    public class SequentialPackageInstaller : EditorWindow
    {
        private List<string> _openUPMPackages = new List<string>();
        private List<string> _gitPackages = new List<string>();
        private Queue<string> _installQueue = new Queue<string>();
        private bool _isInstalling = false;
        private string _currentPackage = "";
        private float _progress = 0f;
        private string _statusMessage = "";
        private bool _showLogs = true;
        private Vector2 _logScrollPosition;
        private List<string> _logs = new List<string>();
        private bool _autoClose = true;
        
        [MenuItem("ActFit/Safe Package Installer")]
        public static void ShowWindow()
        {
            var window = GetWindow<SequentialPackageInstaller>();
            window.titleContent = new GUIContent("패키지 설치");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }
        
        private void OnEnable()
        {
            // 기본 패키지 목록 설정
            _openUPMPackages = new List<string>
            {
                "jp.hadashikick.vcontainer",
                "com.cysharp.unitask",
                "com.coffee.softmask-for-ugui",
                "com.coffee.ui-effect",
                "com.coffee.ui-particle",
                "com.cysharp.zstring"
            };
            
            _gitPackages = new List<string>
            {
                "https://github.com/HuiSungz/UnityProjectCore.git"
            };
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("패키지 설치 도구", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // 패키지 목록 표시
            EditorGUILayout.LabelField("OpenUPM 패키지", EditorStyles.boldLabel);
            for (int i = 0; i < _openUPMPackages.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _openUPMPackages[i] = EditorGUILayout.TextField(_openUPMPackages[i]);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    _openUPMPackages.RemoveAt(i);
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
            }
            
            if (GUILayout.Button("OpenUPM 패키지 추가"))
            {
                _openUPMPackages.Add("");
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Git 패키지", EditorStyles.boldLabel);
            for (int i = 0; i < _gitPackages.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _gitPackages[i] = EditorGUILayout.TextField(_gitPackages[i]);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    _gitPackages.RemoveAt(i);
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
            }
            
            if (GUILayout.Button("Git 패키지 추가"))
            {
                _gitPackages.Add("");
            }
            
            EditorGUILayout.Space();
            
            _autoClose = EditorGUILayout.Toggle("설치 완료 후 자동 닫기", _autoClose);
            
            EditorGUILayout.Space();
            
            // 현재 상태 표시
            if (_isInstalling)
            {
                EditorGUILayout.LabelField($"진행 중: {_currentPackage}");
                
                if (GUILayout.Button("설치 취소"))
                {
                    CancelInstallation();
                }
            }
            else
            {
                if (GUILayout.Button("설치 시작"))
                {
                    StartInstallation();
                }
            }
            
            EditorGUILayout.Space();
            
            // 로그 표시
            _showLogs = EditorGUILayout.Foldout(_showLogs, "설치 로그");
            if (_showLogs)
            {
                _logScrollPosition = EditorGUILayout.BeginScrollView(_logScrollPosition, GUILayout.Height(150));
                foreach (var log in _logs)
                {
                    EditorGUILayout.HelpBox(log, MessageType.None);
                }
                EditorGUILayout.EndScrollView();
                
                if (GUILayout.Button("로그 지우기"))
                {
                    _logs.Clear();
                }
            }
        }
        
        private void StartInstallation()
        {
            if (_isInstalling)
                return;
                
            _isInstalling = true;
            _progress = 0f;
            _installQueue.Clear();
            _logs.Clear();
            
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
                    _installQueue.Enqueue(package);
                }
            }
            
            LogMessage("설치를 시작합니다. 총 " + _installQueue.Count + "개 패키지");
            
            // 모든 OpenUPM 패키지에 대한 레지스트리 설정 한 번에 처리
            EnsureOpenUPMRegistryForAllPackages();
            
            // 첫 번째 패키지 설치 시작
            EditorApplication.delayCall += ProcessNextPackage;
        }
        
        private void ProcessNextPackage()
        {
            if (!_isInstalling || _installQueue.Count == 0)
            {
                if (_isInstalling)
                {
                    CompleteInstallation();
                }
                return;
            }
            
            _currentPackage = _installQueue.Dequeue();
            _progress = 1f - ((float)_installQueue.Count / (_openUPMPackages.Count + _gitPackages.Count));
            _statusMessage = $"설치 중: {_currentPackage} ({Math.Floor(_progress * 100)}%)";
            
            LogMessage($"패키지 설치 시작: {_currentPackage}");
            
            // 패키지 설치 요청
            var request = Client.Add(_currentPackage);
            
            // 설치 완료 확인을 위한 이벤트 핸들러 등록
            EditorApplication.update += WaitForPackageInstallation;
            
            // 마지막 요청 기록
            _lastRequest = request;
            _installationStartTime = DateTime.Now;
            
            // 윈도우 갱신 요청
            Repaint();
        }
        
        private DateTime _installationStartTime;
        private Request _lastRequest;
        private const float TimeoutInSeconds = 60f;
        
        private void WaitForPackageInstallation()
        {
            // 타임아웃 확인
            if ((DateTime.Now - _installationStartTime).TotalSeconds > TimeoutInSeconds)
            {
                LogMessage($"패키지 설치 시간 초과: {_currentPackage}");
                EditorApplication.update -= WaitForPackageInstallation;
                
                // 다음 패키지로 이동
                EditorApplication.delayCall += ProcessNextPackage;
                return;
            }
            
            if (_lastRequest == null || !_lastRequest.IsCompleted)
                return;
                
            EditorApplication.update -= WaitForPackageInstallation;
            
            if (_lastRequest.Status == StatusCode.Success)
            {
                LogMessage($"패키지 설치 성공: {_currentPackage}");
            }
            else
            {
                LogMessage($"패키지 설치 실패: {_currentPackage} - {_lastRequest.Error?.message}");
            }
            
            // 각 패키지 설치 후 약간의 지연 추가
            EditorApplication.delayCall += () => 
            {
                // 한 번의 프레임만 대기 후 다음 패키지 진행
                EditorApplication.delayCall += ProcessNextPackage;
            };
        }
        
        private void CancelInstallation()
        {
            if (!_isInstalling)
                return;
                
            _isInstalling = false;
            _installQueue.Clear();
            EditorApplication.update -= WaitForPackageInstallation;
            
            LogMessage("설치가 사용자에 의해 취소되었습니다.");
        }
        
        private void CompleteInstallation()
        {
            _isInstalling = false;
            _currentPackage = "";
            _progress = 1f;
            _statusMessage = "설치 완료";
            
            LogMessage("모든 패키지 설치가 완료되었습니다.");
            
            if (_autoClose)
            {
                // 3초 후 창 닫기
                EditorApplication.delayCall += () => 
                {
                    EditorApplication.delayCall += () => 
                    {
                        EditorApplication.delayCall += () => 
                        {
                            Close();
                        };
                    };
                };
            }
        }
        
        private void LogMessage(string message)
        {
            _logs.Add($"[{DateTime.Now.ToString("HH:mm:ss")}] {message}");
            Debug.Log($"[패키지 설치] {message}");
            Repaint();
        }
        
        private bool EnsureOpenUPMRegistryForAllPackages()
        {
            string manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
            
            if (!File.Exists(manifestPath))
            {
                LogMessage("manifest.json 파일을 찾을 수 없습니다: " + manifestPath);
                return false;
            }

            try
            {
                string json = File.ReadAllText(manifestPath);
                bool needsUpdate = false;
                
                // OpenUPM 레지스트리가 없는 경우 새로 추가
                if (!json.Contains("https://package.openupm.com"))
                {
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
                    
                    // 스코프 문자열 구성
                    string scopesJson = "";
                    foreach (var scope in scopes)
                    {
                        scopesJson += $"        \"{scope}\"";
                        if (scopes.Count > 1 && scope != scopes.Last())
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
                    foreach (var package in _openUPMPackages)
                    {
                        if (string.IsNullOrEmpty(package))
                            continue;
                            
                        // 패키지 이름에서 스코프 추출
                        var parts = package.Split('.');
                        if (parts.Length < 2)
                            continue;
                            
                        string scope = $"{parts[0]}.{parts[1]}";
                        
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
                    
                    // 이 부분에서 AssetDatabase.Refresh 호출 안 함!
                    LogMessage("OpenUPM 레지스트리 구성이 업데이트되었습니다.");
                }
                
                return true;
            }
            catch (Exception e)
            {
                LogMessage($"OpenUPM 레지스트리 설정 중 오류 발생: {e.Message}");
                return false;
            }
        }
    }
}
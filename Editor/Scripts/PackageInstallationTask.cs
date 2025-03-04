
using System;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ActionFit.PackageInstaller
{
    internal class PackageInstallationTask
    {
        private Request _currentRequest;
        private DateTime _startTime;
        private readonly string _packageName;
        private readonly bool _isGitPackage;
        private readonly float _timeoutSeconds = 30f;

        public PackageInstallationTask(string packageIdentifier)
        {
            if (packageIdentifier.StartsWith("git:"))
            {
                _isGitPackage = true;
                _packageName = packageIdentifier[4..]; // "git:" 제거
            }
            else
            {
                _isGitPackage = false;
                _packageName = packageIdentifier;
            }
        }

        // 설치 시작 후 완료 여부(true: 성공, false: 실패)를 콜백으로 전달합니다.
        public void Start(Action<bool> onCompleted)
        {
            Debug.Log($"[패키지 설치] {_packageName} 설치 요청...");
            
            try
            {
                EditorUtility.DisplayProgressBar("패키지 설치", $"설치 중: {_packageName}", 0);
                _currentRequest = Client.Add(_packageName);
                _startTime = DateTime.Now;
                EditorApplication.update += MonitorInstallation;

                void MonitorInstallation()
                {
                    // 타임아웃 확인
                    if ((DateTime.Now - _startTime).TotalSeconds > _timeoutSeconds)
                    {
                        Debug.LogWarning($"[패키지 설치] {_packageName} 설치 시간 초과");
                        EditorApplication.update -= MonitorInstallation;
                        onCompleted(false);
                        return;
                    }

                    if (_currentRequest is not { IsCompleted: true })
                    {
                        return;
                    }

                    EditorApplication.update -= MonitorInstallation;
                    if (_currentRequest.Status == StatusCode.Success)
                    {
                        Debug.Log($"[패키지 설치] {_packageName} 설치 성공");
                        onCompleted(true);
                    }
                    else
                    {
                        Debug.LogError($"[패키지 설치] {_packageName} 설치 실패: {_currentRequest.Error?.message}");
                        onCompleted(false);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[패키지 설치] {_packageName} 설치 요청 중 오류: {exception.Message}");
                onCompleted(false);
            }
        }

        public bool IsGitPackage()
        {
            return _isGitPackage;
        }
    }
}
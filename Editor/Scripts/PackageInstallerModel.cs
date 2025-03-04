
using System.Collections.Generic;
using System.Threading;

namespace ActionFit.PackageInstaller
{
    public class PackageInstallerModel
    {
        private List<string> _openUPMPackages = new();
        private List<string> _gitPackages = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public IReadOnlyList<string> OpenUPMPackages => _openUPMPackages;
        public IReadOnlyList<string> GitPackages => _gitPackages;
        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        public void SetOpenUPMPackages(List<string> packages)
        {
            _openUPMPackages = packages;
        }

        public void SetGitPackages(List<string> packages)
        {
            _gitPackages = packages;
        }

        public void Cancel()
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
        }
    }
}
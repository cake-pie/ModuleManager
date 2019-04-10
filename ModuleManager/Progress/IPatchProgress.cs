using System;

namespace ModuleManager.Progress
{
    public interface IPatchProgress
    {
        ProgressCounter Counter { get; }

        float ProgressFraction { get; }

        EventVoid OnPatchApplied { get; }
        EventData<IPass> OnPassStarted { get; }

        void Warning(UrlDir.UrlConfig url, string message);
        void Error(UrlDir.UrlConfig url, string message);
        void Error(string message);
        void Exception(string message, Exception exception);
        void Exception(UrlDir.UrlConfig url, string message, Exception exception);
        void KspVersionUnsatisfiedRoot(UrlDir.UrlConfig url);
        void KspVersionUnsatisfiedNode(UrlDir.UrlConfig url, string path);
        void KspVersionUnsatisfiedValue(UrlDir.UrlConfig url, string path);
        void NeedsUnsatisfiedRoot(UrlDir.UrlConfig url);
        void NeedsUnsatisfiedNode(UrlDir.UrlConfig url, string path);
        void NeedsUnsatisfiedValue(UrlDir.UrlConfig url, string path);
        void NeedsUnsatisfiedBefore(UrlDir.UrlConfig url);
        void NeedsUnsatisfiedFor(UrlDir.UrlConfig url);
        void NeedsUnsatisfiedAfter(UrlDir.UrlConfig url);
        void ApplyingCopy(IUrlConfigIdentifier original, UrlDir.UrlConfig patch);
        void ApplyingDelete(IUrlConfigIdentifier original, UrlDir.UrlConfig patch);
        void ApplyingUpdate(IUrlConfigIdentifier original, UrlDir.UrlConfig patch);
        void PatchAdded();
        void PatchApplied();
        void PassStarted(IPass pass);
    }
}

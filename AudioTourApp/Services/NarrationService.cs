using System.Globalization;
using Microsoft.Maui.ApplicationModel;
#if ANDROID
using Android.OS;
using AndroidTextToSpeech = Android.Speech.Tts.TextToSpeech;
using AndroidUtteranceProgressListener = Android.Speech.Tts.UtteranceProgressListener;
using AndroidLocale = Java.Util.Locale;
using AndroidVoice = Android.Speech.Tts.Voice;
#endif

namespace AudioTourApp.Services;

public sealed class NarrationService : IAsyncDisposable
{
#if ANDROID
    private AndroidTextToSpeech? _textToSpeech;
    private TaskCompletionSource<bool>? _initTcs;
    private readonly SemaphoreSlim _initGate = new(1, 1);
    private TaskCompletionSource<bool>? _speakTcs;
    private string? _currentUtteranceId;
    private string _lastInitializationMessage = "";
    private DateTime _lastInitializationFailureUtc = DateTime.MinValue;
#endif

    public async Task<NarrationResult> SpeakAsync(string? text, string language, string? voiceName = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return NarrationResult.Failed("Khong co noi dung TTS de doc.");
        }

#if ANDROID
        var init = await EnsureInitializedAsync(cancellationToken);
        if (!init.Ready || _textToSpeech == null)
        {
            return NarrationResult.Failed(init.Message);
        }

        var configureVoiceResult = ConfigureVoice(language, voiceName);
        if (!configureVoiceResult.Played)
        {
            return configureVoiceResult;
        }

        _textToSpeech.SetPitch(1.0f);
        _textToSpeech.SetSpeechRate(1.0f);

        var utteranceId = Guid.NewGuid().ToString("N");
        var speakTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _currentUtteranceId = utteranceId;
        _speakTcs = speakTcs;

        using var ctr = cancellationToken.Register(() =>
        {
            _textToSpeech.Stop();
            speakTcs.TrySetCanceled(cancellationToken);
        });

        var result = _textToSpeech.Speak(text, Android.Speech.Tts.QueueMode.Flush, null, utteranceId);
        if (result == Android.Speech.Tts.OperationResult.Error)
        {
            _currentUtteranceId = null;
            _speakTcs = null;
            return NarrationResult.Failed("Engine TTS khong bat dau doc duoc.");
        }

        try
        {
            await speakTcs.Task.WaitAsync(cancellationToken);
            return NarrationResult.Success();
        }
        catch (System.OperationCanceledException)
        {
            return NarrationResult.Interrupted("Da dung TTS.");
        }
        catch (Exception ex)
        {
            return NarrationResult.Failed(ex.Message);
        }
#else
        return NarrationResult.Failed("Nen tang hien tai chua ho tro TTS native.");
#endif
    }

    public Task StopAsync()
    {
#if ANDROID
        _textToSpeech?.Stop();
        _speakTcs?.TrySetCanceled();
        _currentUtteranceId = null;
        _speakTcs = null;
#endif
        return Task.CompletedTask;
    }

    public async Task<string> GetDiagnosticsAsync(string language, string? voiceName = null, CancellationToken cancellationToken = default)
    {
#if ANDROID
        var init = await EnsureInitializedAsync(cancellationToken);
        if (!init.Ready || _textToSpeech == null)
        {
            return $"TTS: {init.Message}";
        }

        var locale = ResolveLocale(language);
        var localeTag = locale?.ToLanguageTag() ?? "mac dinh";
        var availableVoices = _textToSpeech.Voices?.Count ?? 0;
        var preferredVoice = ResolveVoice(language, voiceName);
        var availability = locale == null
            ? "khong co locale"
            : _textToSpeech.IsLanguageAvailable(locale).ToString();

        return preferredVoice != null
            ? $"TTS: san sang | locale {localeTag} | availability {availability} | voice {preferredVoice.Name} | total voices {availableVoices}"
            : $"TTS: san sang | locale {localeTag} | availability {availability} | khong tim thay voice cu the, se dung locale mac dinh | total voices {availableVoices}";
#else
        return "TTS: nen tang hien tai chua ho tro chan doan TTS native.";
#endif
    }

    public ValueTask DisposeAsync()
    {
#if ANDROID
        _textToSpeech?.Stop();
        _textToSpeech?.Shutdown();
        _textToSpeech?.Dispose();
#endif
        return ValueTask.CompletedTask;
    }

#if ANDROID
    public NarrationService()
    {
    }

    private async Task<TtsInitializationResult> EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_textToSpeech == null &&
            !string.IsNullOrWhiteSpace(_lastInitializationMessage) &&
            _lastInitializationFailureUtc > DateTime.MinValue &&
            DateTime.UtcNow - _lastInitializationFailureUtc < TimeSpan.FromSeconds(20))
        {
            return TtsInitializationResult.Failed(_lastInitializationMessage);
        }

        await _initGate.WaitAsync(cancellationToken);
        try
        {
            if (_textToSpeech == null)
            {
                _initTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _lastInitializationMessage = "Dang khoi tao TTS...";
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _textToSpeech = new AndroidTextToSpeech(
                        Android.App.Application.Context,
                        new InitListener(_initTcs, SetProgressListener, message => _lastInitializationMessage = message));
                });
            }
        }
        finally
        {
            _initGate.Release();
        }

        if (_initTcs == null)
        {
            return TtsInitializationResult.Failed("Khong tao duoc tien trinh khoi tao TTS.");
        }

        try
        {
            if (!await _initTcs.Task.WaitAsync(TimeSpan.FromSeconds(12), cancellationToken))
            {
                return MarkInitializationFailure("Khoi tao TTS qua lau, vui long thu lai.");
            }

            _lastInitializationMessage = "TTS da san sang.";
            _lastInitializationFailureUtc = DateTime.MinValue;
            return TtsInitializationResult.Success(_lastInitializationMessage);
        }
        catch (TimeoutException)
        {
            return MarkInitializationFailure("Khoi tao TTS qua lau, vui long thu lai.");
        }
        catch (System.OperationCanceledException)
        {
            return MarkInitializationFailure("Khoi tao TTS bi huy.");
        }
        catch (Exception ex)
        {
            return MarkInitializationFailure(ex.GetBaseException().Message);
        }
    }

    private TtsInitializationResult MarkInitializationFailure(string message)
    {
        _lastInitializationMessage = message;
        _lastInitializationFailureUtc = DateTime.UtcNow;
        return TtsInitializationResult.Failed(message);
    }

    private void SetProgressListener()
    {
        if (_textToSpeech == null)
        {
            return;
        }

        _textToSpeech.SetOnUtteranceProgressListener(new ProgressListener(
            onDone: utteranceId =>
            {
                if (utteranceId == _currentUtteranceId)
                {
                    _speakTcs?.TrySetResult(true);
                }
            },
            onError: utteranceId =>
            {
                if (utteranceId == _currentUtteranceId)
                {
                    _speakTcs?.TrySetException(new InvalidOperationException("TTS bao loi khi doc."));
                }
            }));
    }

    private static AndroidLocale? ResolveLocale(string language)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                return AndroidLocale.Default;
            }

            var normalized = language.Replace('_', '-');
            var parts = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return new AndroidLocale(parts[0], parts[1]);
            }

            if (parts.Length == 1)
            {
                return new AndroidLocale(parts[0]);
            }
        }
        catch
        {
            // Ignore and fall back below.
        }

        return AndroidLocale.Default;
    }

    private NarrationResult ConfigureVoice(string language, string? voiceName)
    {
        if (_textToSpeech == null)
        {
            return NarrationResult.Failed("Thiet bi khong khoi tao duoc engine TTS.");
        }

        var preferredVoice = ResolveVoice(language, voiceName);
        if (preferredVoice != null && Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
        {
            var voiceResult = _textToSpeech.SetVoice(preferredVoice);
            if (voiceResult != Android.Speech.Tts.OperationResult.Error)
            {
                return NarrationResult.Success();
            }
        }

        var locale = ResolveLocale(language);
        if (locale == null)
        {
            return NarrationResult.Failed($"Khong tim thay locale TTS cho {language}.");
        }

        var languageResult = _textToSpeech.SetLanguage(locale);
        if (languageResult is Android.Speech.Tts.LanguageAvailableResult.MissingData or Android.Speech.Tts.LanguageAvailableResult.NotSupported)
        {
            return NarrationResult.Failed(!string.IsNullOrWhiteSpace(voiceName)
                ? $"Thiet bi chua ho tro voice '{voiceName}' hoac ngon ngu {language}."
                : $"Thiet bi chua ho tro voice {language}.");
        }

        return NarrationResult.Success();
    }

    private AndroidVoice? ResolveVoice(string language, string? voiceName)
    {
        if (_textToSpeech == null || Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
        {
            return null;
        }

        var voices = _textToSpeech.Voices;
        if (voices == null || voices.Count == 0)
        {
            return null;
        }

        var normalizedLanguage = NormalizeLanguage(language);
        var languageRoot = normalizedLanguage.Split('-')[0];

        if (!string.IsNullOrWhiteSpace(voiceName))
        {
            var exactVoice = voices
                .Cast<AndroidVoice>()
                .FirstOrDefault(v => string.Equals(v.Name, voiceName, StringComparison.OrdinalIgnoreCase));
            if (exactVoice != null)
            {
                return exactVoice;
            }

            var partialVoice = voices
                .Cast<AndroidVoice>()
                .FirstOrDefault(v => v.Name?.Contains(voiceName, StringComparison.OrdinalIgnoreCase) == true);
            if (partialVoice != null)
            {
                return partialVoice;
            }
        }

        var exactLanguageVoice = voices
            .Cast<AndroidVoice>()
            .FirstOrDefault(v => string.Equals(v.Locale?.ToLanguageTag(), normalizedLanguage, StringComparison.OrdinalIgnoreCase));
        if (exactLanguageVoice != null)
        {
            return exactLanguageVoice;
        }

        return voices
            .Cast<AndroidVoice>()
            .FirstOrDefault(v => v.Locale?.Language?.Equals(languageRoot, StringComparison.OrdinalIgnoreCase) == true);
    }

    private static string NormalizeLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return "vi-VN";
        }

        return language.Replace('_', '-');
    }

    private sealed class InitListener : Java.Lang.Object, AndroidTextToSpeech.IOnInitListener
    {
        private readonly TaskCompletionSource<bool> _tcs;
        private readonly Action _onReady;
        private readonly Action<string> _onStatus;

        public InitListener(TaskCompletionSource<bool> tcs, Action onReady, Action<string> onStatus)
        {
            _tcs = tcs;
            _onReady = onReady;
            _onStatus = onStatus;
        }

        public void OnInit(Android.Speech.Tts.OperationResult status)
        {
            if (status == Android.Speech.Tts.OperationResult.Success)
            {
                _onStatus("TTS da khoi tao thanh cong.");
                _onReady();
                _tcs.TrySetResult(true);
                return;
            }

            _onStatus($"Khoi tao TTS that bai: {status}.");
            _tcs.TrySetResult(false);
        }
    }

    private sealed class ProgressListener : AndroidUtteranceProgressListener
    {
        private readonly Action<string> _onDone;
        private readonly Action<string> _onError;

        public ProgressListener(Action<string> onDone, Action<string> onError)
        {
            _onDone = onDone;
            _onError = onError;
        }

        public override void OnStart(string? utteranceId)
        {
        }

        public override void OnDone(string? utteranceId)
        {
            if (!string.IsNullOrWhiteSpace(utteranceId))
            {
                _onDone(utteranceId);
            }
        }

        [Obsolete]
        public override void OnError(string? utteranceId)
        {
            if (!string.IsNullOrWhiteSpace(utteranceId))
            {
                _onError(utteranceId);
            }
        }
    }
#endif
}

public readonly record struct NarrationResult(bool Played, bool WasCompleted, string Message)
{
    public static NarrationResult Success(string message = "Da doc bang TTS.") => new(true, true, message);

    public static NarrationResult Interrupted(string message = "Da dung TTS.") => new(true, false, message);

    public static NarrationResult Failed(string message) => new(false, false, message);
}

internal readonly record struct TtsInitializationResult(bool Ready, string Message)
{
    public static TtsInitializationResult Success(string message) => new(true, message);

    public static TtsInitializationResult Failed(string message) => new(false, message);
}

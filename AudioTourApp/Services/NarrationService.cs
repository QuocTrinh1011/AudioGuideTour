using System.Globalization;
#if ANDROID
using AndroidTextToSpeech = Android.Speech.Tts.TextToSpeech;
using AndroidUtteranceProgressListener = Android.Speech.Tts.UtteranceProgressListener;
using AndroidLocale = Java.Util.Locale;
#endif

namespace AudioTourApp.Services;

public sealed class NarrationService : IAsyncDisposable
{
#if ANDROID
    private readonly AndroidTextToSpeech? _textToSpeech;
    private readonly TaskCompletionSource<bool> _initTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private TaskCompletionSource<bool>? _speakTcs;
    private string? _currentUtteranceId;
#endif

    public async Task<NarrationResult> SpeakAsync(string? text, string language, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return NarrationResult.Failed("Khong co noi dung TTS de doc.");
        }

#if ANDROID
        if (_textToSpeech == null)
        {
            return NarrationResult.Failed("Thiet bi khong khoi tao duoc engine TTS.");
        }

        if (!await _initTcs.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken))
        {
            return NarrationResult.Failed("Khoi tao TTS qua lau, vui long thu lai.");
        }

        var locale = ResolveLocale(language);
        if (locale == null)
        {
            return NarrationResult.Failed($"Khong tim thay locale TTS cho {language}.");
        }

        var languageResult = _textToSpeech.SetLanguage(locale);
        if (languageResult is Android.Speech.Tts.LanguageAvailableResult.MissingData or Android.Speech.Tts.LanguageAvailableResult.NotSupported)
        {
            return NarrationResult.Failed($"Thiet bi chua ho tro voice {language}.");
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
        catch (OperationCanceledException)
        {
            return NarrationResult.Failed("Da dung TTS.");
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
        _textToSpeech = new AndroidTextToSpeech(Android.App.Application.Context, new InitListener(_initTcs, SetProgressListener));
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

    private sealed class InitListener : Java.Lang.Object, AndroidTextToSpeech.IOnInitListener
    {
        private readonly TaskCompletionSource<bool> _tcs;
        private readonly Action _onReady;

        public InitListener(TaskCompletionSource<bool> tcs, Action onReady)
        {
            _tcs = tcs;
            _onReady = onReady;
        }

        public void OnInit(Android.Speech.Tts.OperationResult status)
        {
            if (status == Android.Speech.Tts.OperationResult.Success)
            {
                _onReady();
                _tcs.TrySetResult(true);
                return;
            }

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

public readonly record struct NarrationResult(bool Played, string Message)
{
    public static NarrationResult Success(string message = "Da doc bang TTS.") => new(true, message);

    public static NarrationResult Failed(string message) => new(false, message);
}

#if ANDROID
using Android.Content;
using Android.Media;
#endif

namespace AudioTourApp.Services;

#pragma warning disable CA1422

public partial class AudioInterruptionService
{
    public event EventHandler<string>? Interrupted;

#if ANDROID
    private readonly AudioManager? _audioManager;
    private FocusChangeListener? _focusChangeListener;
    private bool _hasFocus;
#endif

    public AudioInterruptionService()
    {
#if ANDROID
        _audioManager = Android.App.Application.Context.GetSystemService(Context.AudioService) as AudioManager;
#endif
    }

    public Task<bool> BeginPlaybackAsync()
    {
#if ANDROID
        if (_audioManager == null)
        {
            return Task.FromResult(true);
        }

        _focusChangeListener ??= new FocusChangeListener(reason => Interrupted?.Invoke(this, reason));
        var result = _audioManager.RequestAudioFocus(_focusChangeListener, Android.Media.Stream.Music, AudioFocus.GainTransient);
        _hasFocus = result == AudioFocusRequest.Granted;
        return Task.FromResult(_hasFocus);
#else
        return Task.FromResult(true);
#endif
    }

    public Task EndPlaybackAsync()
    {
#if ANDROID
        if (_audioManager != null && _focusChangeListener != null && _hasFocus)
        {
            _audioManager.AbandonAudioFocus(_focusChangeListener);
        }

        _hasFocus = false;
#endif
        return Task.CompletedTask;
    }

#if ANDROID
    private sealed class FocusChangeListener : Java.Lang.Object, AudioManager.IOnAudioFocusChangeListener
    {
        private readonly Action<string> _onInterrupted;

        public FocusChangeListener(Action<string> onInterrupted)
        {
            _onInterrupted = onInterrupted;
        }

        public void OnAudioFocusChange(AudioFocus focusChange)
        {
            if (focusChange is AudioFocus.Loss or AudioFocus.LossTransient or AudioFocus.LossTransientCanDuck)
            {
                _onInterrupted.Invoke("Audio đang bị gián đoạn bởi hệ thống hoặc ứng dụng khác.");
            }
        }
    }
#endif
}

#pragma warning restore CA1422

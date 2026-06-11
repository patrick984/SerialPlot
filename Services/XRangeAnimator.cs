using System;

namespace SerialPlot.Services;

public sealed class XRangeAnimator
{
    public static readonly TimeSpan DefaultDuration = TimeSpan.FromMilliseconds(300);

    private XRange _start;
    private XRange _target;
    private DateTime _startedAtUtc;

    public bool IsActive { get; private set; }
    public XRange? Target => IsActive ? _target : null;

    public void Reset()
    {
        IsActive = false;
    }

    public void Retarget(XRange current, XRange target, DateTime nowUtc, TimeSpan? duration = null)
    {
        if (!IsFinite(current) || !IsFinite(target))
        {
            Reset();
            return;
        }

        _start = current;
        _target = target;
        _startedAtUtc = nowUtc;
        Duration = duration ?? DefaultDuration;
        IsActive = true;
    }

    public TimeSpan Duration { get; private set; } = DefaultDuration;

    public XRange Tick(DateTime nowUtc)
    {
        if (!IsActive)
        {
            return _target;
        }

        var progress = Duration.TotalMilliseconds <= 0
            ? 1
            : Math.Clamp((nowUtc - _startedAtUtc).TotalMilliseconds / Duration.TotalMilliseconds, 0, 1);
        var eased = 1 - Math.Pow(1 - progress, 3);
        var range = new XRange(
            Lerp(_start.Minimum, _target.Minimum, eased),
            Lerp(_start.Maximum, _target.Maximum, eased));

        if (progress >= 1)
        {
            IsActive = false;
            return _target;
        }

        return range;
    }

    private static double Lerp(double start, double end, double amount)
        => start + ((end - start) * amount);

    private static bool IsFinite(XRange range)
        => double.IsFinite(range.Minimum) && double.IsFinite(range.Maximum) && range.Maximum > range.Minimum;
}

using System;
using System.Diagnostics;
using Avalonia.Threading;

namespace Ambit.Avalonia.Controls;

/// <summary>
/// Smoothly interpolates viewport pan/zoom toward a target using exponential smoothing.
/// </summary>
internal sealed class ViewportAnimator : IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly Func<(double zoom, double panX, double panY)> _getCurrent;
    private readonly Func<(double zoom, double panX, double panY), (double zoom, double panX, double panY)> _coerceTarget;
    private readonly Action<double, double, double, bool> _apply; // zoom, panX, panY, raiseChanged

    private double _targetZoom = 1.0;
    private double _targetPanX;
    private double _targetPanY;
    private long _lastTick;

    public ViewportAnimator(
        Func<(double zoom, double panX, double panY)> getCurrent,
        Func<(double zoom, double panX, double panY), (double zoom, double panX, double panY)> coerceTarget,
        Action<double, double, double, bool> apply)
    {
        _getCurrent = getCurrent;
        _coerceTarget = coerceTarget;
        _apply = apply;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += OnTick;
    }

    public bool IsRunning => _timer.IsEnabled;

    public void AnimateTo(double zoom, double panX, double panY)
    {
        if (!double.IsFinite(zoom) || !double.IsFinite(panX) || !double.IsFinite(panY)) return;
        zoom = Math.Clamp(zoom, 0.1, 20.0);
        var coerced = _coerceTarget((zoom, panX, panY));
        _targetZoom = coerced.zoom;
        _targetPanX = coerced.panX;
        _targetPanY = coerced.panY;
        _lastTick = Stopwatch.GetTimestamp();
        if (!_timer.IsEnabled) _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
        var cur = _getCurrent();
        _targetZoom = cur.zoom;
        _targetPanX = cur.panX;
        _targetPanY = cur.panY;
    }

    public void SyncTargetToCurrent()
    {
        var cur = _getCurrent();
        _targetZoom = cur.zoom;
        _targetPanX = cur.panX;
        _targetPanY = cur.panY;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var now = Stopwatch.GetTimestamp();
        var dt = _lastTick == 0 ? 1d / 60d : (now - _lastTick) / (double)Stopwatch.Frequency;
        _lastTick = now;

        var cur = _getCurrent();
        var smoothing = 1d - Math.Exp(-18d * dt);
        var nextZoom = cur.zoom + ((_targetZoom - cur.zoom) * smoothing);
        var nextPanX = cur.panX + ((_targetPanX - cur.panX) * smoothing);
        var nextPanY = cur.panY + ((_targetPanY - cur.panY) * smoothing);

        var done = Math.Abs(_targetZoom - nextZoom) < 0.0005
            && Math.Abs(_targetPanX - nextPanX) < 0.25
            && Math.Abs(_targetPanY - nextPanY) < 0.25;

        var finalZoom = done ? _targetZoom : nextZoom;
        var finalPanX = done ? _targetPanX : nextPanX;
        var finalPanY = done ? _targetPanY : nextPanY;
        _apply(finalZoom, finalPanX, finalPanY, true);

        if (done) _timer.Stop();
    }

    public void Dispose() => _timer.Tick -= OnTick;
}

/**
 * video-interop.js
 *
 * Manages a native <video> element injected into the Avalonia WASM host
 * container (#out).  The element sits behind Avalonia's canvas so that
 * annotations drawn by RegionEditorControl appear on top of the video.
 *
 * All transform math replicates the PanZoomCoordinateTransform used on
 * the C# side so video and annotations stay pixel-perfect aligned.
 */

/** @type {HTMLVideoElement | null} */
let _video = null;

/** Dimensions of the video content (used for fit-scale calculation). */
let _contentWidth = 1920;
let _contentHeight = 1080;

// ── Public API ────────────────────────────────────────────────────────────

/**
 * Creates the <video> element and appends it to the Avalonia host div.
 * @param {string} src         URL of the video asset (relative to the app root).
 * @param {number} contentW    Native width  of the video (e.g. 1920).
 * @param {number} contentH    Native height of the video (e.g. 1080).
 */
export function createVideo(src, contentW, contentH) {
    destroyVideo();

    _contentWidth  = contentW  > 0 ? contentW  : 1920;
    _contentHeight = contentH > 0 ? contentH : 1080;

    const host = document.getElementById('out');
    if (!host) {
        console.error('[video-interop] Host element #out not found.');
        return;
    }

    // Make the host a positioning context if it isn't already.
    const hostStyle = getComputedStyle(host);
    if (hostStyle.position === 'static') {
        host.style.position = 'relative';
    }

    _video = document.createElement('video');
    _video.id      = 'ambit-video';
    _video.src     = src;
    _video.loop    = true;
    _video.muted   = true;          // required for autoplay in most browsers
    _video.autoplay = true;
    _video.controls = false;        // Avalonia UI provides the controls panel
    _video.playsInline = true;      // iOS requires this

    Object.assign(_video.style, {
        position:         'absolute',
        top:              '0',
        left:             '0',
        // Start at 0×0; setTransform will size and position it correctly.
        width:            '0px',
        height:           '0px',
        transformOrigin:  '0 0',
        zIndex:           '0',      // behind Avalonia canvas (z-index 1)
        pointerEvents:    'none',   // all pointer events go to the canvas
        objectFit:        'fill',   // we handle scaling ourselves via transform
        background:       '#000',
    });

    // Push Avalonia's canvas above the video.
    const canvas = host.querySelector('canvas');
    if (canvas) {
        canvas.style.zIndex = '1';
        canvas.style.position = 'relative';
    }

    // Insert before the canvas so DOM order keeps canvas on top.
    if (canvas) {
        host.insertBefore(_video, canvas);
    } else {
        host.appendChild(_video);
    }

    _video.play().catch(err => {
        console.warn('[video-interop] Autoplay blocked:', err.message);
    });
}

/**
 * Applies the pan/zoom transform so the video matches what the Avalonia
 * PanZoomCoordinateTransform computes for the overlay canvas.
 *
 * @param {number} zoom      Current zoom level (1.0 = fit-to-canvas).
 * @param {number} panX      Pan offset in CSS pixels.
 * @param {number} panY      Pan offset in CSS pixels.
 * @param {number} ctrlW     Current width  of the host container in CSS px.
 * @param {number} ctrlH     Current height of the host container in CSS px.
 * @param {number} ctrlX     Current X position of the host container in CSS px.
 * @param {number} ctrlY     Current Y position of the host container in CSS px.
 */
export function setTransform(zoom, panX, panY, ctrlW, ctrlH, ctrlX = 0, ctrlY = 0) {
    if (!_video) return;

    // Replicate PanZoomCoordinateTransform.GetBaseImageRect()
    const scale = Math.min(ctrlW / _contentWidth, ctrlH / _contentHeight);
    const baseW = _contentWidth  * scale;
    const baseH = _contentHeight * scale;
    const baseX = (ctrlW - baseW) / 2.0;
    const baseY = (ctrlH - baseH) / 2.0;

    // Apply zoom around the canvas centre, then add pan offset and container screen offset
    const cx = ctrlW / 2.0;
    const cy = ctrlH / 2.0;
    const tx = (baseX - cx) * zoom + cx + panX + ctrlX;
    const ty = (baseY - cy) * zoom + cy + panY + ctrlY;

    const scaleX = (baseW * zoom) / _contentWidth;
    const scaleY = (baseH * zoom) / _contentHeight;

    // Size the element at native resolution; CSS transform does the rest.
    _video.style.width  = `${_contentWidth}px`;
    _video.style.height = `${_contentHeight}px`;
    _video.style.transform = `matrix(${scaleX},0,0,${scaleY},${tx},${ty})`;
}

/** Pauses playback. */
export function pauseVideo() {
    _video?.pause();
}

/** Resumes playback. */
export function playVideo() {
    _video?.play().catch(() => {});
}

/** Removes the video element and releases the media resource. */
export function destroyVideo() {
    if (_video) {
        _video.pause();
        _video.src = '';
        _video.remove();
        _video = null;
    }
}

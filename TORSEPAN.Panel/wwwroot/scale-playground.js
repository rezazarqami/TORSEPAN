(() => {
    "use strict";

    const semitones = { C: 0, D: 2, E: 4, F: 5, G: 7, A: 9, B: 11 };
    const accents = new WeakMap();
    const voices = new Set();
    let context;
    let output;

    function frequency(pitch) {
        const note = /^([A-G])([#b]?)([2-6])$/i.exec(pitch);
        if (!note) return null;
        const accidental = note[2] === "#" ? 1 : note[2] === "b" ? -1 : 0;
        const midi = (Number(note[3]) + 1) * 12 + semitones[note[1].toUpperCase()] + accidental;
        if (midi < 41 || midi > 85) return null;
        return 440 * 2 ** ((midi - 69) / 12);
    }

    function playSynth(pitch) {
        const fundamental = frequency(pitch);
        const AudioEngine = window.AudioContext || window.webkitAudioContext;
        if (!fundamental || !AudioEngine) return;

        if (!context) {
            context = new AudioEngine();
            output = context.createDynamicsCompressor();
            output.connect(context.destination);
        }
        if (context.state === "suspended") context.resume().catch(() => {});

        const now = context.currentTime;
        if (voices.size >= 12) {
            const oldest = voices.values().next().value;
            oldest.stop(now + 0.025);
            voices.delete(oldest);
        }

        const bus = context.createGain();
        bus.connect(output);
        const oscillators = [];
        // Fundamental, octave and octave plus a pure fifth (ratios 1:2:3).
        const modes = [[1, .32, 2.15], [2, .12, 1.05], [3, .065, .67]];
        for (const [ratio, volume, duration] of modes) {
            if (fundamental * ratio >= context.sampleRate / 2) continue;
            const oscillator = context.createOscillator();
            const envelope = context.createGain();
            oscillator.frequency.value = fundamental * ratio;
            envelope.gain.setValueAtTime(.0001, now);
            envelope.gain.exponentialRampToValueAtTime(volume, now + .007);
            envelope.gain.exponentialRampToValueAtTime(.0001, now + duration);
            oscillator.connect(envelope).connect(bus);
            oscillator.start(now);
            oscillator.stop(now + duration + .02);
            oscillators.push(oscillator);
        }

        const voice = {
            stop(at) {
                bus.gain.setValueAtTime(1, context.currentTime);
                bus.gain.linearRampToValueAtTime(0, at);
                for (const oscillator of oscillators) oscillator.stop(at);
            }
        };
        voices.add(voice);
        let remaining = oscillators.length;
        for (const oscillator of oscillators) {
            oscillator.onended = () => {
                if (--remaining === 0) {
                    bus.disconnect();
                    voices.delete(voice);
                }
            };
        }
    }

    document.addEventListener("click", event => {
        const button = event.target.closest("button[data-scale-pitch]");
        if (!button) return;
        const recording = button.querySelector("audio");
        const pitch = button.dataset.scalePitch;
        if (recording) {
            const player = recording.paused ? recording : new Audio(recording.src);
            player.currentTime = 0;
            player.play().catch(() => playSynth(pitch));
        } else {
            playSynth(pitch);
        }

        button.classList.add("playing");
        clearTimeout(accents.get(button));
        accents.set(button, setTimeout(() => button.classList.remove("playing"), 155));
    });

    window.scalePlayground = {
        position(element, x, y) {
            const rect = element.getBoundingClientRect();
            const percent = (coordinate, start, length) => Math.max(0, Math.min(100, (coordinate - start) / length * 100));
            return [percent(x, rect.left, rect.width), percent(y, rect.top, rect.height)];
        }
    };
})();

(() => {
    "use strict";

    const semitones = { C: 0, D: 2, E: 4, F: 5, G: 7, A: 9, B: 11 };
    // Measurements from the isolated portions of Torsepan's D3, F#3, C#4 and A4 recordings.
    // Each mode has a relative peak, time to peak, early decay constant, and tail decay constant.
    const profiles = [
        { midi: 50, modes: [[1, .010, .34, .53], [.14, .115, .22, 1.18], [.038, .137, .14, .74]] },
        { midi: 54, modes: [[1, .009, .17, .75], [.51, .118, .25, .80], [.096, .142, .15, .39]] },
        { midi: 61, modes: [[1, .008, .28, .81], [.26, .117, .16, 1.04], [.04, .138, .10, .42]] },
        { midi: 69, modes: [[1, .009, .14, .52], [.50, .064, .18, .46], [.39, .126, .13, .22]] }
    ];
    const accents = new WeakMap();
    const voices = new Set();
    let context;
    let output;

    function midiFor(pitch) {
        const note = /^([A-G])([#b]?)([2-6])$/i.exec(pitch);
        if (!note) return null;
        const accidental = note[2] === "#" ? 1 : note[2] === "b" ? -1 : 0;
        const midi = (Number(note[3]) + 1) * 12 + semitones[note[1].toUpperCase()] + accidental;
        if (midi < 41 || midi > 85) return null;
        return midi;
    }

    function modesFor(midi) {
        const upperIndex = profiles.findIndex(profile => profile.midi >= midi);
        if (upperIndex <= 0) return profiles[upperIndex === 0 ? 0 : profiles.length - 1].modes;
        const upper = profiles[upperIndex];
        const lower = profiles[upperIndex - 1];
        const mix = (midi - lower.midi) / (upper.midi - lower.midi);
        return lower.modes.map((mode, index) =>
            mode.map((value, parameter) => value + mix * (upper.modes[index][parameter] - value)));
    }

    function playSynth(pitch) {
        const midi = midiFor(pitch);
        if (midi === null) return;
        const fundamental = 440 * 2 ** ((midi - 69) / 12);
        const AudioEngine = window.AudioContext || window.webkitAudioContext;
        if (!AudioEngine) return;

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
        // Keep the three tuned modes at exactly 1:2:3 while reproducing their separate attacks and tails.
        const modes = modesFor(midi);
        for (const [index, [relativePeak, attack, earlyDecay, tailDecay]] of modes.entries()) {
            const oscillator = context.createOscillator();
            const envelope = context.createGain();
            const peak = relativePeak * .30;
            const floor = .00008;
            const earlyEnd = now + attack + .17;
            const tailStart = Math.max(floor, peak * Math.exp(-.17 / earlyDecay));
            const end = earlyEnd + Math.min(4, tailDecay * Math.log(tailStart / floor));

            oscillator.frequency.value = fundamental * (index + 1);
            envelope.gain.setValueAtTime(floor, now);
            envelope.gain.exponentialRampToValueAtTime(peak, now + attack);
            envelope.gain.exponentialRampToValueAtTime(tailStart, earlyEnd);
            envelope.gain.exponentialRampToValueAtTime(floor, end);
            oscillator.connect(envelope).connect(bus);
            oscillator.start(now);
            oscillator.stop(end + .02);
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

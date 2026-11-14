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
    const sampleDirectory = new URL("./handpan-samples/", document.currentScript.src);
    const highNoteStart = 76; // E5
    const lowNoteEnd = 65; // F4
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

    function noteGain(midi) {
        // Boost low and middle notes progressively, then taper into the highest register.
        const gainDb = midi <= 69 ? 9 - (midi - 41) * .21 : 3.12 - Math.min(7, midi - 69) * (7.12 / 7);
        // F2 through D#4 are the lower half of the bank. Ease into the upper half.
        const bassBoostDb = 7 * Math.max(0, Math.min(1, (64 - midi) / 5));
        return 10 ** ((gainDb + bassBoostDb) / 20);
    }

    function noteDuration(midi) {
        if (midi <= 50) return 7; // Through D3
        return midi <= lowNoteEnd ? 5 : 4;
    }

    function applyNoteDecay(gain, peak, start, duration) {
        // Keep the initial strike, then fade throughout the note's sustain.
        const fadeEnd = start + duration;
        gain.setValueAtTime(peak, start + .025);
        gain.exponentialRampToValueAtTime(peak * .01, fadeEnd - .12);
        gain.linearRampToValueAtTime(0, fadeEnd);
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

    function prepareAudio() {
        const AudioEngine = window.AudioContext || window.webkitAudioContext;
        if (!AudioEngine) return false;
        if (!context) {
            context = new AudioEngine();
            output = context.createDynamicsCompressor();
            output.connect(context.destination);
        }
        if (context.state === "suspended") context.resume().catch(() => {});
        return true;
    }

    function makeRoom(now) {
        if (voices.size >= 12) {
            const oldest = voices.values().next().value;
            oldest.stop(now + .025);
            voices.delete(oldest);
        }
    }

    function playBankNote(pitch) {
        const midi = midiFor(pitch);
        if (midi === null) return;

        const player = new Audio(new URL(`abtin/${midi}.flac`, sampleDirectory));
        player.play().catch(() => playSynth(pitch));
    }

    function playSynth(pitch) {
        const midi = midiFor(pitch);
        if (midi === null || !prepareAudio()) return;
        const fundamental = 440 * 2 ** ((midi - 69) / 12);
        const now = context.currentTime;
        makeRoom(now);

        const bus = context.createGain();
        bus.connect(output);
        const duration = noteDuration(midi);
        bus.gain.setValueAtTime(1, now);
        applyNoteDecay(bus.gain, 1, now, duration);
        const oscillators = [];
        const highNote = midi >= highNoteStart;
        const profile = modesFor(midi);
        const modes = highNote ? profile.slice(0, 2) : profile;
        // Reinforce the upper modes below G3 so small phone speakers can reproduce the strike.
        const bassSupport = Math.max(0, Math.min(1, (65 - midi) / 24));
        const minimumBassPeaks = [1, .76, .42];
        for (const [index, [relativePeak, attack, earlyDecay, tailDecay]] of modes.entries()) {
            const oscillator = context.createOscillator();
            const envelope = context.createGain();
            const supportedPeak = relativePeak + bassSupport * Math.max(0, minimumBassPeaks[index] - relativePeak);
            const peak = supportedPeak * (.30 + .05 * bassSupport) * noteGain(midi);
            const attackTime = attack * (1 - .6 * bassSupport);
            const floor = .00008;
            const earlyEnd = now + attackTime + .17;
            const tailStart = Math.max(floor, peak * Math.exp(-.17 / (earlyDecay * 1.35)));
            const sustain = tailDecay * 1.65 + .28 * bassSupport;
            const end = now + duration;
            const tailLevel = Math.max(floor, tailStart * Math.exp(-(duration - (earlyEnd - now)) / sustain));

            oscillator.frequency.value = fundamental * (index + 1);
            envelope.gain.setValueAtTime(floor, now);
            envelope.gain.exponentialRampToValueAtTime(peak, now + attackTime);
            envelope.gain.exponentialRampToValueAtTime(tailStart, earlyEnd);
            envelope.gain.exponentialRampToValueAtTime(tailLevel, end);
            oscillator.connect(envelope).connect(bus);
            oscillator.start(now);
            oscillator.stop(end);
            oscillators.push(oscillator);
        }

        const voice = {
            stop(at) {
                bus.gain.cancelAndHoldAtTime(context.currentTime);
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
            player.play().catch(() => playBankNote(pitch));
        } else {
            playBankNote(pitch);
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

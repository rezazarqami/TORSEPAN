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
    const highNoteSample = "69-octave-sustain";
    const lowNoteEnd = 65; // F4
    const sampleBytes = new Map();
    const sampleBuffers = new Map();
    const bassBuffers = new Map();
    const bassLoads = new Map();
    const accents = new WeakMap();
    const notesPlayedOnTouch = new WeakSet();
    const touchesWaitingForAudio = new Map();
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

    function prepareAudio(resume = true) {
        const AudioEngine = window.AudioContext || window.webkitAudioContext;
        if (!AudioEngine) return false;
        if (!context) {
            context = new AudioEngine();
            output = context.createDynamicsCompressor();
            output.connect(context.destination);
        }
        if (resume && context.state === "suspended") context.resume().catch(() => {});
        return true;
    }

    function makeRoom(now) {
        if (voices.size >= 12) {
            const oldest = voices.values().next().value;
            oldest.stop(now + .025);
            voices.delete(oldest);
        }
    }

    function fetchSample(sample) {
        if (!sampleBytes.has(sample)) {
            const bytes = fetch(new URL(`${sample}.mp3`, sampleDirectory))
                .then(response => {
                    if (!response.ok) throw new Error("Handpan sample unavailable");
                    return response.arrayBuffer();
                });
            sampleBytes.set(sample, bytes);
            bytes.catch(() => sampleBytes.delete(sample));
        }
        return sampleBytes.get(sample);
    }

    function loadBassSample(midi) {
        if (bassBuffers.has(midi) || bassLoads.has(midi)) return;

        const load = fetch(new URL(`abtin/${midi}.flac`, sampleDirectory))
            .then(response => {
                if (!response.ok) throw new Error("Handpan sample unavailable");
                return response.arrayBuffer();
            })
            .then(bytes => context.decodeAudioData(bytes))
            .then(buffer => bassBuffers.set(midi, buffer))
            .catch(() => {})
            .finally(() => bassLoads.delete(midi));
        bassLoads.set(midi, load);
    }

    function playBassNote(pitch, midi) {
        const buffer = bassBuffers.get(midi);
        if (!buffer) {
            loadBassSample(midi);
            playSynth(pitch);
            return;
        }

        const now = context.currentTime;
        makeRoom(now);
        const source = context.createBufferSource();
        const bus = context.createGain();
        source.buffer = buffer;
        source.connect(bus).connect(context.destination);
        const voice = {
            stop(at) {
                bus.gain.setValueAtTime(1, context.currentTime);
                bus.gain.linearRampToValueAtTime(0, at);
                source.stop(at);
            }
        };
        voices.add(voice);
        source.onended = () => {
            bus.disconnect();
            voices.delete(voice);
        };
        source.start(now);
    }

    async function playBankNote(pitch) {
        const midi = midiFor(pitch);
        if (midi === null || !prepareAudio()) return;

        if (midi <= 56) { // F2 through G#3
            playBassNote(pitch, midi);
            return;
        }

        const anchor = profiles.reduce((nearest, item) =>
            Math.abs(item.midi - midi) < Math.abs(nearest.midi - midi) ? item : nearest).midi;
        const highNote = midi >= highNoteStart;
        // The octave-only sample uses the same A4 recording and preserves its strike.
        const sample = highNote ? highNoteSample : `${anchor}-sustain`;
        try {
            if (!sampleBuffers.has(sample)) {
                const buffer = fetchSample(sample).then(bytes => context.decodeAudioData(bytes.slice(0)));
                sampleBuffers.set(sample, buffer);
                buffer.catch(() => sampleBuffers.delete(sample));
            }
            const buffer = await sampleBuffers.get(sample);
            const now = context.currentTime;
            makeRoom(now);

            const source = context.createBufferSource();
            const bus = context.createGain();
            source.buffer = buffer;
            source.playbackRate.value = 2 ** ((midi - anchor) / 12);
            const deepBass = midi <= 50; // D3
            const volume = (deepBass ? .94 : .72) * noteGain(midi);
            bus.gain.setValueAtTime(volume, now);
            const clarityGain = 10 * Math.max(0, (69 - midi) / 28)
                + 5 * Math.max(0, (64 - midi) / 23);
            if (clarityGain > 0) {
                const clarity = context.createBiquadFilter();
                clarity.type = "highshelf";
                clarity.frequency.value = 440 * 2 ** ((midi - 69) / 12) * 1.8;
                clarity.gain.value = clarityGain;
                source.connect(clarity).connect(bus);
            } else {
                source.connect(bus);
            }
            bus.connect(output);
            const voice = {
                stop(at) {
                    bus.gain.cancelAndHoldAtTime(context.currentTime);
                    bus.gain.linearRampToValueAtTime(0, at);
                    source.stop(at);
                }
            };
            voices.add(voice);
            source.onended = () => {
                bus.disconnect();
                voices.delete(voice);
            };
            const duration = noteDuration(midi);
            applyNoteDecay(bus.gain, volume, now, duration);
            source.start(now);
            source.stop(now + duration);
        } catch {
            playSynth(pitch);
        }
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

    const practiceUrl = new URL("./handpan-practice/01-g2-hijaz.json", document.currentScript.src);
    const practiceRuns = new WeakMap();
    let practiceScore;

    function loadPracticeScore() {
        practiceScore ??= fetch(practiceUrl).then(response => {
            if (!response.ok) throw new Error("Practice score unavailable");
            return response.json();
        }).catch(error => {
            practiceScore = undefined;
            throw error;
        });
        return practiceScore;
    }

    function practiceStatus(root, message) {
        root.querySelector("[data-practice-status]").textContent = message;
    }

    function showPracticeCue(root, pitches = []) {
        const wanted = new Set(pitches.map(midiFor));
        for (const button of root.querySelectorAll("button[data-scale-pitch]")) {
            button.classList.toggle("practice-cue", wanted.has(midiFor(button.dataset.scalePitch)));
        }
    }

    function stopPractice(root) {
        clearTimeout(practiceRuns.get(root)?.timer);
        practiceRuns.delete(root);
        showPracticeCue(root);
    }

    function setStep(root, run) {
        const step = run.steps[run.index];
        if (!step) {
            stopPractice(root);
            practiceStatus(root, "تمرین کامل شد.");
            return;
        }
        run.remaining = new Set(step.notes.map(midiFor));
        run.firstHit = undefined;
        showPracticeCue(root, step.notes);
        practiceStatus(root, step.notes.length > 1 ? "نت‌های روشن را با هم بزن." : "نت روشن را بزن.");
    }

    function acceptPracticeStrike(button) {
        const root = button.closest("[data-handpan-practice]");
        const run = root && practiceRuns.get(root);
        if (run?.mode !== "step" || !run.remaining) return;
        const now = performance.now();
        if (run.firstHit !== undefined && now - run.firstHit > 180) setStep(root, run);
        if (!run.remaining.delete(midiFor(button.dataset.scalePitch))) return;
        run.firstHit ??= now;
        if (run.remaining.size === 0) {
            run.index += 1;
            setStep(root, run);
        }
    }

    async function startPractice(root, mode) {
        stopPractice(root);
        const run = { mode, index: 0 };
        practiceRuns.set(root, run);
        const selected = root.querySelector("[data-practice-phrase]").value;
        const speed = Number(root.querySelector("[data-practice-speed]").value) || 1;
        practiceStatus(root, "در حال آماده‌سازی تمرین…");
        if (mode === "listen") prepareAudio();

        try {
            const { phrases } = await loadPracticeScore();
            if (!root.isConnected || practiceRuns.get(root) !== run) return;
            run.steps = selected === "all" ? phrases.flat() : phrases[Number(selected)];
            if (!run.steps?.length) throw new Error("Practice phrase unavailable");
            if (mode === "step") {
                setStep(root, run);
                return;
            }

            practiceStatus(root, mode === "listen" ? "در حال پخش تمرین…" : "با روشن شدن نت‌ها همراهی کن.");
            const origin = selected === "all" ? 0 : run.steps[0].ms;
            const started = performance.now() + (mode === "guide" ? 750 : 0);
            function advance() {
                if (!root.isConnected || practiceRuns.get(root) !== run) return;
                const step = run.steps[run.index++];
                if (!step) {
                    stopPractice(root);
                    practiceStatus(root, "تمرین به پایان رسید.");
                    return;
                }
                showPracticeCue(root, step.notes);
                if (mode === "listen") for (const pitch of step.notes) playBankNote(pitch);
                scheduleNext();
            }
            function scheduleNext() {
                const next = run.steps[run.index];
                const delay = next ? started + (next.ms - origin) / speed - performance.now() : 500;
                run.timer = setTimeout(advance, Math.max(0, delay));
            }
            scheduleNext();
        } catch {
            if (practiceRuns.get(root) !== run) return;
            stopPractice(root);
            practiceStatus(root, "بارگذاری تمرین انجام نشد. دوباره امتحان کن.");
        }
    }

    document.addEventListener("click", event => {
        const control = event.target.closest("button[data-practice-action]");
        const root = control?.closest("[data-handpan-practice]");
        if (!root) return;
        const mode = control.dataset.practiceAction;
        if (mode === "stop") {
            stopPractice(root);
            practiceStatus(root, "تمرین متوقف شد.");
        } else {
            startPractice(root, mode);
        }
    });

    document.addEventListener("change", event => {
        const control = event.target.closest("[data-practice-phrase], [data-practice-speed]");
        const root = control?.closest("[data-handpan-practice]");
        if (!root || !practiceRuns.has(root)) return;
        stopPractice(root);
        practiceStatus(root, "برای شروع دوباره یک حالت تمرین را انتخاب کن.");
    });

    function strikeNote(button) {
        acceptPracticeStrike(button);
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
    }

    document.addEventListener("pointerdown", event => {
        const button = event.target.closest('button[data-scale-multitouch="true"]');
        if (!button || button.disabled) return;

        if (event.pointerType !== "touch") {
            notesPlayedOnTouch.delete(button);
            return;
        }

        // Each finger has its own pointer. Play on contact, including non-primary touches.
        notesPlayedOnTouch.add(button);
        if (navigator.userActivation?.hasBeenActive === false || touchesWaitingForAudio.size > 0) {
            touchesWaitingForAudio.set(event.pointerId, button);
            return;
        }
        strikeNote(button);
    });

    // On a fresh page some browsers only unlock audio when the first touch ends.
    document.addEventListener("pointerup", event => {
        if (!touchesWaitingForAudio.has(event.pointerId)) return;
        const startingNotes = [...touchesWaitingForAudio.values()];
        touchesWaitingForAudio.clear();
        for (const button of startingNotes) strikeNote(button);
    });

    document.addEventListener("pointercancel", event => {
        touchesWaitingForAudio.delete(event.pointerId);
    });

    document.addEventListener("click", event => {
        const button = event.target.closest("button[data-scale-pitch]");
        if (!button || button.disabled) return;

        // A touch can also produce a native click on release. Keyboard clicks have zero detail.
        if (event.detail > 0 && notesPlayedOnTouch.delete(button)) return;
        strikeNote(button);
    });

    window.scalePlayground = {
        preload() {
            for (const { midi } of profiles) fetchSample(`${midi}-sustain`).catch(() => {});
            fetchSample(highNoteSample).catch(() => {});
            if (prepareAudio(false)) {
                for (let midi = 41; midi <= 56; midi++) loadBassSample(midi);
            }
        },
        position(element, x, y) {
            const rect = element.getBoundingClientRect();
            const percent = (coordinate, start, length) => Math.max(0, Math.min(100, (coordinate - start) / length * 100));
            return [percent(x, rect.left, rect.width), percent(y, rect.top, rect.height)];
        }
    };
})();

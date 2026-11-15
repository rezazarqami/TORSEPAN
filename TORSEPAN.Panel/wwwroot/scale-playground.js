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
    const bassPlaybackGain = .5; // Reserve 6 dB of headroom for bass samples and overlapping strikes.
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

    function playBassNote(pitch, midi, articulation) {
        const buffer = bassBuffers.get(midi);
        if (!buffer) {
            loadBassSample(midi);
            if (!articulation) playSynth(pitch, bassPlaybackGain);
            return;
        }

        const now = context.currentTime;
        makeRoom(now);
        const source = context.createBufferSource();
        const bus = context.createGain();
        source.buffer = buffer;
        const level = bassPlaybackGain * (articulation === "P" ? .65 : 1);
        bus.gain.setValueAtTime(level, now);
        let mutedTail;
        if (articulation) {
            const muted = context.createBiquadFilter();
            muted.type = "lowpass";
            muted.frequency.value = articulation === "P" ? 450 : 1200;
            source.connect(muted).connect(bus);
            mutedTail = articulation === "P" ? .13 : .19;
            bus.gain.setValueAtTime(level, now + .025);
            bus.gain.exponentialRampToValueAtTime(.001, now + mutedTail);
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
        source.start(now);
        if (mutedTail) source.stop(now + mutedTail + .01);
    }

    async function playBankNote(pitch, muted = false) {
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
            const duration = muted ? .18 : noteDuration(midi);
            applyNoteDecay(bus.gain, volume, now, duration);
            source.start(now);
            source.stop(now + duration);
        } catch {
            playSynth(pitch);
        }
    }

    function playSynth(pitch, volume = 1) {
        const midi = midiFor(pitch);
        if (midi === null || !prepareAudio()) return;
        const fundamental = 440 * 2 ** ((midi - 69) / 12);
        const now = context.currentTime;
        makeRoom(now);

        const bus = context.createGain();
        bus.connect(output);
        const duration = noteDuration(midi);
        bus.gain.setValueAtTime(volume, now);
        applyNoteDecay(bus.gain, volume, now, duration);
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

    const practiceDirectory = new URL("./handpan-practice/", document.currentScript.src);
    const practiceRuns = new WeakMap();
    const practiceScores = new Map();
    const preparedExercise = new WeakMap();

    function loadPracticeScore(name) {
        if (!practiceScores.has(name)) {
            const load = fetch(new URL(`${name}.json`, practiceDirectory)).then(response => {
                if (!response.ok) throw new Error("Practice score unavailable");
                return response.json();
            }).catch(error => {
                practiceScores.delete(name);
                throw error;
            });
            practiceScores.set(name, load);
        }
        return practiceScores.get(name);
    }

    function practiceStatus(root, message) {
        root.querySelector("[data-practice-status]").textContent = message;
    }

    const practicePercussion = {
        T: { sample: "finger-tap.wav" },
        K: { sample: "body-knock.wav" },
        P: { mutedDing: true },
        DM: { mutedDing: true, label: "Dᴹ" },
        S: { sample: "body-knock.wav", highpass: 950, rate: 1.35 },
        "•": { sample: "soft-touch.wav" }
    };
    const percussionBuffers = new Map();

    function loadPercussion(sample) {
        if (!percussionBuffers.has(sample)) {
            const buffer = fetch(new URL(sample, practiceDirectory))
                .then(response => {
                    if (!response.ok) throw new Error("Body strike sample unavailable");
                    return response.arrayBuffer();
                })
                .then(bytes => context.decodeAudioData(bytes))
                .catch(error => {
                    percussionBuffers.delete(sample);
                    throw error;
                });
            percussionBuffers.set(sample, buffer);
        }
        return percussionBuffers.get(sample);
    }

    function playPracticeHit(hit) {
        if (!Object.hasOwn(practicePercussion, hit)) {
            playBankNote(hit);
            return;
        }
        if (!prepareAudio()) return;

        const sound = practicePercussion[hit];
        if (sound.mutedDing) {
            playBassNote("D3", 50, hit);
            return;
        }
        loadPercussion(sound.sample).then(buffer => {
            const source = context.createBufferSource();
            const gain = context.createGain();
            source.buffer = buffer;
            source.playbackRate.value = sound.rate ?? 1;
            gain.gain.value = sound.gain ?? 1;
            if (sound.highpass) {
                const filter = context.createBiquadFilter();
                filter.type = "highpass";
                filter.frequency.value = sound.highpass;
                source.connect(filter).connect(gain);
            } else {
                source.connect(gain);
            }
            gain.connect(output);
            source.onended = () => {
                source.disconnect();
                gain.disconnect();
            };
            source.start();
        }).catch(() => {});
    }

    function practiceHits(step) {
        if (!step) return [];
        return step.percussion ? [...step.notes, step.percussion] : step.notes;
    }

    function practiceHitKey(hit) {
        return Object.hasOwn(practicePercussion, hit) ? hit : midiFor(hit);
    }

    function buttonPracticeHit(button) {
        return button.dataset.practicePercussion ?? button.dataset.scalePitch;
    }

    function showPracticeCue(root, pitches = []) {
        const wanted = new Set(pitches.map(practiceHitKey));
        for (const button of root.querySelectorAll("button[data-scale-pitch], button[data-practice-percussion]")) {
            button.classList.toggle("practice-cue", wanted.has(practiceHitKey(buttonPracticeHit(button))));
        }
    }

    function practiceSelection(root, score) {
        const value = root.querySelector("[data-practice-phrase]").value;
        if (value === "all") return { start: 0, end: score.totalTicks, steps: score.phrases.flat() };
        const phrase = Number(value);
        return {
            start: score.phraseStarts[phrase],
            end: score.phraseStarts[phrase + 1] ?? score.totalTicks,
            steps: score.phrases[phrase]
        };
    }

    async function preparePractice(root) {
        const name = root.dataset.practiceScore;
        if (prepareAudio(false)) {
            loadBassSample(50);
            for (const sample of ["finger-tap.wav", "body-knock.wav", "soft-touch.wav"]) loadPercussion(sample).catch(() => {});
        }
        try {
            const score = await loadPracticeScore(name);
            if (!root.isConnected || name !== root.dataset.practiceScore) return;
            const phrase = root.querySelector("[data-practice-phrase]");
            if (preparedExercise.get(root) !== name) {
                phrase.replaceChildren(new Option("کل تمرین", "all"), ...score.phrases.map((_, index) =>
                    new Option(score.phraseNames?.[index] ?? `جملهٔ ${(index + 1).toLocaleString("fa")}`, index)));
                preparedExercise.set(root, name);
                phrase.value = "all";
            }

            const { start, end, steps } = practiceSelection(root, score);
            const byTick = new Map(steps.map(step => [step.tick, step]));
            const track = root.querySelector("[data-practice-timeline]");
            const grid = document.createElement("div");
            grid.className = "practice-grid";
            const ticksPerBar = score.beatsPerBar * score.subdivisions;
            const pickup = score.pickupTicks ?? 0;
            const firstBar = Math.floor((start + pickup) / ticksPerBar);
            const lastBar = Math.floor((end + pickup - 1) / ticksPerBar);
            for (let bar = firstBar; bar <= lastBar; bar += 1) {
                const row = document.createElement("div");
                row.className = "practice-row";
                row.setAttribute("aria-label", `میزان ${bar + 1}`);
                row.style.setProperty("--ticks-per-bar", ticksPerBar);

                const measure = document.createElement("span");
                measure.className = "practice-measure";
                measure.textContent = bar + 1;
                row.append(measure);

                for (let offset = 0; offset < ticksPerBar; offset += 1) {
                    const tick = bar * ticksPerBar + offset - pickup;
                    const cell = document.createElement("div");
                    cell.className = "practice-tick";
                    if (offset % score.subdivisions === 0) cell.classList.add("is-beat");
                    if (tick >= start && tick < end) {
                        cell.dataset.practiceTick = tick;
                        const step = byTick.get(tick);
                        if (step?.hand) {
                            const hand = document.createElement("span");
                            hand.className = "practice-hand";
                            hand.textContent = step.hand;
                            hand.setAttribute("aria-label", step.hand === "R" ? "دست راست" : "دست چپ");
                            cell.append(hand);
                        }
                        for (const pitch of practiceHits(step)) {
                            const note = document.createElement("span");
                            const percussion = Object.hasOwn(practicePercussion, pitch);
                            note.className = percussion ? "practice-pitch practice-body-hit" : "practice-pitch";
                            note.textContent = percussion ? practicePercussion[pitch].label ?? pitch : step.muted ? `${pitch}ᴹ` : pitch;
                            cell.append(note);
                        }
                    }
                    row.append(cell);
                }
                grid.append(row);
            }
            track.replaceChildren(grid);
            track.scrollTop = 0;
            const tickSeconds = 60 / (score.bpm * score.subdivisions);
            updatePracticeTime(root, start * tickSeconds, score.totalTicks * tickSeconds);
            practiceStatus(root, `${score.bpm} ضرب در دقیقه در سرعت اصلی`);
        } catch {
            if (root.isConnected && name === root.dataset.practiceScore) {
                practiceStatus(root, "بارگذاری تمرین انجام نشد. دوباره امتحان کن.");
            }
        }
    }

    function updatePracticeTime(root, elapsed, total) {
        function format(seconds) {
            const whole = Math.round(seconds);
            return `${Math.floor(whole / 60)}:${String(whole % 60).padStart(2, "0")}`;
        }
        const progress = root.querySelector("[data-practice-progress]");
        progress.max = total || 1;
        progress.value = elapsed;
        root.querySelector("[data-practice-elapsed]").textContent = format(elapsed);
        root.querySelector("[data-practice-duration]").textContent = format(total);
    }

    function movePracticeCursor(root, tick, interval = 0) {
        const track = root.querySelector("[data-practice-timeline]");
        track.querySelector(".is-active")?.classList.remove("is-active");
        const cell = track.querySelector(`[data-practice-tick="${tick}"]`);
        if (!cell) return;
        cell.style.setProperty("--tick-duration", `${interval}ms`);
        cell.classList.add("is-active");
        track.scrollTop = cell.parentElement.offsetTop;
    }

    function stopPractice(root) {
        clearTimeout(practiceRuns.get(root)?.timer);
        practiceRuns.delete(root);
        showPracticeCue(root);
        movePracticeCursor(root);
    }

    function setStep(root, run) {
        const step = run.steps[run.index];
        if (!step) {
            stopPractice(root);
            updatePracticeTime(root, run.end * run.tickSeconds, run.totalSeconds);
            practiceStatus(root, "تمرین کامل شد.");
            return;
        }
        const hits = practiceHits(step);
        run.remaining = new Set(hits.map(practiceHitKey));
        run.firstHit = undefined;
        showPracticeCue(root, hits);
        movePracticeCursor(root, step.tick);
        updatePracticeTime(root, step.tick * run.tickSeconds, run.totalSeconds);
        practiceStatus(root, hits.length > 1 ? "جای ضربه‌های روشن را با هم بزن." :
            step.percussion === "T" ? "ضربهٔ انگشت (T) را بزن." :
            step.percussion === "K" ? "ناک بدنه (K) را بزن." : "نت روشن را بزن.");
    }

    function acceptPracticeStrike(button) {
        const root = button.closest("[data-handpan-practice]");
        const run = root && practiceRuns.get(root);
        if (run?.mode !== "step" || !run.remaining) return;
        const now = performance.now();
        const timeWindow = practiceHits(run.steps[run.index]).length > 2 ? 500 : 180;
        if (run.firstHit !== undefined && now - run.firstHit > timeWindow) setStep(root, run);
        if (!run.remaining.delete(practiceHitKey(buttonPracticeHit(button)))) return;
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
        const speed = Number(root.querySelector("[data-practice-speed]").value) || 1;
        practiceStatus(root, "در حال آماده‌سازی تمرین…");
        if (mode === "listen") prepareAudio();

        try {
            const name = root.dataset.practiceScore;
            if (preparedExercise.get(root) !== name) await preparePractice(root);
            const score = await loadPracticeScore(name);
            if (!root.isConnected || practiceRuns.get(root) !== run || name !== root.dataset.practiceScore) return;
            const { start, end, steps } = practiceSelection(root, score);
            run.steps = steps;
            run.end = end;
            run.tickSeconds = 60 / (score.bpm * score.subdivisions);
            run.totalSeconds = score.totalTicks * run.tickSeconds;
            updatePracticeTime(root, start * run.tickSeconds, run.totalSeconds);
            if (!steps?.length) throw new Error("Practice phrase unavailable");
            if (mode === "step") {
                setStep(root, run);
                return;
            }

            practiceStatus(root, mode === "listen" ? "در حال پخش تمرین…" : "با روشن شدن نت‌ها همراهی کن.");
            const byTick = new Map(steps.map(step => [step.tick, step]));
            const interval = 60000 / (score.bpm * score.subdivisions * speed);
            const started = performance.now() + (mode === "guide" ? 750 : 0);
            let tick = start;
            function advance() {
                if (!root.isConnected || practiceRuns.get(root) !== run) return;
                if (tick >= end) {
                    stopPractice(root);
                    updatePracticeTime(root, end * run.tickSeconds, run.totalSeconds);
                    practiceStatus(root, "تمرین به پایان رسید.");
                    return;
                }
                const step = byTick.get(tick);
                const notes = practiceHits(step);
                movePracticeCursor(root, tick, interval);
                updatePracticeTime(root, tick * run.tickSeconds, run.totalSeconds);
                showPracticeCue(root, notes);
                if (mode === "listen") {
                    for (const hit of notes) {
                        if (step?.muted && step.notes.includes(hit)) playBankNote(hit, true);
                        else playPracticeHit(hit);
                    }
                }
                tick += 1;
                scheduleNext();
            }
            function scheduleNext() {
                const delay = started + (tick - start) * interval - performance.now();
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
        const control = event.target.closest("[data-practice-action]");
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

    document.addEventListener("click", event => {
        const picker = event.target.closest(".practice-picker");
        for (const open of document.querySelectorAll(".practice-picker[open]")) {
            if (open !== picker) open.open = false;
        }
        if (!picker) return;
        const select = picker.querySelector("select");
        const summary = picker.querySelector("summary");
        if (event.target.closest("summary") && !picker.open) {
            const choices = Array.from(select.options, option => {
                const button = document.createElement("button");
                button.type = "button";
                button.dataset.practiceOption = option.value;
                button.textContent = option.textContent;
                button.dir = option.dir || "auto";
                button.setAttribute("aria-pressed", String(option.selected));
                return button;
            });
            picker.querySelector(".practice-options").replaceChildren(...choices);
        }
        const button = event.target.closest("[data-practice-option]");
        if (!button) return;
        picker.open = false;
        summary.focus();
        if (select.value === button.dataset.practiceOption) return;
        select.value = button.dataset.practiceOption;
        summary.querySelector("strong").textContent = button.textContent;
        select.dispatchEvent(new Event("change", { bubbles: true }));
    });

    document.addEventListener("keydown", event => {
        if (event.key !== "Escape") return;
        const picker = event.target.closest(".practice-picker[open]");
        if (!picker) return;
        picker.open = false;
        picker.querySelector("summary").focus();
    });

    document.addEventListener("change", event => {
        const control = event.target.closest("[data-practice-exercise], [data-practice-phrase], [data-practice-speed]");
        const root = control?.closest("[data-handpan-practice]");
        if (!root) return;
        stopPractice(root);
        if (control.matches("[data-practice-exercise]")) {
            const option = control.selectedOptions[0];
            root.dataset.practiceScore = control.value;
            preparedExercise.delete(root);
            root.querySelector("[data-practice-scale]").textContent = option.dataset.scaleName;
            root.querySelector("[data-practice-photo]").hidden = ["18", "21"].includes(option.dataset.notes);
            for (const layout of root.querySelectorAll("[data-practice-layout]")) {
                layout.hidden = layout.dataset.practiceLayout !== option.dataset.notes;
            }
            const extras = option.dataset.extra.split(" ");
            for (const hit of root.querySelectorAll("[data-practice-extra-hit]")) {
                hit.hidden = !extras.includes(hit.dataset.practicePercussion);
            }
            root.querySelector("[data-practice-timeline]").replaceChildren();
            root.querySelector("[data-practice-phrase]").replaceChildren(new Option("کل تمرین", "all"));
            root.querySelector("[data-practice-phrase]").closest("details").querySelector("summary strong").textContent = "کل تمرین";
            updatePracticeTime(root, 0, 0);
            practiceStatus(root, "در حال آماده‌سازی تمرین…");
            preparePractice(root);
        } else if (control.matches("[data-practice-phrase]")) {
            preparePractice(root);
        } else {
            practiceStatus(root, "برای شروع دوباره یک حالت تمرین را انتخاب کن.");
        }
    });

    function strikeNote(button) {
        acceptPracticeStrike(button);
        const recording = button.querySelector("audio");
        const pitch = button.dataset.scalePitch;
        if (button.dataset.practicePercussion) {
            playPracticeHit(button.dataset.practicePercussion);
        } else if (recording) {
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
        const button = event.target.closest("button[data-scale-pitch], button[data-practice-percussion]");
        if (!button || button.disabled) return;

        // A touch can also produce a native click on release. Keyboard clicks have zero detail.
        if (event.detail > 0 && notesPlayedOnTouch.delete(button)) return;
        strikeNote(button);
    });

    window.scalePlayground = {
        preparePractice,
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

import { ClientListener, CombinedController, Sp } from "./clientListener";
import { logTrace, logError } from "../../logging";
import * as fs from "fs";
import * as path from "path";

export class VoiceHudService extends ClientListener {
    private hasInjectedHud = false;
    private lastReadTime = 0;
    private lastTalking = false;
    private lastMode = -1;
    private lastLevel = -1;
    private lastStateTick = 0;
    private readonly voiceStatePath = "./Data/Platform/voice-state.json";

    constructor(private sp: Sp, private controller: CombinedController) {
        super();

        this.controller.emitter.on("browserWindowLoaded", () => {
            this.injectHud();
        });

        this.controller.emitter.on("createActorMessage", (e: any) => {
            if (e && e.message && e.message.isMe) {
                this.sp.Utility.wait(0.6).then(() => {
                    this.injectHud();
                });
            }
        });

        this.controller.on("update", () => {
            if (!this.hasInjectedHud) {
                const player = this.sp.Game.getPlayer();
                if (player && player.getFormID() !== 0) {
                    this.injectHud();
                }
            }

            const now = Date.now();
            if (now - this.lastReadTime >= 45) {
                this.lastReadTime = now;
                this.pollVoiceState();
            }
        });
    }

    private injectHud(): void {
        if (this.hasInjectedHud) return;
        this.hasInjectedHud = true;
        logTrace(this, "Injecting Nordic UI Voice HUD into CEF browser...");

        const hudJs = `
        (function() {
            try {
                if (document.getElementById("skymp-voice-hud-root")) return;

                const style = document.createElement("style");
                style.id = "skymp-voice-hud-style";
                style.textContent = \`
                    #skymp-voice-hud-root {
                        position: fixed;
                        left: 28px;
                        bottom: 42px;
                        z-index: 99999;
                        pointer-events: none;
                        user-select: none;
                        display: flex;
                        align-items: center;
                        gap: 10px;
                        padding: 7px 14px;
                        background: rgba(12, 16, 20, 0.82);
                        border: 1px solid rgba(195, 185, 165, 0.28);
                        box-shadow: 0 4px 18px rgba(0, 0, 0, 0.65), inset 0 0 10px rgba(0, 0, 0, 0.5);
                        border-radius: 2px;
                        font-family: 'Cinzel', 'Trajan Pro', 'Georgia', serif;
                        color: #e2ded4;
                        opacity: 0;
                        transform: translateY(6px);
                        transition: opacity 0.25s cubic-bezier(0.2, 0.9, 0.3, 1), transform 0.25s cubic-bezier(0.2, 0.9, 0.3, 1);
                    }
                    #skymp-voice-hud-root.visible {
                        opacity: 1;
                        transform: translateY(0);
                    }
                    .nordic-corner {
                        position: absolute;
                        width: 4px;
                        height: 4px;
                        border-color: rgba(220, 210, 190, 0.55);
                        border-style: solid;
                    }
                    .top-left { top: -1px; left: -1px; border-width: 1px 0 0 1px; }
                    .top-right { top: -1px; right: -1px; border-width: 1px 1px 0 0; }
                    .bottom-left { bottom: -1px; left: -1px; border-width: 0 0 1px 1px; }
                    .bottom-right { bottom: -1px; right: -1px; border-width: 0 1px 1px 0; }
                    
                    .mic-icon-wrapper {
                        display: flex;
                        align-items: center;
                        justify-content: center;
                        width: 18px;
                        height: 18px;
                    }
                    .mic-svg {
                        width: 15px;
                        height: 15px;
                        fill: #9ea3ac;
                        transition: fill 0.2s, filter 0.2s;
                    }
                    .talking .mic-svg {
                        fill: #f4f1ea;
                        filter: drop-shadow(0 0 5px rgba(244, 241, 234, 0.7));
                    }
                    .mode-label {
                        font-size: 11px;
                        font-weight: 600;
                        letter-spacing: 1.6px;
                        text-transform: uppercase;
                        white-space: nowrap;
                    }
                    .mode-whisper {
                        color: #90caf9;
                        text-shadow: 0 0 8px rgba(144, 202, 249, 0.4);
                    }
                    .mode-normal {
                        color: #e2ded4;
                        text-shadow: 0 0 6px rgba(226, 222, 212, 0.35);
                    }
                    .mode-shout {
                        color: #ff8a65;
                        text-shadow: 0 0 9px rgba(255, 138, 101, 0.6);
                    }
                    .meter-bars {
                        display: flex;
                        align-items: flex-end;
                        gap: 3px;
                        height: 15px;
                        padding-left: 2px;
                    }
                    .meter-bar {
                        width: 3px;
                        height: 3px;
                        background: rgba(180, 175, 160, 0.3);
                        border-radius: 1px;
                        transition: height 0.08s ease-out, background 0.12s;
                    }
                    .meter-bar.active {
                        background: #d4cfbe;
                        box-shadow: 0 0 4px rgba(212, 207, 190, 0.6);
                    }
                    .mode-shout-active .meter-bar.active {
                        background: #ff7043;
                        box-shadow: 0 0 5px rgba(255, 112, 67, 0.8);
                    }
                    .mode-whisper-active .meter-bar.active {
                        background: #64b5f6;
                        box-shadow: 0 0 5px rgba(100, 181, 246, 0.8);
                    }
                \`;
                document.head.appendChild(style);

                const hud = document.createElement("div");
                hud.id = "skymp-voice-hud-root";
                hud.innerHTML = \`
                    <div class="nordic-corner top-left"></div>
                    <div class="nordic-corner top-right"></div>
                    <div class="nordic-corner bottom-left"></div>
                    <div class="nordic-corner bottom-right"></div>
                    <div class="mic-icon-wrapper" id="skymp-voice-mic-wrap">
                        <svg class="mic-svg" viewBox="0 0 24 24">
                            <path d="M12 14c1.66 0 3-1.34 3-3V5c0-1.66-1.34-3-3-3S9 3.34 9 5v6c0 1.66 1.34 3 3 3z"/>
                            <path d="M17 11c0 2.76-2.24 5-5 5s-5-2.24-5-5H5c0 3.53 2.61 6.43 6 6.92V21h2v-3.08c3.39-.49 6-3.39 6-6.92h-2z"/>
                        </svg>
                    </div>
                    <span class="mode-label mode-normal" id="skymp-voice-mode-text">NORMAL [V]</span>
                    <div class="meter-bars" id="skymp-voice-meter">
                        <div class="meter-bar" style="height: 3px;"></div>
                        <div class="meter-bar" style="height: 3px;"></div>
                        <div class="meter-bar" style="height: 3px;"></div>
                        <div class="meter-bar" style="height: 3px;"></div>
                        <div class="meter-bar" style="height: 3px;"></div>
                    </div>
                \`;
                document.body.appendChild(hud);

                let hideTimeout = null;
                let lastModeIndex = -1;

                window.skympVoiceHudUpdate = function(state) {
                    if (!state) return;
                    const root = document.getElementById("skymp-voice-hud-root");
                    const mic = document.getElementById("skymp-voice-mic-wrap");
                    const text = document.getElementById("skymp-voice-mode-text");
                    const meter = document.getElementById("skymp-voice-meter");
                    if (!root || !mic || !text || !meter) return;

                    const isTalking = !!state.talking;
                    const mode = typeof state.mode === "number" ? state.mode : 1;
                    const level = typeof state.level === "number" ? state.level : 0.0;

                    // Mode switch detection (shows HUD for 2.2s even if not talking)
                    const modeChanged = (lastModeIndex !== -1 && lastModeIndex !== mode);
                    lastModeIndex = mode;

                    // Update mode text and style
                    text.className = "mode-label";
                    root.className = "";
                    if (mode === 0) {
                        text.innerText = isTalking ? "FISILTI" : "FISILTI (F8)";
                        text.classList.add("mode-whisper");
                        root.classList.add("mode-whisper-active");
                    } else if (mode === 2) {
                        text.innerText = isTalking ? "BAĞIRMA" : "BAĞIRMA (F8)";
                        text.classList.add("mode-shout");
                        root.classList.add("mode-shout-active");
                    } else {
                        text.innerText = isTalking ? "NORMAL" : "NORMAL (F8)";
                        text.classList.add("mode-normal");
                    }

                    if (isTalking) {
                        mic.classList.add("talking");
                    } else {
                        mic.classList.remove("talking");
                    }

                    // Update real amplitude volume bars (5 bars)
                    const bars = meter.children;
                    const thresholds = [0.03, 0.18, 0.38, 0.60, 0.82];
                    for (let i = 0; i < 5; i++) {
                        const bar = bars[i];
                        if (isTalking && level >= thresholds[i]) {
                            bar.classList.add("active");
                            // Dynamic reactive bar height based on amplitude
                            const barH = Math.min(15, Math.max(4, Math.round(level * 15 * (1 + i * 0.1))));
                            bar.style.height = barH + "px";
                        } else {
                            bar.classList.remove("active");
                            bar.style.height = "3px";
                        }
                    }

                    if (isTalking) {
                        if (hideTimeout) { clearTimeout(hideTimeout); hideTimeout = null; }
                        root.classList.add("visible");
                    } else if (modeChanged) {
                        root.classList.add("visible");
                        if (hideTimeout) clearTimeout(hideTimeout);
                        hideTimeout = setTimeout(function() {
                            root.classList.remove("visible");
                        }, 2200);
                    } else if (!hideTimeout && root.classList.contains("visible")) {
                        hideTimeout = setTimeout(function() {
                            root.classList.remove("visible");
                            hideTimeout = null;
                        }, 400);
                    }
                };

            } catch (err) {
                console.error("Voice HUD injection error:", err);
            }
        })();
        `;

        this.sp.browser.executeJavaScript(hudJs);
    }

    private pollVoiceState(): void {
        try {
            if (!fs.existsSync(this.voiceStatePath)) return;
            const raw = fs.readFileSync(this.voiceStatePath, "utf8");
            if (!raw || raw.length < 5) return;
            const data = JSON.parse(raw);

            const talking = !!data.talking;
            const mode = typeof data.mode === "number" ? data.mode : 1;
            const level = typeof data.level === "number" ? data.level : 0.0;
            const tick = typeof data.t === "number" ? data.t : 0;

            // Only update when something changed or when actively talking
            const hasChanged = (talking !== this.lastTalking) ||
                               (mode !== this.lastMode) ||
                               (talking && Math.abs(level - this.lastLevel) > 0.04) ||
                               (tick !== this.lastStateTick && (talking || mode !== this.lastMode));

            if (hasChanged) {
                this.lastTalking = talking;
                this.lastMode = mode;
                this.lastLevel = level;
                this.lastStateTick = tick;

                const payload = JSON.stringify({ talking, mode, level });
                this.sp.browser.executeJavaScript(`window.skympVoiceHudUpdate && window.skympVoiceHudUpdate(${payload});`);
            }
        } catch (e) {
            // Ignore transient read lock while launcher writes .tmp -> rename
        }
    }
}

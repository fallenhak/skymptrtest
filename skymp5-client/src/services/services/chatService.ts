import { ClientListener, CombinedController, Sp } from "./clientListener";
import { CustomPacketMessage } from "../messages/customPacketMessage";
import { MsgType } from "../../messages";
import { logError, logTrace } from "../../logging";
import { BrowserMessageEvent, DxScanCode, Menu, MenuCloseEvent, MenuOpenEvent } from "skyrimPlatform";

export class ChatService extends ClientListener {
    private hasInitializedWidget = false;
    private badMenusOpen = new Set<string>();

    private readonly badMenus: Menu[] = [
        Menu.Barter,
        Menu.Book,
        Menu.Container,
        Menu.Crafting,
        Menu.Gift,
        Menu.Inventory,
        Menu.Journal,
        Menu.Lockpicking,
        Menu.Loading,
        Menu.Map,
        Menu.RaceSex,
        Menu.Stats,
        Menu.Tween,
        Menu.Console,
        Menu.Main,
    ];

    constructor(private sp: Sp, private controller: CombinedController) {
        super();

        // Gelen sunucu paketlerini dinle
        this.controller.emitter.on("customPacketMessage", (e) => this.onCustomPacketMessage(e));

        // Tarayıcıdan (CEF) gelen mesajları dinle (chatInput vs.)
        this.controller.on("browserMessage", (e) => this.onBrowserMessage(e));

        // Oyuncu dünyada aktifleştiğinde veya arayüz yüklendiğinde sohbeti hazırla
        this.controller.emitter.on("createActorMessage", (e: any) => {
            if (e && e.message && e.message.isMe) {
                // AuthService resetWidgets çağrısından hemen sonra sohbeti başlat
                this.sp.Utility.wait(0.5).then(() => {
                    this.initChatWidget();
                });
            }
        });

        this.controller.emitter.on("browserWindowLoaded", () => {
            this.initChatWidget();
        });

        // Menü durumlarını izle
        this.controller.on("menuOpen", (e) => this.onMenuOpen(e));
        this.controller.on("menuClose", (e) => this.onMenuClose(e));

        // Klavye olayları (Enter ile açma, Escape ile kapatma)
        this.controller.emitter.on("queryKeyCodeBindings", (e) => this.onQueryKeyCodeBindings(e));

        // Yedek kontrol: Oyuncu dünyada varsa başlat
        this.controller.on("update", () => {
            if (!this.hasInitializedWidget) {
                const player = this.sp.Game.getPlayer();
                if (player && player.getFormID() !== 0) {
                    this.hasInitializedWidget = true;
                    this.initChatWidget();
                }
            }
        });
    }

    public initChatWidget(): void {
        logTrace(this, "Initializing in-game React Chat widget...");
        const initJs = `
        (function() {
            try {
                if (!window.skyrimPlatform || !window.skyrimPlatform.widgets) return;
                window.chatMessages = window.chatMessages || [];
                window.chat = [{
                    type: "chat",
                    messages: window.chatMessages,
                    isInputHidden: true,
                    placeholder: "Sohbet etmek icin Enter'a basin (/me, /do, /b, /w, /s, /zar)...",
                    send: function(text) {
                        if (window.skyrimPlatform && window.skyrimPlatform.sendMessage) {
                            window.skyrimPlatform.sendMessage("chatInput", text);
                        }
                    }
                }];
                window.skyrimPlatform.widgets.set(window.chat);

                if (!window.__chatEventsAttached) {
                    window.__chatEventsAttached = true;
                    window.addEventListener("skymp5-client:browserFocused", function() {
                        if (window.chat && window.chat[0]) {
                            window.chat[0].isInputHidden = false;
                            window.skyrimPlatform.widgets.set([...window.chat]);
                        }
                    });
                    window.addEventListener("skymp5-client:browserUnfocused", function() {
                        if (window.chat && window.chat[0]) {
                            window.chat[0].isInputHidden = true;
                            window.skyrimPlatform.widgets.set([...window.chat]);
                        }
                    });
                }
                if (window.scrollToLastMessage) {
                    window.scrollToLastMessage();
                }
            } catch(e) {
                console.error("Chat init error:", e);
            }
        })();
        `;
        this.sp.browser.executeJavaScript(initJs);
    }

    private onQueryKeyCodeBindings(e: any): void {
        if (this.badMenusOpen.size === 0 && e.isDown([DxScanCode.Enter])) {
            this.openChat();
        }
        if (e.isDown([DxScanCode.Escape])) {
            if (this.sp.browser.isFocused()) {
                this.closeChat();
            }
        }
    }

    public openChat(): void {
        this.sp.browser.setFocused(true);
        const js = `
        (function() {
            if (window.chat && window.chat[0]) {
                window.chat[0].isInputHidden = false;
                window.skyrimPlatform.widgets.set([...window.chat]);
            }
        })();
        `;
        this.sp.browser.executeJavaScript(js);
    }

    public closeChat(): void {
        this.sp.browser.setFocused(false);
        const js = `
        (function() {
            if (window.chat && window.chat[0]) {
                window.chat[0].isInputHidden = true;
                window.skyrimPlatform.widgets.set([...window.chat]);
            }
            window.dispatchEvent(new CustomEvent('skymp5-client:browserUnfocused', {}));
        })();
        `;
        this.sp.browser.executeJavaScript(js);
    }

    private onBrowserMessage(e: BrowserMessageEvent): void {
        if (e.arguments[0] === "chatInput") {
            const text = e.arguments[1];
            this.closeChat();

            if (typeof text === "string" && text.trim().length > 0) {
                this.sendChatInputToServer(text.trim());
            }
        }
    }

    private sendChatInputToServer(text: string): void {
        logTrace(this, `Sending chat message to server: ${text}`);
        const message: CustomPacketMessage = {
            t: MsgType.CustomPacket,
            contentJsonDump: JSON.stringify({
                customPacketType: "chatInput",
                text: text
            })
        };
        this.controller.emitter.emit("sendMessage", {
            message,
            reliability: "reliable"
        });
    }

    private onCustomPacketMessage(e: any): void {
        let content: any;
        try {
            content = JSON.parse(e.message.contentJsonDump);
        } catch {
            return;
        }
        if (!content || !content.customPacketType) return;

        if (content.customPacketType === "chatMessage") {
            this.handleIncomingChatMessage(content.message);
        }
    }

    private handleIncomingChatMessage(messageObj: any): void {
        if (!messageObj) return;

        const msgJson = JSON.stringify(messageObj);
        const js = `
        (function() {
            try {
                window.chatMessages = window.chatMessages || [];
                if (window.chatMessages.length >= 60) {
                    window.chatMessages.shift();
                }
                window.chatMessages.push(${msgJson});
                if (window.chat && window.chat[0]) {
                    window.chat[0].messages = window.chatMessages;
                    window.skyrimPlatform.widgets.set([...window.chat]);
                }
                if (window.scrollToLastMessage) {
                    window.scrollToLastMessage();
                }
            } catch (err) {
                console.error("Chat message error:", err);
            }
        })();
        `;
        this.sp.browser.executeJavaScript(js);
    }

    private onMenuOpen(e: MenuOpenEvent): void {
        if (this.isBadMenu(e.name)) {
            this.badMenusOpen.add(e.name);
            if (this.sp.browser.isFocused()) {
                this.closeChat();
            }
        }
    }

    private onMenuClose(e: MenuCloseEvent): void {
        this.badMenusOpen.delete(e.name);
    }

    private isBadMenu(menu: string): boolean {
        return this.badMenus.includes(menu as Menu);
    }
}

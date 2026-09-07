import { ClientListener, CombinedController, Sp } from "./clientListener";
import { CustomPacketMessage } from "../messages/customPacketMessage";
import { MsgType } from "../../messages";
import { logError, logTrace } from "../../logging";

export class PerkSyncService extends ClientListener {
    // Bilinen zanaat perkleri (Demircilik, Simya, Efsunlama)
    private readonly monitoredPerks: number[] = [
        // Demircilik (Smithing)
        0x0005218E, // Steel Smithing (Çelik Demircilik)
        0x0005218F, // Arcane Blacksmith (Gizemli Demircilik)
        0x000CB40E, // Elven Smithing (Elf Demirciliği)
        0x000CB414, // Advanced Armors (Gelişmiş Zırhlar)
        0x000CB411, // Glass Smithing (Cam Demirciliği)
        0x00052190, // Dragon Armor (Ejderha Zırhı)
        0x000CB40F, // Dwarven Smithing (Cüce Demirciliği)
        0x000CB410, // Orcish Smithing (Ork Demirciliği)
        0x000CB412, // Ebony Smithing (Abanoz Demirciliği)
        0x000CB413, // Daedric Smithing (Daedra Demirciliği)

        // Simyacılık (Alchemy)
        0x000BE127, 0x000C07CA, 0x000C07CB, 0x000C07CC, 0x000C07CD, // Alchemist 1-5
        0x00058215, // Physician
        0x00058216, // Benefactor
        0x00058217, // Poisoner
        0x00058218, 0x000A3F64, 0x000A3F65, // Experimenter 1-3
        0x00105F2F, // Concentrated Poison
        0x00105F2E, // Green Thumb
        0x00105F2C, // Snakeblood
        0x0005821D, // Purity

        // Efsunlama (Enchanting)
        0x000BEE97, 0x000C07CE, 0x000C07CF, 0x000C07D0, 0x000C07D1, // Enchanter 1-5
        0x00058F80, // Fire Enchanter
        0x00058F81, // Frost Enchanter
        0x00058F82, // Storm Enchanter
        0x00058F7E, // Insightful Enchanter
        0x00058F7D, // Corpus Enchanter
        0x00058F7F, // Extra Effect
        0x00058F7C, // Soul Squeezer
        0x00108A44, // Soul Siphon
    ];

    private preMenuPerks = new Set<number>();
    private statsMenuIsOpen = false;
    private hasRequestedInitialSync = false;
    private lastSyncRequest = 0;
    private pendingSync: any = null;

    constructor(private sp: Sp, private controller: CombinedController) {
        super();

        this.controller.emitter.on("connectionAccepted", () => { this.hasRequestedInitialSync = false; this.lastSyncRequest = 0; this.pendingSync = null; });

        // Gelen sunucu paketlerini dinle
        this.controller.emitter.on("customPacketMessage", (e) => this.onCustomPacketMessage(e));

        // StatsMenu açılma ve kapanma olayları
        this.controller.on("menuOpen", (e) => this.onMenuOpen(e.name));
        this.controller.on("menuClose", (e) => this.onMenuClose(e.name));

        // Oyuncu dünyada aktifleştiğinde ilk eşitlemeyi iste
        this.controller.on("update", () => {
            if (this.pendingSync && !this.statsMenuIsOpen && this.sp.Game.getPlayer()?.getParentCell()) {
                this.handleSyncPerks(this.pendingSync);
                this.pendingSync = null;
            }
            if (!this.hasRequestedInitialSync && Date.now() - this.lastSyncRequest > 3000) {
                const player = this.sp.Game.getPlayer();
                if (player && player.getFormID() !== 0) {
                    this.lastSyncRequest = Date.now();
                    this.requestSyncFromServer();
                }
            }
        });
    }

    private onMenuOpen(menuName: string): void {
        if (menuName.toLowerCase() === "statsmenu") {
            this.statsMenuIsOpen = true;
            this.snapshotPlayerPerks();
            logTrace(this, "StatsMenu opened, snapshot taken");
        }
    }

    private onMenuClose(menuName: string): void {
        if (menuName.toLowerCase() === "statsmenu") {
            this.statsMenuIsOpen = false;
            this.checkForNewPerks();
            logTrace(this, "StatsMenu closed, checked for perk acquisitions");
        }
    }

    private snapshotPlayerPerks(): void {
        const player = this.sp.Game.getPlayer();
        if (!player) return;

        this.preMenuPerks.clear();
        for (const perkId of this.monitoredPerks) {
            const perkObj = this.sp.Perk.from(this.sp.Game.getFormEx(perkId));
            if (perkObj && player.hasPerk(perkObj)) {
                this.preMenuPerks.add(perkId);
            }
        }
    }

    private checkForNewPerks(): void {
        const player = this.sp.Game.getPlayer();
        if (!player) return;

        for (const perkId of this.monitoredPerks) {
            if (!this.preMenuPerks.has(perkId)) {
                const perkObj = this.sp.Perk.from(this.sp.Game.getFormEx(perkId));
                if (perkObj && player.hasPerk(perkObj)) {
                    // Oyuncu menüde bu perki açtı! Sunucuya bildir:
                    logTrace(this, `Player selected perk 0x${perkId.toString(16)} in StatsMenu, requesting server verification...`);
                    this.sendSelectPerkRequest(perkId);
                    this.preMenuPerks.add(perkId);
                }
            }
        }
    }

    private sendSelectPerkRequest(perkId: number): void {
        const message: CustomPacketMessage = {
            t: MsgType.CustomPacket,
            contentJsonDump: JSON.stringify({
                customPacketType: "requestSelectPerk",
                perkId: perkId
            })
        };
        this.controller.emitter.emit("sendMessage", {
            message,
            reliability: "reliable"
        });
    }

    private requestSyncFromServer(): void {
        const message: CustomPacketMessage = {
            t: MsgType.CustomPacket,
            contentJsonDump: JSON.stringify({
                customPacketType: "requestSyncPerks"
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

        if (content.customPacketType === "syncPerks") {
            this.hasRequestedInitialSync = true;
            this.pendingSync = content;
        } else if (content.customPacketType === "selectPerkRejected") {
            this.handleSelectPerkRejected(content);
        }
    }

    private handleSyncPerks(content: any): void {
        const player = this.sp.Game.getPlayer();
        if (!player) return;

        // Perk puanını eşitle
        if (typeof content.perkPoints === "number") {
            this.sp.Game.setPerkPoints(content.perkPoints);
            logTrace(this, `Synchronized perk points: ${content.perkPoints}`);
        }

        // Remove managed perks absent from the authoritative snapshot.
        if (Array.isArray(content.perks)) {
            for (const id of this.monitoredPerks) {
                const perk = this.sp.Perk.from(this.sp.Game.getFormEx(id));
                if (perk && player.hasPerk(perk) && !content.perks.includes(id)) player.removePerk(perk);
            }
        }
        for (const skill of ["Smithing", "Alchemy", "Enchanting"]) {
            const level = content.skills?.[skill];
            if (Number.isFinite(level) && level >= 0 && level <= 100) player.setActorValue(skill, level);
        }

        // Onaylanmış perkleri yükle
        if (Array.isArray(content.perks)) {
            for (const perkId of content.perks) {
                const perkObj = this.sp.Perk.from(this.sp.Game.getFormEx(perkId));
                if (perkObj && !player.hasPerk(perkObj)) {
                    player.addPerk(perkObj);
                    logTrace(this, `Added server perk 0x${perkId.toString(16)} to player`);
                }
            }
        }
    }

    private handleSelectPerkRejected(content: any): void {
        const player = this.sp.Game.getPlayer();
        if (!player) return;

        const perkId = content.perkId;
        if (!Number.isInteger(perkId) || perkId <= 0) return;
        const perkObj = this.sp.Perk.from(this.sp.Game.getFormEx(perkId));
        if (perkObj && player.hasPerk(perkObj)) {
            player.removePerk(perkObj);
            // The server follows rejection with an authoritative point snapshot.
            logError(this, `Perk 0x${perkId.toString(16)} rejected by server (${content.reason || 'unauthorized'}), reverted.`);
        }
    }
}

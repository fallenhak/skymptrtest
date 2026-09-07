import * as fs from "fs";
import * as path from "path";
import { System, Content, SystemContext, Log } from "./system";
import { INITIAL_PERK_POINTS, INITIAL_SKILLS, PERK_POLICY_VERSION, validatePerkSelection } from '../../../shared/rp/perkRules';

export interface PlayerPerkData {
  profileId: number;
  level: number;
  perkPoints: number;
  perks: number[];
  skills: Record<string, number>;
  updatedAtUtc: string;
}

export class PerkSystem implements System {
  systemName = "PerkSystem";

  private userToProfile = new Map<number, number>();
  private playersDir: string;

  constructor(private log: Log = console.log) {
    this.playersDir = path.join(process.cwd(), "data", "players");
    if (!fs.existsSync(this.playersDir)) {
      fs.mkdirSync(this.playersDir, { recursive: true });
    }
  }

  async initAsync(ctx: SystemContext): Promise<void> {
    ctx.gm.on("spawnAllowed", (userId: number, profileId: number) => {
      this.userToProfile.set(userId, profileId);
      this.log(`[PerkSystem] User ${userId} mapped to profile ${profileId}`);

      // Karakter verisini yükle ve istemciye gönder
      const playerData = this.getOrCreatePlayerData(profileId);
      this.sendSyncPacket(userId, playerData, ctx);
    });
  }

  disconnect(userId: number, ctx: SystemContext): void {
    this.userToProfile.delete(userId);
  }

  customPacket(
    userId: number,
    type: string,
    content: Content,
    ctx: SystemContext
  ): void {
    if (type === "requestSyncPerks") {
      const profileId = this.userToProfile.get(userId);
      if (profileId !== undefined) {
        const playerData = this.getOrCreatePlayerData(profileId);
        this.sendSyncPacket(userId, playerData, ctx);
      }
      return;
    }

    if (type === "requestSelectPerk") {
      const profileId = this.userToProfile.get(userId);
      if (profileId === undefined) {
        this.log(`[PerkSystem] Received requestSelectPerk from unknown user ${userId}`);
        return;
      }

      const perkId = content.perkId;
      const playerData = this.getOrCreatePlayerData(profileId);
      const reason = validatePerkSelection(playerData, perkId);
      if (reason) {
        this.sendRejectPacket(userId, perkId, reason, ctx);
        this.sendSyncPacket(userId, playerData, ctx);
        return;
      }

      // Zaten var mı kontrolü
      if (playerData.perks.includes(perkId)) {
        this.log(`[PerkSystem] User ${userId} (profile ${profileId}) already has perk 0x${perkId.toString(16)}`);
        this.sendSyncPacket(userId, playerData, ctx);
        return;
      }

      // Onayla ve düş
      playerData.perkPoints -= 1;
      playerData.perks.push(perkId);
      playerData.updatedAtUtc = new Date().toISOString();
      this.savePlayerData(playerData);

      this.log(`[PerkSystem] Profile ${profileId} unlocked perk 0x${perkId.toString(16)}. Remaining points: ${playerData.perkPoints}`);
      this.sendSyncPacket(userId, playerData, ctx);
    }
  }

  private getOrCreatePlayerData(profileId: number): PlayerPerkData {
    const filePath = path.join(this.playersDir, `${profileId}.json`);
    if (fs.existsSync(filePath)) {
      try {
        const data = JSON.parse(fs.readFileSync(filePath, "utf8"));
        if (!data || data.profileId !== profileId || !Number.isInteger(data.perkPoints) || data.perkPoints < 0 ||
            !Array.isArray(data.perks) || !data.perks.every((id: unknown) => typeof id === 'number' && Number.isInteger(id) && id > 0) ||
            !data.skills || !Object.keys(INITIAL_SKILLS).every(skill => Number.isFinite(data.skills[skill]))) {
          throw new Error('Invalid player data');
        }
        return data as PlayerPerkData;
      } catch (e) {
        throw new Error(`Cannot read perk data for profile ${profileId}; original file preserved: ${e}`);
      }
    }

    // Existing profiles are preserved; the lab preset applies to new profiles.
    const initialData: PlayerPerkData = {
      profileId,
      level: 1,
      perkPoints: INITIAL_PERK_POINTS,
      perks: [],
      skills: { ...INITIAL_SKILLS },
      updatedAtUtc: new Date().toISOString()
    };
    this.savePlayerData(initialData);
    return initialData;
  }

  private savePlayerData(data: PlayerPerkData): void {
    const filePath = path.join(this.playersDir, `${data.profileId}.json`);
    const temporary = filePath + '.tmp';
    fs.writeFileSync(temporary, JSON.stringify(data, null, 2), "utf8");
    fs.renameSync(temporary, filePath);
  }

  private sendSyncPacket(userId: number, data: PlayerPerkData, ctx: SystemContext): void {
    const packet = JSON.stringify({
      customPacketType: "syncPerks",
      policyVersion: PERK_POLICY_VERSION,
      profileId: data.profileId,
      perkPoints: data.perkPoints,
      perks: data.perks,
      skills: data.skills
    });
    ctx.svr.sendCustomPacket(userId, packet);
  }

  private sendRejectPacket(userId: number, perkId: number, reason: string, ctx: SystemContext): void {
    const packet = JSON.stringify({
      customPacketType: "selectPerkRejected",
      perkId,
      reason
    });
    ctx.svr.sendCustomPacket(userId, packet);
  }
}

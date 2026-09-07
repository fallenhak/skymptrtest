const fs = require('fs');
const path = require('path');

console.log("[SKYMP_LAB_READY]", JSON.stringify({ onlinePlayers: mp.get(0, "onlinePlayers") }));

// -------------------------------------------------------------
// SkyMP TR: Kalıcı ve Sunucu Onaylı Perk Sistemi (PerkSystem)
// -------------------------------------------------------------

const playersDir = path.join(process.cwd(), 'data', 'players');
if (!fs.existsSync(playersDir)) {
  fs.mkdirSync(playersDir, { recursive: true });
}

function getOrCreatePlayerData(profileId) {
  const filePath = path.join(playersDir, `${profileId}.json`);
  if (fs.existsSync(filePath)) {
    try {
      return JSON.parse(fs.readFileSync(filePath, 'utf8'));
    } catch (e) {
      console.error(`[PerkSystem] JSON parse hatasi (profile ${profileId}):`, e.message);
    }
  }

  // Yeni oyuncu: 1 başlangıç perk puanı, temel zanaat seviyeleri
  const initialData = {
    profileId,
    level: 1,
    perkPoints: 1,
    perks: [],
    skills: {
      Smithing: 20,
      Alchemy: 15,
      Enchanting: 15
    },
    updatedAtUtc: new Date().toISOString()
  };
  savePlayerData(initialData);
  return initialData;
}

function savePlayerData(data) {
  const filePath = path.join(playersDir, `${data.profileId}.json`);
  fs.writeFileSync(filePath, JSON.stringify(data, null, 2), 'utf8');
}

function sendSyncPacket(userId, data) {
  const packet = JSON.stringify({
    customPacketType: "syncPerks",
    profileId: data.profileId,
    perkPoints: data.perkPoints,
    perks: data.perks,
    skills: data.skills
  });
  mp.sendCustomPacket(userId, packet);
  console.log(`[PerkSystem] syncPerks gönderildi -> Kullanıcı: ${userId}, Profil: #${data.profileId}, Puan: ${data.perkPoints}, Perk Sayısı: ${data.perks.length}`);
}

const userProfiles = new Map();

mp.on("customPacket", (userId, rawContent) => {
  let content;
  try {
    content = typeof rawContent === "string" ? JSON.parse(rawContent) : rawContent;
  } catch (e) {
    return;
  }
  if (!content || !content.customPacketType) return;

  if (content.customPacketType === "requestSyncPerks") {
    let profileId = userProfiles.get(userId) || content.profileId || 1;
    userProfiles.set(userId, profileId);
    const data = getOrCreatePlayerData(profileId);
    sendSyncPacket(userId, data);
    return;
  }

  if (content.customPacketType === "requestSelectPerk") {
    let profileId = userProfiles.get(userId) || content.profileId || 1;
    userProfiles.set(userId, profileId);

    const perkId = typeof content.perkId === "number" ? content.perkId : parseInt(content.perkId, 16);
    if (!perkId || isNaN(perkId)) {
      console.warn(`[PerkSystem] Geçersiz perk ID: ${content.perkId}`);
      return;
    }

    const data = getOrCreatePlayerData(profileId);

    // Puan kontrolü
    if (data.perkPoints < 1) {
      console.warn(`[PerkSystem] Profil #${profileId} için yetersiz perk puanı (0). Perk 0x${perkId.toString(16)} reddedildi.`);
      mp.sendCustomPacket(userId, JSON.stringify({
        customPacketType: "selectPerkRejected",
        perkId,
        reason: "Yetersiz perk puani"
      }));
      return;
    }

    // Zaten varsa tekrar puan düşme
    if (data.perks.includes(perkId)) {
      sendSyncPacket(userId, data);
      return;
    }

    // Puan düş ve perki kaydet
    data.perkPoints -= 1;
    data.perks.push(perkId);
    data.updatedAtUtc = new Date().toISOString();
    savePlayerData(data);

    console.log(`[PerkSystem] [✓] Profil #${profileId} perk açtı: 0x${perkId.toString(16)}. Kalan puan: ${data.perkPoints}`);
    sendSyncPacket(userId, data);
  }
});

mp.on("disconnect", (userId) => {
  userProfiles.delete(userId);
});

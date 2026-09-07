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

// -------------------------------------------------------------
// SkyMP TR: Oyun İçi Metin Sohbeti & Rol Sistemi (ChatSystem)
// -------------------------------------------------------------

const connectedUsers = new Set();

function sendPrivateSystemMessage(userId, text, color = "#3498db") {
  const packet = JSON.stringify({
    customPacketType: "chatMessage",
    message: {
      opacity: 1,
      category: "system",
      text: [
        {
          text: text,
          color: color,
          opacity: 1,
          type: ["system"]
        }
      ]
    }
  });
  mp.sendCustomPacket(userId, packet);
}

function getActorCoordinates(actorId) {
  try {
    if (actorId && typeof mp.getActorPos === "function") {
      return mp.getActorPos(actorId);
    }
  } catch (e) {}
  return null;
}

function getActorCell(actorId) {
  try {
    if (actorId && typeof mp.getActorCellOrWorld === "function") {
      return mp.getActorCellOrWorld(actorId);
    }
  } catch (e) {}
  return 0;
}

function calculateDistance(pos1, pos2) {
  if (!pos1 || !pos2) return 0;
  const dx = pos1[0] - pos2[0];
  const dy = pos1[1] - pos2[1];
  const dz = pos1[2] - pos2[2];
  return Math.sqrt(dx * dx + dy * dy + dz * dz);
}

function handleChatInput(userId, rawText) {
  const text = (rawText || "").trim();
  if (!text) return;

  const profileId = userProfiles.get(userId) || 1;
  const playerData = getOrCreatePlayerData(profileId);
  const senderActor = typeof mp.getUserActor === "function" ? mp.getUserActor(userId) : 0;
  const charName = playerData.rpName || (senderActor && typeof mp.getActorName === "function" ? mp.getActorName(senderActor) : "") || `Oyuncu #${profileId}`;

  // Komut kontrolü
  if (text.startsWith("/")) {
    const parts = text.split(" ");
    const cmd = parts[0].toLowerCase();
    const args = parts.slice(1).join(" ").trim();

    // 1. /yardim veya /help
    if (cmd === "/yardim" || cmd === "/help") {
      const helpLines = [
        "====== [ SkyMP TR Sohbet Komutları ] ======",
        "• Normal Konuşma: Sadece mesajınızı yazın (25m çevre)",
        "• /me <eylem>: Karakterinizin yaptığı hareket (* Ahmet kılıcını biler *)",
        "• /do <durum>: Çevresel durum/betimleme (* Masada iksir şişeleri var *)",
        "• /b <mesaj>: Rol dışı (OOC) sohbet (( Ahmet: birazdan geliyorum ))",
        "• /s <mesaj>: Bağırma (50m geniş menzil)",
        "• /w <mesaj>: Fısıltı (5m dar menzil)",
        "• /zar [sayı]: Zar atma (Varsayılan: 1-100)",
        "• /g <mesaj>: Genel sunucu sohbeti (Tüm oyuncular)",
        "• /isim <yeni ad>: Karakterinizin rol adını belirler",
        "==========================================="
      ];
      for (const line of helpLines) {
        sendPrivateSystemMessage(userId, line, "#d4af37");
      }
      return;
    }

    // 2. /isim veya /nick
    if (cmd === "/isim" || cmd === "/nick") {
      if (!args) {
        sendPrivateSystemMessage(userId, "[!] Kullanım: /isim <karakterinizin_adı>", "#e74c3c");
        return;
      }
      playerData.rpName = args;
      savePlayerData(playerData);
      sendPrivateSystemMessage(userId, `[✓] Karakter adınız '${args}' olarak ayarlandı.`, "#2ecc71");
      return;
    }

    // 3. /me <eylem>
    if (cmd === "/me") {
      if (!args) {
        sendPrivateSystemMessage(userId, "[!] Kullanım: /me <eylem>", "#e74c3c");
        return;
      }
      broadcastChatMessage({
        senderUserId: userId,
        senderActor,
        radius: 2200,
        category: "action",
        spans: [
          { text: `* ${charName} ${args} *`, color: "#c37bdd", type: ["action"] }
        ]
      });
      return;
    }

    // 4. /do <durum>
    if (cmd === "/do") {
      if (!args) {
        sendPrivateSystemMessage(userId, "[!] Kullanım: /do <durum/betimleme>", "#e74c3c");
        return;
      }
      broadcastChatMessage({
        senderUserId: userId,
        senderActor,
        radius: 2200,
        category: "action",
        spans: [
          { text: `* ${args} (${charName}) *`, color: "#5dade2", type: ["action"] }
        ]
      });
      return;
    }

    // 5. /b veya /ooc
    if (cmd === "/b" || cmd === "/ooc") {
      if (!args) {
        sendPrivateSystemMessage(userId, "[!] Kullanım: /b <rol dışı mesaj>", "#e74c3c");
        return;
      }
      broadcastChatMessage({
        senderUserId: userId,
        senderActor,
        radius: 2200,
        category: "nonrp",
        spans: [
          { text: `(( ${charName}: ${args} ))`, color: "#a6acaf", type: ["nonrp"] }
        ]
      });
      return;
    }

    // 6. /s veya /shout (Bağırma)
    if (cmd === "/s" || cmd === "/shout") {
      if (!args) {
        sendPrivateSystemMessage(userId, "[!] Kullanım: /s <bağırılacak mesaj>", "#e74c3c");
        return;
      }
      broadcastChatMessage({
        senderUserId: userId,
        senderActor,
        radius: 5000,
        category: "plain",
        spans: [
          { text: `${charName} bağırıyor: `, color: "#e67e22", type: ["shout"] },
          { text: args, color: "#f39c12", type: ["shout"] }
        ]
      });
      return;
    }

    // 7. /w veya /whisper (Fısıltı)
    if (cmd === "/w" || cmd === "/whisper") {
      if (!args) {
        sendPrivateSystemMessage(userId, "[!] Kullanım: /w <fısıldanacak mesaj>", "#e74c3c");
        return;
      }
      broadcastChatMessage({
        senderUserId: userId,
        senderActor,
        radius: 600,
        category: "plain",
        spans: [
          { text: `${charName} fısıldıyor: `, color: "#7fb3d5", type: ["whisper"] },
          { text: args, color: "#d6eaf8", type: ["whisper"] }
        ]
      });
      return;
    }

    // 8. /zar veya /roll
    if (cmd === "/zar" || cmd === "/roll") {
      const maxVal = args && !isNaN(parseInt(args)) ? Math.max(2, parseInt(args)) : 100;
      const rollResult = Math.floor(Math.random() * maxVal) + 1;
      broadcastChatMessage({
        senderUserId: userId,
        senderActor,
        radius: 2200,
        category: "dice",
        spans: [
          { text: `🎲 ${charName} zar attı: `, color: "#2ecc71", type: ["dice"] },
          { text: `${rollResult}`, color: "#f1c40f", type: ["dice"] },
          { text: ` (1-${maxVal})`, color: "#8d93a8", type: ["dice"] }
        ]
      });
      return;
    }

    // 9. /g veya /genel (Global Sohbet)
    if (cmd === "/g" || cmd === "/genel") {
      if (!args) {
        sendPrivateSystemMessage(userId, "[!] Kullanım: /g <genel mesaj>", "#e74c3c");
        return;
      }
      broadcastChatMessage({
        senderUserId: userId,
        senderActor,
        radius: Infinity,
        category: "system",
        spans: [
          { text: `[GENEL] ${charName}: `, color: "#f1c40f", type: ["plain"] },
          { text: args, color: "#ffffff", type: ["plain"] }
        ]
      });
      return;
    }

    sendPrivateSystemMessage(userId, `[!] Bilinmeyen komut: ${cmd}. Komut listesi için /yardim yazın.`, "#e74c3c");
    return;
  }

  // Normal Yakınlık Konuşması
  broadcastChatMessage({
    senderUserId: userId,
    senderActor,
    radius: 2200,
    category: "plain",
    spans: [
      { text: `${charName}: `, color: "#f39c12", type: ["plain"] },
      { text: text, color: "#ffffff", type: ["plain"] }
    ]
  });
}

function broadcastChatMessage(opts) {
  const { senderUserId, senderActor, radius, category, spans } = opts;
  const senderPos = getActorCoordinates(senderActor);
  const senderCell = getActorCell(senderActor);

  console.log(`[Chat] [${category}] Gönderen: #${senderUserId} -> ${spans.map(s => s.text).join("")}`);

  // Hedef alıcıları belirle (bağlı tüm kullanıcılar)
  const targets = new Set(connectedUsers);
  targets.add(senderUserId); // Göndereni kesinlikle dahil et
  if (userProfiles) {
    for (const uid of userProfiles.keys()) {
      targets.add(uid);
    }
  }

  for (const targetUserId of targets) {
    let opacity = 1.0;

    if (radius !== Infinity && targetUserId !== senderUserId) {
      const targetActor = typeof mp.getUserActor === "function" ? mp.getUserActor(targetUserId) : 0;
      if (targetActor && senderActor) {
        // Hücre kontrolü (farklı iç/dış mekandaysalar duyulmaz)
        const targetCell = getActorCell(targetActor);
        if (senderCell !== 0 && targetCell !== 0 && senderCell !== targetCell) {
          continue;
        }

        const targetPos = getActorCoordinates(targetActor);
        if (senderPos && targetPos) {
          const dist = calculateDistance(senderPos, targetPos);
          if (dist > radius) {
            continue; // Menzil dışı
          }
          if (dist > 500) {
            opacity = Math.max(0.25, Number(((radius - dist) / (radius - 500)).toFixed(2)));
          }
        }
      }
    }

    const payload = JSON.stringify({
      customPacketType: "chatMessage",
      message: {
        opacity: opacity,
        category: category,
        text: spans.map(s => ({
          text: s.text,
          color: s.color || "#ffffff",
          opacity: opacity,
          type: s.type || ["plain"]
        }))
      }
    });

    try {
      mp.sendCustomPacket(targetUserId, payload);
    } catch (e) {
      console.error(`[Chat] Paket gönderim hatası (kullanıcı ${targetUserId}):`, e.message);
    }
  }
}

// -------------------------------------------------------------
// Olay Dinleyicileri (Event Handlers)
// -------------------------------------------------------------

mp.on("connect", (userId) => {
  connectedUsers.add(userId);
  console.log(`[ChatSystem] Kullanıcı bağlandı: ${userId}`);
  // Bağlanan kullanıcıya hoş geldin mesajı gönder
  setTimeout(() => {
    sendPrivateSystemMessage(userId, "SkyMP TR sunucusuna hoş geldiniz! Sohbet komutları için /yardim yazabilirsiniz.", "#d4af37");
  }, 1000);
});

mp.on("customPacket", (userId, rawContent) => {
  let content;
  try {
    content = typeof rawContent === "string" ? JSON.parse(rawContent) : rawContent;
  } catch (e) {
    return;
  }
  if (!content || !content.customPacketType) return;

  // Metin Sohbeti Girdisi
  if (content.customPacketType === "chatInput") {
    handleChatInput(userId, content.text);
    return;
  }

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
  connectedUsers.delete(userId);
  userProfiles.delete(userId);
  console.log(`[ChatSystem] Kullanıcı ayrıldı: ${userId}`);
});

// -------------------------------------------------------------
// SkyMP TR: UDP 3D Yakınlık Sesli Sohbet Sunucusu (VoiceRelay)
// -------------------------------------------------------------
const dgram = require('dgram');
const voiceRelay = dgram.createSocket('udp4');
const voiceClients = new Map(); // profileId -> { address, port, lastSeen }

voiceRelay.on('error', (err) => {
  console.error('[VoiceRelay] UDP soket hatası:', err.message);
});

voiceRelay.on('message', (msg, rinfo) => {
  if (!msg || msg.length < 5) return;
  const packetType = msg.readUInt8(0);
  const profileId = msg.readUInt32LE(1);

  if (packetType === 1) {
    // Handshake / Heartbeat
    voiceClients.set(profileId, {
      address: rinfo.address,
      port: rinfo.port,
      lastSeen: Date.now()
    });
    return;
  }

  if (packetType === 2) {
    // Ses Verisi (Voice Chunk)
    voiceClients.set(profileId, {
      address: rinfo.address,
      port: rinfo.port,
      lastSeen: Date.now()
    });

    if (msg.length <= 9) return;
    const audioPayload = msg.subarray(9);

    // Konuşan oyuncunun koordinatlarını bul
    let speakerActor = 0;
    if (typeof mp.getActorsByProfileId === 'function') {
      const actors = mp.getActorsByProfileId(profileId);
      if (actors && actors.length > 0) speakerActor = actors[0];
    }
    if (!speakerActor && typeof mp.getUserActor === 'function') {
      for (const [uid, pid] of userProfiles.entries()) {
        if (pid === profileId) {
          speakerActor = mp.getUserActor(uid);
          break;
        }
      }
    }

    const speakerPos = getActorCoordinates(speakerActor);
    const speakerCell = getActorCell(speakerActor);

    // Yakındaki tüm dinleyicilere 3D uzamsal konumla ilet
    const now = Date.now();
    for (const [targetProfileId, client] of voiceClients.entries()) {
      if (targetProfileId === profileId) continue; // Kendine yankı yapma
      if (now - client.lastSeen > 12000) continue; // Zaman aşımı

      let listenerActor = 0;
      if (typeof mp.getActorsByProfileId === 'function') {
        const actors = mp.getActorsByProfileId(targetProfileId);
        if (actors && actors.length > 0) listenerActor = actors[0];
      }
      if (!listenerActor && typeof mp.getUserActor === 'function') {
        for (const [uid, pid] of userProfiles.entries()) {
          if (pid === targetProfileId) {
            listenerActor = mp.getUserActor(uid);
            break;
          }
        }
      }

      let dist = 0.0;
      let relX = 0.0;
      let relY = 0.0;

      if (speakerActor && listenerActor) {
        const listenerCell = getActorCell(listenerActor);
        if (speakerCell !== 0 && listenerCell !== 0 && speakerCell !== listenerCell) {
          continue; // Farklı iç/dış mekan hücresi
        }

        const listenerPos = getActorCoordinates(listenerActor);
        if (speakerPos && listenerPos) {
          dist = calculateDistance(speakerPos, listenerPos);
          if (dist > 2200) {
            continue; // Duyma mesafesi dışı
          }
          relX = speakerPos[0] - listenerPos[0];
          relY = speakerPos[1] - listenerPos[1];
        }
      }

      // 3D Ses Paketi formatı:
      // [0]: uint8 (3 = SPATIAL_VOICE)
      // [1..4]: uint32 (speakerProfileId)
      // [5..8]: float32 (dist)
      // [9..12]: float32 (relX)
      // [13..16]: float32 (relY)
      // [17..]: PCM ses verisi
      const outPacket = Buffer.alloc(17 + audioPayload.length);
      outPacket.writeUInt8(3, 0);
      outPacket.writeUInt32LE(profileId, 1);
      outPacket.writeFloatLE(dist, 5);
      outPacket.writeFloatLE(relX, 9);
      outPacket.writeFloatLE(relY, 13);
      audioPayload.copy(outPacket, 17);

      voiceRelay.send(outPacket, client.port, client.address);
    }
  }
});

try {
  voiceRelay.bind(3001, () => {
    console.log('[VoiceRelay] UDP 3D Proximity Voice Server 3001 portunda başlatıldı.');
  });
} catch (err) {
  console.error('[VoiceRelay] Port bağlama hatası:', err.message);
}



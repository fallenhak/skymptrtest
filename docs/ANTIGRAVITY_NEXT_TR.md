# Antigravity — devam görevi (7 Eylül 2026)

Kullanıcı kalan işleri senin tamamlamanı istedi. Önce `AGENTS.md`, `MODEL_BRIEFING_TR.md`, `PERK_LAB_TR.md` ve `reviews/2026-09-07-antigravity-review.md` dosyalarını oku. Bu belge, eski tamamlandı ifadelerinden daha güncel olan uygulama/kabul testi ayrımını özetler.

## Başlangıç noktası

- Repo: `fallenhak/skymptrtest`; dal: `codex/character-perks-voice`; son işlevsel commit: `00d642e9`.
- Çalışma dizini: `C:\Users\kerim\Documents\ChatGPT\SkyMPTR Test`.
- Kod yerel lab'a dağıtıldı; GitHub release/launcher paketi güncellenmedi. Main dalına birleştiğini varsayma.
- Son kontrol: istemci webpack ve sunucu TypeScript/esbuild geçti; toplam 20 otomatik test geçti. Yerel native sunucu başlangıcı ve beş-master manifest testi geçti. Canlı oyun kabul testi yapılmadı; kullanıcıdan henüz sonuç gelmedi.
- Son oturumda test sunucusu PID **32260** ile açık bırakıldı, manifest HTTP 200 verdi. Bu tarihsel bilgidir: işlem kimliği ve komut satırını yeniden kontrol et; PID'ye körlemesine işlem yapma. Loglar `.local/perk-lab-server.stdout.log`, `.stderr.log`, `.pid`.
- Son kod dağıtım yedeği `.local/code-backups/20260907-173018-715`; manifest hangi yedeğin hangi dosyaya ait olduğunu içerir.
- Ayrı Faalgrin kurulumu ve kullanıcının kayıtlarına dokunma. Önceki kontrolde Faalgrin oyunu açıktı; test oyunu değildi.

## Kullanıcının kesin istediği sonuç

1. İlk girişte karakter oluşturma; tamamlanmış karaktere tekrar girişte gereksiz yeniden oluşturma olmaması.
2. Skyrim'in kendi perk menüsünü koruyan çalışan perkler. **Önce mevcut Demircilik, Simya, Efsunlama ağaçlarıyla başlamak onaylandı.** Madencilik, terzilik, aşçılık daha sonraki aşama.
3. Sesli sohbette **Fısıltı / Normal / Bağırma**. Mikrofon açıldığında oyun içinde gösterge; konuşurken **gerçek ses seviyesine göre** hareket etmesi.
4. UI, Skyrim'e olabildiğince yakın; Nordic UI benzeri ölçülü tasarım. Genel web paneli, neon, iri yuvarlak kartlar veya rastgele hareket eden sahte ses göstergesi istemiyor.
5. Her çalışma sonunda `MODEL_BRIEFING_TR.md` güncellenecek. Bu kural AGENTS.md'de de mevcut. Yapılanları, gerçekten çalıştırılan testleri, dağıtılan dosyaları, açık sorunları ve sıradaki adımı ayrı belirt.

## Tamamlanan kaynak değişiklikleri — yeniden yazma

- `skymp5-server/ts/systems/spawn.ts`: görünümü olmayan mevcut aktörde race menu yeniden istenir. Yeni aktörde menü bayrağı `setUserActor` öncesinde ayarlanır.
- `skymp5-client/src/services/services/remoteServer.ts`: race menu isteği dünya/hücre hazır olana ve yükleme menüsü kapanana kadar bekletilir.
- `shared/rp/perkRules.ts`: altı izinli perk; seviye ve önceki perk kontrolü. Yeni perk profili 3 puan, Smithing 30, Alchemy 20, Enchanting 20. Mevcut profiller sıfırlanmaz.
- `skymp5-server/ts/systems/perkSystem.ts`: profil oyun girişinden alınır, istemcinin profileId iddiasına güvenilmez; son puanı kullanan tekrar talep tekrar ücretlendirilmez; bozuk kayıt korunarak hata verilir; geçici dosya üzerinden kayıt yapılır.
- `perkSyncService.ts`: ilk sync yanıt alınana kadar tekrar istenir, snapshot dünya hazır ve StatsMenu kapalıyken uygulanır. Onaysız yönetilen perkler kaldırılır, puan/seviye sunucudan alınır. Ret mesajında körlemesine puan artırılmaz.
- `scripts/gamemode.js`: ikinci perk uygulaması kaldırıldı; tek sahibi TS PerkSystem. Tanımsız userProfiles referansları kaldırıldı. Sohbet isimleri `data/chat` altında; eski `data/players` içindeki rpName okunabilir ama sohbet artık perk dosyasına yazmaz.
- `scripts/deploy-lab-code.ps1`: sunucu JS/map, gamemode, istemci JS birlikte yedeklenip dağıtılır. Ayar/world/native dosyalar korunur. CI'a yeni testler eklendi.

## Önce doğrula: karakter ve perkler

`PERK_LAB_TR.md` tam kabul adımlarını ve Form ID tablosunu içerir. Kullanıcıdan teknik olmayan oyun kontrollerini isteyebilirsin; limit tüketimini azaltmak istiyor.

- Lab'da yeni/yarım kalmış/tamamlanmış karakter için oluşturma ekranını kontrol et. Açılmazsa server log, `appearance` ve `isRaceMenuOpen`, istemci packet/load sırasını incele. Dünyayı veya kayıtları silerek geçiştirme.
- Yeni perk profilinde 3 puan ve 30/20/20; Steel → Dwarven → Alchemist 1 seçiminden sonra 0 puan; yeniden bağlantıda kalıcılık. Diğer dal için ayrı test profili kullan; mevcut profili izinsiz sıfırlama.
- CSF `SKILLS.json` halen sadece vanilla ağaç isimlerini içerir. Düğümler/adlar/açıklamalar özelleştirilmedi. Diğer vanilla düğümler görünebilir; sunucu bunları onaylamaz. Görseli tamamlanmış özel meslek sistemi gibi sunma.
- **En önemli açık ayrım:** sunucu şu an perk sahipliği/puanını doğrular. Üretim, iksir ve efsunlama sonuçlarına native/server etkisi henüz doğrulanmadı. C++ üretim kodu bu commit'te değişmedi. Görsel perk/JSON kaydını işlev kanıtı sayma; iki oyuncuyla somut sonuçları ölç, gerekiyorsa native hesaplamayı düzelt.

## Sonra uygula: ses ve Skyrim tarzı HUD

Mevcut mikrofon/UDP uygulaması `src/launcher/Program.cs` içindeki `VoiceManager`; relay `scripts/gamemode.js` sonunda. Frontend `skymp5-front/src`, client servisleri `skymp5-client/src/services/services` altında.

Aşağıdakiler **uygulanmış değil**, devam için önerilen yaklaşım:

- Üç modu hem launcher hem relay'de ortak protokole bağla. V mevcut bas-konuş tuşu; mod değiştirme için örneğin F8 kullanılabilir, bu kullanıcı tarafından henüz seçilmiş bir tuş değil. Menzilleri tek yerde tanımla ve belgeye yaz.
- Ses yakalama callback'inde PCM örneklerinden RMS/tepe seviyesi hesapla; HUD'a gerçek ölçümü aktar. Yalnızca launcher üzerindeki durum yazısı yeterli değil.
- Client ile launcher arasında lab'a ait küçük durum dosyaları kullanılabilir (örn. `Data/Platform/voice-state.json`); atomik yazma, sınırlı okuma sıklığı ve eski veriyi susturma gerekir. Alternatif mevcut mimariye daha uygunsa kullan. Rastgele animasyonla ses ölçümünü taklit etme.
- Bas-konuş yalnızca ilgili test Skyrim'i ön plandayken yayın yapmalı; tuş bırakma, odak kaybı, oyun/launcher kapanışı yayın durumunu temizlemeli. Faalgrin veya diğer uygulamada V tuşuna basmak mikrofonu açmamalı.
- **Mevcut UDP protokolü yalnızca profileId ile kayıt kabul ediyor.** Oyun girişinden verilen kısa ömürlü oturum kimliğiyle ilişkilendir; kopuşta iptal et. Aktör/konum/hücre bilinmiyorsa sesi iletme. Modu yalnızca HUD metni olarak değiştirmek yeterli değil; relay menzili gerçekten değişmeli.
- Konuşmacı mesafesi ve dinleyicinin baktığı yönle ses/pan kontrolü yap. Eski pan dünya eksenine bağlı; oyuncu dönünce doğru kalacağı varsayılmamalı.
- WinMM durdurma/kaynak temizliği ve başarısız cihaz açma durumunu kontrol et; ses callback belleğini boşa çıkarırken yaşam döngüsünü bozma.
- Oyun HUD: küçük, yarı saydam koyu zemin; kırık beyaz metin, ince çizgiler, sade mikrofon/rün benzeri işaret; mod adı ve gerçek seviyeye bağlı gösterge. Mevcut font ve UI yaklaşımını kullan; mod varlıklarını izinsiz kopyalama. Sohbet ve diğer menülerle çakışmasını kontrol et.

Ses kabul testi: iki ayrı profil, üç modda sınır içi/dışı, farklı hücre, dinleyicinin dönmesi, tuş bırakma/alt-tab, bağlantı kopması, mikrofon bulunmaması. Tek oyunculu derleme testi bunu kanıtlamaz. HUD'un hem sessiz hem konuşan durumunu kullanıcıya göster.

## Dağıtım ve bilinen engeller

- Lab: `.local/skymp-756fb86/skyrim-1.6.1170/lab-player-1`; oyun `game`, modlar `mod-organizer/mods` altında. İstemci JS hem `05_SkyMP_Client/Platform/Plugins` hem game/Data tarafında güncellenmeli; yoksa başlatma eski dosyayı geri kopyalayabilir.
- Sunucu `.local/skymp-756fb86/server`. `prepare-local-server.ps1` halen upstream JS ve basit gamemode kurar. Yerel derleme + `deploy-lab-code.ps1` atlanırsa yeni özellikler çalışmaz. Temiz kurulum akışının bunu otomatik yapması açık iştir.
- `scripts/start-game-lab.ps1` varsayılanı doğrudan SKSE; `-ViaMO2` opsiyonel. MO2 `Start-Process -Wait` kullanma. Kayıt için mevcut çözüm LocalSaves=false; profil başına save izolasyonu varmış gibi davranma.
- Launcher aynı boyuttaki dosya güncellemesini atlayabiliyor; ZIP hedef containment/hash kontrolü yok; `build-launcher.ps1` içindeki LiteralPath wildcard hatası biliniyor. Release hazırlamadan önce bunları gider; detay ve örnekler inceleme belgesinde.
- Steam 1.7 oyun verileri ile lab 1.6.1170 verilerini karıştırma. Sadece EXE değiştirmek eşdeğer veri seti sağlamaz.
- C# launcher eski .NET Framework CSC/C#5 ile derleniyor: catch/finally içinde await ve yeni dil özellikleri kullanma. Native C++ build son durumda VS2022 gereksinimi nedeniyle doğrulanmadı; VS2026'nın bulunması yeterli değil.

## Tekrar çalıştırılacak kontroller

```powershell
.\scripts\build-client.ps1
# skymp5-server dizininde:
npm exec --yes --package=yarn@1.22.22 -- yarn install --frozen-lockfile
npm run build-ts
# Repo kökünde:
node --test skymp5-server/tests/rpSystems.test.cjs skymp5-client/tests/perkSyncService.test.cjs skymp5-client/tests/settingsService.test.cjs
.\scripts\build-front.ps1 # UI değişirse
.\scripts\deploy-lab-code.ps1 # Lab ve test sunucusu kapalı olmalı
node scripts/test-local-server.mjs # Aynı portta açık sunucu olmamalı
```

Smoke test kendi sunucusunu kapatır. Oyun testi için `scripts/start-local-server.ps1` gerekir. Başlatıcı/HUD dosyaları mevcut deploy betiğinin kapsamına dahil değil; bunları eklediğinde dağıtım ve rollback listesini genişlet. Son olarak ortak briefing'i güncelle, kodu feature dalına gönder; test edilmemiş release'i hazır diye duyurma.

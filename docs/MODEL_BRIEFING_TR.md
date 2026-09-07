# SkyMP TR — Modeller ve Ajanlar Arası Bilgilendirme ve Devir Belgesi (Model Briefing)

> **Amaç:** Bu belge, SkyMP TR projesinde çalışan yapay zeka modelleri (LLM), kodlama ajanları ve geliştiriciler arasındaki bağlam kaybını önlemek, mimari kararları, son yapılan geliştirmeleri ve çalışma kurallarını eksiksiz aktarmak için hazırlanmıştır.

**Son Güncelleme:** 7 Eylül 2026  
**Aktif Dal (Branch):** `codex/local-game-profile`  
**GitHub Deposu:** [fallenhak/skymptrtest](https://github.com/fallenhak/skymptrtest) (Görünürlük: **PUBLIC**)  
**Son Canlı Sürüm (Release):** [v5.0.0](https://github.com/fallenhak/skymptrtest/releases/tag/v5.0.0)  

---

## 1. Projenin Amacı ve Vizyonu

SkyMP TR; The Elder Scrolls V: Skyrim Special Edition (1.6.1170) tabanında çalışan, bağımsız (Stock Game), rol yapma (RP) odaklı ve arkadaşlar arasında sıfır zahmetle oynanabilen açık kaynaklı bir çok oyunculu (multiplayer) altyapısıdır.

### Öncelik Sıralaması:
1. **Oynanabilir ve kesintisiz altyapı:** Otomatik kurulum, tek tıkla başlatma, bağlantı kararlılığı.
2. **İletişim ve RP araçları:** 3D uzamsal sesli sohbet, oyun içi metin sohbeti, rol komutları.
3. **Sunucu taraflı onaylı yetenek (Perk) sistemi:** Skyrim'in orijinal `StatsMenu` (yıldız ağacı) menüsünü koruyarak sunucu onaylı perk ilerlemesi.
4. **Meslekler ve ekonomi:** Madencilik, demircilik, simyacılık, aşçılık gibi RP meslek ağaçları.

---

## 2. Sistem Mimarisi ve Son Yapılan Geliştirmeler

```
                                  +---------------------------------------+
                                  |    GitHub Releases CDN (Public)       |
                                  |  v5.0.0: Launcher, Data, Modpack      |
                                  +---------------------------------------+
                                        ^                          ^
                      (Yedek İndirme /  |                          | (Veri Paketi
                       Sunucu Kapalı)   |                          |  Otomatik İndirme)
                                        v                          v
+---------------------------------------------------------------------------------+
|                       SkyMP TR Launcher (WPF / C# 5)                            |
|                          dist/SkyMPTR-Launcher.exe                              |
|  - Akıllı Kurulum Devam Etme (Smart Resume)                                     |
|  - Otomatik Downgrade (1.6.1170)                                                |
|  - Çift Kaynaklı Mod Eşitleyici (Yerel Sunucu + GitHub Fallback)                |
|  - 3D Yakınlık Ses Motoru (winmm.dll + Global V Tuşu PTT Hook)                  |
+---------------------------------------------------------------------------------+
           |                                                      ^
           | (Oyun Başlatma)                                      | (UDP 3001 Ses)
           v                                                      v
+------------------------------------+             +------------------------------+
|     Skyrim SE (Stock Game)         |             |   SkyMP TR Sunucusu          |
|  - SKSE 2.2.8 (1.6.1170)           |             |   (Node.js + C++20 Native)   |
|  - Skyrim Platform 2.9.0           |<---(UDP)----> - Port 7777: Oyun Protokolü  |
|  - skymp5-client.js                |    7777     | - Port 3000: HTTP Manifest   |
|    * ChatService (Enter / Escape)  |             |   ve Modpack Eşitleme        |
|    * PerkSyncService (StatsMenu)   |<---(HTTP)---> - Port 3001: UDP 3D Ses     |
|  - CEF React Chat UI               |    3000       |   Yönlendiricisi             |
+------------------------------------+             +------------------------------+
```

---

### A. Tek Dosya Hafif Başlatıcı & GitHub CDN Dağıtımı (`src/launcher/Program.cs`)
- **Dosya:** [`src/launcher/Program.cs`](file:///c:/Users/kerim/Documents/ChatGPT/SkyMPTR%20Test/src/launcher/Program.cs) -> Derleme Çıktısı: [`dist/SkyMPTR-Launcher/SkyMPTR-Launcher.exe`](file:///c:/Users/kerim/Documents/ChatGPT/SkyMPTR%20Test/dist/SkyMPTR-Launcher/SkyMPTR-Launcher.exe) (~76 KB).
- **Public CDN Entegrasyonu:**
  - Depo public yapılarak GitHub Releases doğrudan ücretsiz küresel CDN olarak kullanıldı.
  - Başlatıcı arkadaşa tek başına (~76 KB) gönderilebilir.
  - `EnsureCompanionDataDownloaded`: Yerelde `SkyMPTR-Data.zip` yoksa, başlatıcı bunu GitHub Releases CDN'inden (`https://github.com/fallenhak/skymptrtest/releases/latest/download/SkyMPTR-Data.zip`) arka planda canlı MB/yüzde göstergesiyle otomatik indirir.
- **Akıllı Kurulum ve Kaldığı Yerden Devam Etme (Smart Resume):**
  - `CopyFileIfDifferent` ve `ExtractEntryIfDifferent`: Hedefte zaten bulunan ve boyutu kaynakla uyuşan dosyalar (ESM, BSA, DLL) taranıp saliseler içinde atlanır.
  - Yarıda kalan kurulumlar sıfırdan başlamaz; buton **`KURULUMU TAMAMLA (Kaldigi Yerden)`** olarak güncellenir.
  - Canlı dosya göstergesi: Hangi BSA dosyasının kopyalandığı (ör. `Skyrim - Misc.bsa (17 MB)...`) arayüze anlık yansıtılır.
- **Çift Kaynaklı Mod Güncelleme (Dual-Source Updater):**
  - Sunucu açıksa: Yerel HTTP sunucusundan (`http://<IP>:3000/modpack.zip`) hızlı aktarım.
  - Sunucu kapalıysa: GitHub Releases üzerindeki `modpack-version.json` kontrol edilir. Yeni sürüm varsa altın sarısı rozetle **`GUNCELLEMEYI INDIR (GitHub vX)`** butonu çıkar.

---

### B. 3 Boyutlu Yakınlık Sesli Sohbet (Proximity Voice Chat)
- **Mimari:** CEF tarayıcısının mikrofon engellerine ve çökmelerine takılmamak için ses motoru doğrudan `SkyMPTR-Launcher.exe` içine entegre edilmiştir.
- **Donanım Erişimi:** Harici DLL bağımlılığı olmadan Windows `winmm.dll` (waveIn/waveOut) API'siyle 16.000 Hz 16-bit Mono kayıt ve Stereo oynatma.
- **Push-to-Talk (PTT):** `SetWindowsHookEx(WH_KEYBOARD_LL)` ile düşük seviyeli global klavye kancası. Skyrim tam ekranda ve odakta olsa dahi `V` tuşuna basılı tutulduğunda mikrofon anında açılır.
- **Sunucu UDP Ses Yönlendiricisi (`scripts/gamemode.js` Port 3001):**
  - Node.js `dgram` modülü ile UDP 3001 portunda çalışır.
  - Oyuncuların koordinatları (`mp.getActorPos`) ve hücreleri (`mp.getActorCellOrWorld`) karşılaştırılır.
  - Ses yalnızca aynı hücredeki ve 2200 birim (~25-30m) mesafedeki oyunculara gider.
  - Mesafe arttıkça ses kısılır (400 birimden sonra başlar, 2200 birimde sıfırlanır).
  - Stereo panning: Konuşan kişi soldaysa sol kulaklıktan, sağdaysa sağ kulaklıktan duyulur.

---

### C. Oyun İçi Zengin Metin Sohbeti & RP Motoru
- **İstemci:** `skymp5-client` içine `ChatService` yazıldı. Upstream `skymp5-front` React sohbet bileşeni oyuna bağlandı.
- **Kontroller:**
  - `Enter`: Sohbet kutusunu otomatik odaklar.
  - Mesaj yazılıp `Enter`'a basıldığında mesaj sunucuya gider, kutu kapanır, kontroller Skyrim'e döner.
  - `Escape`: Mesaj göndermeden kutuyu kapatır.
  - Envanter, yetenek menüsü vb. açıkken `Enter` tuşunun arayüzü engellemesi önlenmiştir.
- **Sunucu Komutları (`scripts/gamemode.js`):**
  - **Normal Mesaj:** 2200 birim menzil, mesafeye göre şeffaflaşma (Distance Opacity).
  - `/me <eylem>`: Mor renkte fiziksel eylem (* Ahmet demirci çekicini kaldırır *).
  - `/do <durum>`: Açık mavi renkte çevresel betimleme (* Masada yeni dökülmüş iksirler vardır *).
  - `/b <mesaj>`: Gri renkte OOC mesaj ((( Ahmet: sesim geliyor mu? ))).
  - `/s <mesaj>`: Turuncu renkte bağırma (5000 birim).
  - `/w <mesaj>`: Mavi renkte fısıltı (600 birim).
  - `/zar [max]`: Yeşil renkte hilesiz zar (1-100).
  - `/isim <ad>`: Kalıcı karakter rol adı belirleme (`data/players/{profileId}.json`).
  - `/g <mesaj>`: Küresel genel sohbet (altın sarısı).
  - `/yardim`: Komut kılavuzu.

---

### D. Sunucu Onaylı Perk Eşitleme Sistemi
- **İstemci:** [`skymp5-client/src/services/services/perkSyncService.ts`](file:///c:/Users/kerim/Documents/ChatGPT/SkyMPTR%20Test/skymp5-client/src/services/services/perkSyncService.ts)
  - Skyrim'in orijinal `StatsMenu` açılış ve kapanış olaylarını dinler.
  - Oyuncu menüde yeni bir perke tıkladığında sunucuya `requestSelectPerk` gönderir.
  - Sunucudan ret gelirse perki yerel olarak geri alır, kabul gelirse kalıcı kılar.
- **Sunucu:** `scripts/gamemode.js`
  - Oyuncu puanını ve perklerini `data/players/{profileId}.json` içinde saklar.
  - Giriş yapan oyuncuya `syncPerks` ile mevcut perklerini otomatik yükler.

---

## 3. Ağ ve Port Topolojisi

| Port | Protokol | Kullanım Alanı | Tanım |
| :--- | :--- | :--- | :--- |
| **7777** | UDP | Oyun Sunucusu | Native SkyMP çok oyunculu senkronizasyon protokolü |
| **3000** | TCP / HTTP | Kaynak ve Mod Dağıtımı | Manifest, `modpack-version.json`, `modpack.zip` |
| **3001** | UDP | Ses Yönlendiricisi | 3D Uzamsal Yakınlık Sesli Sohbet (Proximity Voice) |

---

## 4. Temel Komutlar ve İş Akışları (Cheat Sheet)

Tüm komutlar depo kökünde (`C:\Users\kerim\Documents\ChatGPT\SkyMPTR Test`) çalıştırılmalıdır:

### 1. Sunucu Yönetimi
- **Sunucuyu Başlatma:**
  ```powershell
  .\scripts\start-local-server.ps1
  ```
- **Sunucu Sağlık ve Manifest Testi:**
  ```powershell
  node .\scripts\test-local-server.mjs
  ```

### 2. İstemci Derleme ve Test
- **TypeScript İstemciyi Derleme:**
  ```powershell
  .\scripts\build-client.ps1
  ```
- **Manifest / İstemci Regresyon Testleri:**
  ```powershell
  node --test skymp5-client/tests/settingsService.test.cjs
  ```
- **UI (CEF Front) Derleme:**
  ```powershell
  .\scripts\build-front.ps1
  ```

### 3. Mod Paketi ve Launcher Derleme
- **Sunucu Mod Paketini Senkronize Etme (vX paketleme):**
  ```powershell
  .\scripts\sync-server-modpack.ps1
  ```
- **Launcher ve Stock Game Paketini Derleme:**
  ```powershell
  .\scripts\build-launcher.ps1
  ```
- **Tek Komutla GitHub Release Yayınlama:**
  ```powershell
  .\scripts\publish-github-release.ps1
  ```

---

## 5. Gelecek Modeller ve Geliştiriciler İçin Kritik Kurallar (Gotchas)

**Zorunlu belge güncelleme kuralı:** Her model/geliştirici, inceleme dahil her çalışma sonunda bu belgeyi güncellemelidir. Yapılan değişiklik veya bulgular, gerçekten çalıştırılan kontroller ve sonuçları, kalan sorunlar ve sıradaki somut adım yazılmalıdır. Kaynak kodda bulunması, yerelde derlenmesi, dağıtım paketine girmesi ve oyun içinde doğrulanması ayrı durumlardır. Önceki kişinin bağlamı korunmalı; geçersiz kalan bilgiler açıkça düzeltilmelidir. Kullanıcının 7 Eylül tarihli isteğiyle eklendi.

1. **C# 5 ve CSC Uyumluluğu:**
   - `src/launcher/Program.cs`, .NET Framework 4.0 `csc.exe` ile harici IDE veya SDK olmadan derlenir.
   - **`catch` veya `finally` blokları içinde kesinlikle `await` KULLANILAMAZ (CS1985 hatası).**
   - C# 6+ özellikleri (ör. string interpolation `$"..."`, expression bodied methods `=>`, null-conditional `?.`) `csc.exe` sürümüne göre derleme hatası verebilir; `string.Format` ve klasik `if (x != null)` tercih edilmelidir.
2. **WPF İş Parçacığı Kuralları (Dispatcher Thread Affinity):**
   - UI elemanlarına (`ProgressBar`, `TextBlock`, `Button`) asenkron veya arka plan iş parçacığından erişirken daima `window.Dispatcher.Invoke(...)` kullanılmalıdır.
3. **MO2 ve PowerShell İşlem Çakışması (`CREATE_BREAKAWAY_FROM_JOB`):**
   - MO2 veya SKSE süreçlerini başlatırken PowerShell'de `Start-Process -Wait` **KULLANILMAMALIDIR**. PowerShell job nesnesi MO2'nin breakaway bayrağıyla çakışarak Error 5 üretir. `-PassThru` ile process nesnesi alınıp `$p.WaitForExit()` kullanılmalıdır.
4. **Sürüm Sabitlemesi (1.6.1170 vs 1.7):**
   - Lab ortamı ve Stock Game **Skyrim 1.6.1170** sürümüne sabitlenmiştir. Steam'deki 1.7.x dosyaları doğrudan çalıştırılamaz; başlatıcı otomatik olarak 1.6.1170 ikililerine downgrade eder.
5. **Oyuncu Kimlik İzolasyonu (`profileId`):**
   - Çok oyunculu testlerde aynı `profileId`'ye sahip istemciler aynı karakteri yönetmeye çalışır. Her oyuncunun `skymp5-client-settings.txt` içinde benzersiz bir `profileId`'si olmalıdır. Mevcut kod GUID kullanmıyor: `new Random().Next(10002, 99999)` kullanıp dosyada saklıyor; sunucuda çakışma kontrolü yok. Benzersizlik garanti edilmiş değildir.

---

## 6. Bir Sonraki Aşama ve Yapılacaklar (Roadmap)

1. **2 Oyuncunun Gerçek Canlı Testi:**
   - İki farklı istemcinin (Sunucu Sahibi + Radmin VPN ile bağlanan Arkadaş) oyuna girmesi.
   - Karakter modelleri, hücre geçişleri, envanter, 3D ses ve metin sohbetinin canlı doğrulanması.
2. **Gelişmiş Perk Ağaçları ve Özel Meslekler:**
   - Custom Skills Framework (CSF) mimarisi veya oyun içi perk ağacı manipülasyonu.
   - Madencilik, terzilik, aşçılık gibi RP mesleklerinin sunucu mantığına eklenmesi.
3. **Sunucu Tarafı Kalıcı Ekonomi:**
   - Para transferi, dükkanlar ve oyuncular arası ticaret.

## 7. Codex kaynak incelemesi — 7 Eylül 2026

İncelenen taban `b280ec9b`; önceki Codex commit'i `4d66f079` sonrasındaki değişiklikler esas alındı. **Karar: REQUEST CHANGES.** [Ayrıntılı inceleme ve düzeltme önerileri](reviews/2026-09-07-antigravity-review.md).

- Launcher, sohbet, ses ve perk bileşenleri kaynakta mevcut; üstteki işlev anlatımları tamamlanmış oyun içi kabul testleri olarak okunmamalı. Bu inceleme canlı oyunu, sunucuyu veya yayımlanmış release paketlerini değiştirmedi/yeniden doğrulamadı.
- İzole kontrollerde gamemode sohbet/perk olaylarında `userProfiles is not defined`, perk sunucusunda tanımsız `0xDEADBEEF` kabulü ve son puanla alınan perkin tekrar isteğinde ret üretildi. Oyun oturumu olmadan UDP heartbeat kaydına ses iletilebildi (sahte ağ/oyuncu nesneleriyle).
- C# launcher kaynak kodu .NET Framework CSC ile ayrı inceleme klasörüne derlendi. Gerçek `ExtractEntryIfDifferent` metodu çağrıldığında aynı boyuttaki `new-data` yerine `old-data` kaldı. Paketleme betiğindeki `Copy-Item -LiteralPath ... '*'` ifadesinin hata verdiği küçük dosya örneğinde doğrulandı.
- Kaynak incelemesinde ayrıca ZIP hedef yolunun sınırlandırılmadığı, 1.7 ESM'lerinin yalnızca EXE değiştirilerek 1.6 tabanıyla karıştırıldığı, temiz sunucu hazırlamanın yeni gamemode/TS sunucu çıktısını kurmadığı görüldü. Mevcut Skyrim.esm boyutları Steam'de 249752131, lab'da 249753412 bayt; kurulum eşdeğerliği varsayılamaz.
- Öncelik: oyuncu kimliği/olay akışını tek sunucu uygulamasında bağlamak; güncelleme bütünlüğünü ve hedef yollarını düzeltmek; perk koşullarını ve tekrar isteklerini doğrulamak; ses oturumunu oyun girişine bağlamak. Sonra temiz kurulum ve iki oyuncu kabul testi.
- Bu turda yalnızca belgeler değişti. İşlevsel hatalar henüz düzeltilmedi. Ham izole inceleme araçları `.local/review-antigravity` altında; oyun varlıkları ve test çıktısı exe Git dışında.
- Kayıt yolu için mevcut çözüm `LocalSaves=false` ve `Saves\\` kullanıyor; eski belgelerdeki profil başına kayıt izolasyonu artık geçerli değil. Güncel test yalnızca yol hizasını doğruluyor. Kullanıcı sorunlarını paylaşınca mevcut oyun durumunu esas alarak devam edin.

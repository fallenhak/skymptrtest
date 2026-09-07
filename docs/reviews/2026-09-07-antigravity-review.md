# Antigravity değişiklikleri — kod incelemesi

**Verdict:** REQUEST CHANGES  
**Confidence:** HIGH (kaynak ve belirtilen izole örnekler için)  
**Kapsam:** `4d66f079..b280ec9b`, özellikle launcher, dağıtım, sohbet, ses ve perk akışları. Canlı release ikililerinin kaynakla eşleşmesi ve iki oyunculu oyun davranışı bu incelemede doğrulanmadı.

## Özet

Kurulum ve iletişim için yeni bileşenler eklenmiş; fakat bağlantıları, veri doğrulaması ve güncelleme mantığında düzeltme gerektiren hatalar var. Derlenebilir olması bu işlevlerin uçtan uca çalıştığını göstermiyor. Bu çalışma işlevsel kodu değiştirmeyen bir incelemedir.

## Bulgular

| Öncelik | Bulgu | Konum |
| --- | --- | --- |
| P1 | R1: Gamemode oyuncu eşlemesi tanımlanmamış | `scripts/gamemode.js:114,396,445` |
| P1 | R2: Aynı boyuttaki değişmiş dosyalar güncellenmiyor | `src/launcher/Program.cs:1116` |
| P1 | R3: ZIP girdileri kurulum dışına yazabiliyor | `src/launcher/Program.cs:935` |
| P1 | R4: Perk ID ve ön koşullar sunucuda doğrulanmıyor | `skymp5-server/ts/systems/perkSystem.ts:60` |
| P1 | R5: Tekrarlanan perk isteği kabul edilmiş perki geri aldırıyor | `skymp5-server/ts/systems/perkSystem.ts:73` |
| P1 | R6: Ses uç noktası oyun oturumuna bağlanmıyor | `scripts/gamemode.js:465` |
| P1 | R7: Downgrade ESM sürümünü eşitlemiyor | `src/launcher/Program.cs:697,764` |
| P1 | R8: Launcher paketleme son kopyalama adımında hata veriyor | `scripts/build-launcher.ps1:153` |
| P1 | R9: Temiz sunucu kurulumu yeni özellikleri kurmuyor | `scripts/prepare-local-server.ps1:77` |
| P2 | R10: Ses yönü oyuncunun bakış yönünü hesaba katmıyor | `src/launcher/Program.cs:1411` |
| P2 | R11: Dört saniyeyi aşan sağlıklı indirme iptal ediliyor | `src/launcher/Program.cs:904` |
| P2 | R12: Mod paketi etkin profil yerine bütün mod klasörlerini topluyor | `scripts/sync-server-modpack.ps1:42` |
| P2 | R13: Profil testi başlattığı süreç dışındaki MO2'leri de kapatabilir | `scripts/test-game-profile.ps1:35` |

## Ayrıntılar ve önerilen düzeltmeler

### R1 — Oyuncu kimlik eşlemesi

Gamemode içinde `userProfiles` okunuyor, yazılıyor ve siliniyor; tanımı veya login/spawn olayından doldurulması yok. Sohbet girdisi ve perk eşitleme isteği gerçek dosyanın VM içinde çalıştırıldığı izole örnekte `ReferenceError` verdi. Ayrı TS `PerkSystem.userToProfile` haritası bu modülün değişkeni değildir.

**Düzeltme:** Kimliği doğrulanmış spawn/login akışından türetin ve sohbet/perk/ses için tek bir oturum eşlemesi kullanın. Sadece boş Map eklemek yetmez; `content.profileId || 1` ile başka karaktere düşülmemeli. TS sistemi ile gamemode'daki iki ayrı perk işleyicisi tekleştirilmeli.

### R2 — Dosya boyutu içerik eşitliği değildir

`ExtractEntryIfDifferent` ve `CopyFileIfDifferent` yalnızca uzunluğu karşılaştırıyor. Sekiz baytlık eski `old-data` dosyası ile sekiz baytlık yeni `new-data` ZIP girdisi için gerçek derlenmiş launcher metodu çağrıldı: güncelleme atlandı ve eski içerik kaldı. Sonrasında kurulum sürümü güncel yazılabildiğinden tekrar deneme de tetiklenmeyebilir. Özellikle aynı uzunlukta JS/INI değişiklikleri etkilenir.

**Düzeltme:** Dosya içerik hash'ini kullanın veya doğrulanmış güncelleme paketini kontrollü biçimde üzerine açın. `installed-version.json` yalnızca uygulanan dosyalar doğrulandıktan sonra atomik yazılmalı; resume ile güncelleme aynı kısa yolu paylaşmamalı.

### R3 — ZIP hedef yolu doğrulanmıyor

Hem ilk kurulum hem mod güncellemesi `Path.Combine(dest, entry.FullName)` sonucuna doğrudan yazıyor. `..` içeren veya mutlak bir girdi kurulum kökünün dışına çıkabilir. Mod güncellemesi HTTP'den geliyor; indirilen paketin hash'i de açılmadan önce hesaplanıp karşılaştırılmıyor. Bu bulgu kaynak akışından saptandı; gerçek sisteme zararlı arşiv uygulanmadı.

**Düzeltme:** Her hedefi `Path.GetFullPath` ile çözümleyin; kök + dizin ayıracı altında kaldığını doğrulayın ve mutlak/geçiş içeren girdileri reddedin. Paketi güvenilir bir manifest/hash veya imza ile doğrulayın. GitHub fallback kullanıldığında dosya ile sürüm/hash bilgisinin aynı yayından geldiğini kontrol edin.

### R4 — Sunucu yalnızca puan kontrol ediyor

`PerkSystem.customPacket` sayısal, sıfır olmayan bir ID'yi yeterli puan varsa kaydediyor. Perkin mevcut olması, izinli ağaçta bulunması, skill seviyesi ve önceki rank/bağlantı koşulları doğrulanmıyor. İzole örnekte `0xDEADBEEF` kabul edilip kalıcı JSON'a yazıldı. Gamemode kopyasında da aynı doğrulama eksikliği var.

**Düzeltme:** Sunucuya ait perk tanımları ve ön koşullar üzerinden doğrulayın; istemcinin menüsünü yetki sınırı saymayın. İstemci menüsü, sunucu kayıtları ve tarif/etki hesapları aynı tanımlara dayanmalı. Bu haliyle belge bunu tam sunucu onaylı perk sistemi olarak nitelememeli.

### R5 — Son puandan sonra tekrar istek

Puan kontrolü `perks.includes` kontrolünden önce. Son puanla alınmış perkin isteği tekrar geldiğinde sunucu onu zaten sahip olunan perk olarak eşitlemek yerine reddediyor. İstemci ret üzerine perki kaldırıp bir puan ekliyor; sunucu perki tutmaya devam ediyor. İzole TS örneği kabul → tekrar → `selectPerkRejected` sırasını doğruladı.

**Düzeltme:** Zaten sahip olunan perk/işlenmiş istek kontrolünü harcamadan önce yapın. Kabul ve ret sonrasında otoriter perk/puan durumu dönün; istemcide körlemesine `addPerkPoints(1)` kullanmayın. Tekrar, yeniden bağlantı ve gecikmiş yanıt testleri ekleyin.

### R6 — Ses kimliği istemcinin beyanına dayanıyor

Her UDP heartbeat, bildirilen profileId için adres/port kaydını değiştirebiliyor. O profile ait etkin oyun oturumu veya oturum token'ı istenmiyor. İzole örnekte hiçbir oyun login olayı gerçekleşmeden iki endpoint arasında ses yönlendirildi. Bilinen bir profil ID'siyle kaydolmak dinleyici adresini değiştirebilir; aktör/konum bulunamadığında ise kodun varsayılan mesafesi sıfır ve filtreler atlanabiliyor.

**Düzeltme:** Oyun girişinde üretilen kısa ömürlü oturum anahtarıyla ses kaydını doğrulayın. Kopan oturumları silin; geçerli aktör/konum yoksa ses göndermeyin. Endpoint sahipliği doğrulanmadan profileId kaydını değiştirmeyin.

### R7 — 1.7 verisi ile 1.6 çalıştırıcısı karışıyor

Launcher temel ESM/BSA dosyalarını kullanıcının seçtiği kaynaktan kopyalıyor; downgrade yalnızca EXE/bink dosyalarını değiştiriyor. Bu makinede Steam Skyrim.esm 249752131, 1.6 lab dosyası 249753412 bayt. Sunucu lab master'larını kullanırken bu istemci dosyaları mevcut CRC/boyut kontrolünde eşleşmez. Çalıştırıcı sürümünün doğru olması veri eşleşmesini sağlamaz.

**Düzeltme:** Kurulum başlamadan sunucunun master manifestini doğrulayın. Desteklenen aynı veri tabanı sağlanmadan kurulumu başarılı saymayın; yalnızca EXE değişikliğini tam sürüm eşitleme olarak sunmayın. Master dosyaları eksikse de sessizce devam edilmemeli.

### R8 — LiteralPath joker karakteri açmaz

`Copy-Item -LiteralPath (Join-Path $buildDir '*')` gerçek adı `*` olan bir yol arıyor. Küçük dosya örneği aynı “path does not exist” hatasını verdi. `$ErrorActionPreference='Stop'` nedeniyle standalone klasöre teslim adımı ve dolayısıyla standart yayın akışı kesilir; eski çıktılar varsa kullanıcı güncel derleme yerine eskisini kullanabilir.

**Düzeltme:** `Get-ChildItem -LiteralPath $buildDir` ile dosyaları listeleyip her birini açık hedefe kopyalayın. Temiz çıktı klasörüyle paketleme kontrolü ekleyin; yalnızca CSC derlemesi yeterli değildir.

### R9 — Kaynak ile yeniden kurulan sunucu ayrışıyor

Yeni `PerkSystem` kaynak `index.ts` dosyasına eklenmiş; fakat `prepare-local-server.ps1` halen sabit eski upstream JS çıktısını kuruyor ve tek satırlık hazır olma gamemode'u yazıyor. `start-local-server.ps1` yalnızca mod paketini eşitleyip bu JS'yi başlatıyor; yeni `scripts/gamemode.js` aktarımı veya yeni TS sunucu derlemesi yok. Mevcut makinedeki elle hazırlanmış dosyalar başka geliştiricinin temiz kurulumuna taşınmıyor.

**Düzeltme:** Sunucu TS derleme/deploy adımını sabitleyin; yeni gamemode'u açıkça kurun ve kurulan kaynak commit/hash'ini kaydedin. Dünya verisine dokunmadan temiz runtime örneğiyle sohbet/perk girişini doğrulayın.

## Diğer iyileştirmeler

- **R10:** Panning yalnızca dünya X/Y farkına bakıyor. Dinleyici 180 derece döndüğünde sesin kulağı değişmiyor. Göreli vektörü dinleyicinin yaw açısıyla yerel koordinata dönüştürün.
- **R11:** Dört saniye bağlantı süresi değil, bütün ZIP aktarım süresi olarak uygulanıyor. Normal büyük indirme kesilip GitHub'a düşüyor; fallback paketi sunucu sürümü diye işaretlenebilir. Bağlantı/ilerleme timeout'larını ayırın ve fallback manifestini birlikte alın.
- **R12:** Devre dışı modlar da ekleniyor, MO2 önceliği yerine alfabetik sıra kullanılıyor. Silinen modun eski dosyaları kurulumdan kaldırılmıyor. Etkin `modlist.txt` sırası ve paket sahiplik manifesti kullanılmalı.
- **R13:** Test başlangıçta MO2 açık olmasını reddetse de daha sonra ad bazında bütün MO2 süreçlerini beş kez zorla kapatıyor. Arada açılan başka profil de kapanabilir. Yalnızca `$process.Id` ve doğrulanmış executable yolu üzerinde işlem yapın.

## Doğrulama ve sınırlar

- Gamemode gerçek kaynak dosyası, sahte dosya sistemi/ağ/oyuncu nesneleriyle çalıştırıldı; gerçek UDP portu veya dünya verisi kullanılmadı.
- PerkSystem gerçek TS kaynağı transpile edilip izole olaylarla çağrıldı.
- Launcher gerçek kaynak kodu CSC ile derlendi; uygulama arayüzü açılmadan özel statik güncelleme metodu reflection ile çağrıldı.
- Yerel inceleme araçları ve sonuçları `.local/review-antigravity` içinde. Yeni runtime kurulumu, büyük paketleme, canlı release indirme ve iki oyunculu oyun testi yapılmadı.
- Mevcut CI yalnızca istemci/UI derlemesi, eski manifest testleri ve betik sözdizimini kapsıyor; yeni sunucu/launcher davranışlarını kapsayan kontroller eklenmeli.

## Öneri

Önce R1/R8/R9 ile tekrarlanabilir çalışan tabanı sağlayın. Ardından R2/R3/R7 ile kurulum ve güncellemeyi, R4/R5/R6 ile sunucu durumunu doğrulayın. Kullanıcının oyun içi sorunları bu bulgularla birlikte önceliklendirilebilir; kapsamlı bir yeniden yazım gerekmiyor.

# Ayrı Skyrim oyun testi

Bu akış **1.6.1170.0** için sabitlenmiştir. Kullanıcının Faalgrin `Stock Game` kopyasında bu sürüm doğrulandı. Oyun varlıkları ve üçüncü taraf arşivleri repoya eklenmez; her geliştirici kendi yasal oyun kurulumunu kullanır. Önce ayrı oyun kopyasını, ardından [yerel sunucuyu](LOCAL_SERVER_TR.md) aynı kopyanın ESM'leriyle hazırlayın.

## Derleme ve kurulum

```powershell
.\scripts\build-client.ps1
.\scripts\build-front.ps1
.\scripts\prepare-local-client.ps1 -ProfileId 1
```

Sabitlenen bağımlılıklar SKSE **Steam 2.2.8**, Address Library **All in One v13** ve Mod Organizer **2.5.2 .7z**. Address Library 13 paketi 1.6.1170 dosyasını da içerir. Betik `sources.lock.json` içindeki SHA-256 değerlerini kontrol eder. SKSE 2.2.8 bu makinenin mevcut indirme önbelleğinde bulundu; readme'si 1.6.1170 desteğini belirtiyor, kurulu loader'ın Ian Patterson imzası geçerli. Başka bilgisayarda aynı sürüm resmi Nexus sayfasının arşivinden temin edilmeli; güncel 2.3.1 sessizce yerine kullanılamaz.

```powershell
.\scripts\prepare-game-lab.ps1 `
  -SkyrimDirectory 'C:\Users\kerim\Games\Faalgrin\Modlist\Stock Game' `
  -SkseArchive 'C:\Users\kerim\Games\Faalgrin\Downloads\Skyrim Script Extender (SKSE64) Steam 30379 2.2.8 2026-08-20T08-07Z s6Og0dF9H.7z' `
  -AddressLibraryArchive '.research\dependencies\address-library-13.7z' `
  -ModOrganizerArchive '.research\dependencies\Mod.Organizer-2.5.2.7z'
```

Yollar bu makinenin örneğidir; kendi 1.6.1170 kopyanızı kullanın. Betik temel beş ESM'yi, temel BSA arşivlerini ve oyun çalıştırıcılarını `.local/skymp-756fb86/skyrim-1.6.1170/lab-player-1/game` içine normal dosya kopyası olarak alır. SKSE, Address Library, resmi SkyMP native çıktısı ve bizim derlediğimiz istemci/UI burada kurulur. Faalgrin'in mod listesi, özel eklentileri veya sunucu özellikleri aktarılmaz. Skyrim.ccc boş tutulur. Orijinal oyun klasörüne yazılmaz; var olan lab/profil üstüne kurulum reddedilir.

Taşınabilir MO2, aynı lab'ın `mod-organizer` klasöründedir; `SkyMPTR` profili kendi INI dosyalarını, yükleme sırasını ve kayıtlarını kullanır. Grafik ayarları oyunun Medium şablonundan, 1280×720 pencere olarak hazırlanır. Normal oynadığınız karakterler bu profile kopyalanmaz. MO2/SKSE'nin kendi tanılama günlükleri ayrıca oluşabilir.

Sunucunun `loadOrder` yolları da bu ayrı kopyanın beş ESM'sine işaret etmeli. Steam 1.7 ESM'leri ile 1.6 istemci verileri bu makinede farklı boyuttadır; iki sürümü karıştırmayın. Yeni sunucu hazırlarken `prepare-local-server.ps1 -SkyrimDirectory '<ayrı lab oyun klasörü>'` kullanın. Var olan dünya/ayarlar korunarak yalnızca gerekli yollar güncellenmeli.

## Çalıştırma

Steam açıkken iki ayrı PowerShell terminalinde:

```powershell
.\scripts\start-local-server.ps1
```

```powershell
.\scripts\start-game-lab.ps1 -ProfileId 1
```

Başlatma betiği MO2 üzerinden SKSE'yi çalıştırır. `skse64_loader.exe` doğrudan açılırsa MO2'nin profil yönlendirmesi uygulanmaz. İstemci offline profil kimliğiyle otomatik bağlanmayı dener; IP yazmak veya bir sunucu seçme menüsü açmak gerekmez. İlk denemede profil 1 ile sunucu girişi kaydedildi; dünyaya giriş ayrıca doğrulanmalı. UI, repodaki widget arayüzüdür; native perk menüsü geliştirmesi henüz yapılmadı.

Elle başlatmak için lab içindeki `mod-organizer/ModOrganizer.exe` dosyasını açın, **SkyMPTR** profilini ve **SKSE** çalıştırıcısını seçip **Çalıştır** düğmesine basın. Bu kurulumda dosyalar ayrı oyunun `Data` klasöründedir; MO2'nin sol mod listesinin boş olması beklenir. Oyun yolunun aynı lab içindeki `game` klasörü olduğunu kontrol edin.

## Kontrol ve kanıt sınırı

```powershell
.\scripts\check-client-prerequisites.ps1 `
  -SkyrimDirectory '.local\skymp-756fb86\skyrim-1.6.1170\lab-player-1\game'
.\scripts\test-game-profile.ps1
```

Dosya kontrolü yalnızca dosyaların bulunduğunu gösterir. Gerçek aşamalar: MO2 ayar/kayıt yönlendirmesi, SKSE ve native DLL yüklenmesi, yerel sunucuya bağlanma, karakter oluşturma ve dünyaya girme. Bunlar ayrı ayrı kaydedilmelidir. İki oyuncu ve perk etkisi tek istemci başlangıcından çıkarılamaz.

`test-game-profile.ps1`, MO2 üzerinden Node kontrolünü çalıştırır; sanal INI/plugin dosyalarını ve kayıt yazımının yalnızca profil `saves` klasörüne gitmesini doğrular. Gerçek kullanıcı ayarlarının önce/sonra özetlerini de karşılaştırır. Bu kontrol Skyrim native uyumunu kanıtlamaz.

7 Eylül'de 1.6.1170 lab'ın dosya ve gerçek profil kontrolleri geçti. MO2 temel master'ları otomatik etkinleştirdiği için `plugins.txt` boş olabilir; kontrol beş master'ın `loadorder.txt` sırasını ve ek etkin plugin bulunmadığını doğrular. [Taşınabilir test kaydı](evidence/2026-09-07-game-lab.json).

## MO2 “Cannot start” / Error 5 düzeltmesi

İlk betik sürümü `Start-Process -Wait` kullanıyordu. MO2 hem `node.exe` hem `skse64_loader.exe` başlatırken `ERROR_ACCESS_DENIED` verdi. MO2'nin [başlatma kodu](https://github.com/ModOrganizer2/modorganizer/blob/v2.5.2/src/spawn.cpp) `CREATE_BREAKAWAY_FROM_JOB` kullanır; PowerShell'in `-Wait` işlem grubu ile bu bayrağın [bilinen çakışması](https://github.com/PowerShell/PowerShell/issues/1748) aynı hatayı üretir. Betikler şimdi `Start-Process -PassThru` ve ardından yalnızca MO2 için `WaitForExit()` kullanır. Bu değişiklikten sonra Node başlatma ve gerçek profil testi geçti; antivirüs/Windows güvenlik ayarı değiştirilmedi. Genel hata kutusu tek başına antivirüs engelinin kanıtı değildir.

Önceki 1.7.104 lab'ı `.local/skymp-756fb86/lab-player-1` altında tanılama için korundu. Orada görülen başlangıç hatası ve bekletilen kaynak yaması [native uyumluluk notunda](NATIVE_COMPATIBILITY_TR.md) kayıtlıdır.

MO2 davranışı [resmi profil kodu](https://github.com/ModOrganizer2/modorganizer/blob/v2.5.2/src/profile.cpp), [komut satırı kodu](https://github.com/ModOrganizer2/modorganizer/blob/v2.5.2/src/commandline.cpp) ve sürümle gelen kaynak arşivinden kontrol edildi. Sonuçlar ve sıradaki iş için [ortak durum belgesini](HANDOFF_TR.md) okuyun.

## Anniversary Edition indirme ekranı ve ek içerik

Bu lab yalnızca beş temel master kullanır. `SkyrimPrefs.ini` dosyasının `[General]` bölümündeki `bFreebiesSeen=1`, AE indirme teklifinin tekrar gösterilmesini önlemek için hazırlanır. Ayarın davranışı [Step Mods INI incelemesinde](https://stepmodifications.org/wiki/Guide:SkyrimPrefs_INI/General#bFreebiesSeen) açıklanır; 1.6.1170 EXE içinde anahtar da bulundu. Bu makinede ayar eklendi, sonraki açılışta ekran sonucu bekleniyor.

MO2 üzerinden yanlışlıkla indirilen CC paketleri `overwrite` içine düşebilir. Skyrim ve MO2 kapalıyken yalnızca lab’a yeni eklenen `cc*.esl/esm/esp/bsa` dosyalarını lab içindeki bir yedeğe taşıyın; bütün `overwrite` klasörünü silmeyin. Profilin yükleme sırasını beş master’a geri döndürün ve `test-game-profile.ps1` çalıştırın. İlk olayda sekiz CC dosyası yedeklendi, diğer ayarlar korundu ve profil testi geçti. Güncel hata ve doğrulama durumu HANDOFF_TR.md bölüm 9’da kayıtlıdır.

## SkyMP native kayıt yolu ve MO2 profil hizalaması

SkyMP'nin resmi native çalışma eklentisi (`SkyrimPlatformImpl.dll` / `LoadGame.cpp`), sunucu bağlantısında oyuncuyu dünyaya sokmak için geçici bir bootstrap kaydı (`TESMODPLATFORM-<GUID>.ess`) oluşturur ve `saveLoadManager->Load` çağırır. Bu yol C++ binary içinde sabit olarak `Documents\My Games\Skyrim Special Edition\Saves\` altına yazılır ve dünyada 5 saniye sonra `LoadGameEventSink` tarafından otomatik silinir.

MO2 profilinde `LocalSaves=true` ve `sLocalSavePath=__MO_Saves\` ayarlandığında, Skyrim motoru kaydı `__MO_Saves\` içinde arar fakat native DLL fiziksel `Saves\` altına yazdığı için otomatik yükleme başarısız olur ve ana menüde kalınır. Bu nedenle lab profili `LocalSaves=false` ve `sLocalSavePath=Saves\` olarak yapılandırılır; INI (`LocalSettings=true`) ve plugin izolasyonu korunur.

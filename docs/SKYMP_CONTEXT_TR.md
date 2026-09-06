# SkyMP — yerel senkronizasyon geliştirme bağlamı

İnceleme tarihi: **5 Eylül 2026**. Bu belge kaynak kodu, proje belgeleri, GitHub CI/issue bilgileri ve bilgisayardaki geliştirme araçlarının incelenmesine dayanır. Resmi Windows sunucu çıktısıyla yerel başlangıç testi geçti: native modül yüklendi, gamemode çalıştı ve beş temel ESM'nin HTTP manifesti doğrulandı. Kaynak derlemesi ve oyun içi bağlantı testi henüz yapılmadı. Güncel kurulum adımları `LOCAL_SERVER_TR.md`, kullanıcı tarafından belirlenen perk öncelikleri `PERK_SYSTEM_PLAN_TR.md` belgesinde.

**Amaç ve kapsam**

İki geliştirici olarak SkyMP'nin eksik veya sorunlu senkronizasyonlarını yerelde yeniden üretmek, düzeltmek ve iki oyuncuyla doğrulamak istiyoruz. Faalgrin ve Keizaal bu çalışmanın motivasyonu; bu sunucuların özel kaynak koduna erişimimiz yok. İlk çalışma tabanımız doğrudan açık SkyMP deposu olacak. Buradaki düzeltmelerin başka sunuculara aktarılabilirliği, onların kullandığı commit ve değişikliklerle sonradan değerlendirilir.

Güncel öncelik: **çalışan yerel altyapı → Skyrim'in kendi menüsünde gerçekten çalışan ve kalıcı perkler → RP dengesi ve yeni meslek ağaçları**. Simyacılık ve demirciliğin yanında madencilik, terzilik ve aşçılık hedefleniyor. Aşağıdaki genel senkronizasyon adayları bekleme listesi; ilk özellik seçimi değiller.

**İncelenen sürüm ve kaynakların güvenilirliği**

- Kaynak: [skyrim-multiplayer/skymp](https://github.com/skyrim-multiplayer/skymp).
- Dal: `main`; commit: [`756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3`](https://github.com/skyrim-multiplayer/skymp/commit/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3), 18 Ağustos 2026.
- Skyrim Platform paket sürümü: `2.9.0`.
- İnceleme kopyası: `C:\Users\kerim\Documents\ChatGPT\SkyMPTR Test\.research\upstream-skymp`. Bu, geçmişi sınırlı bir araştırma klonudur; `vcpkg` alt modülü indirilmedi. İleride ortak geliştirme için tam geçmişli fork kullanılmalı.
- `vcpkg` sabitlemesi: `cb2981c4e03d421fa03b9bb5044cd1986180e7e4`. Sistemden rastgele güncel vcpkg seçmek yerine depo sabitlemesi korunmalı.
- Ana dal için [5 Eylül Windows Flatrim CI çalışması](https://github.com/skyrim-multiplayer/skymp/actions/runs/33943379757) başarılı. `dist` yaklaşık 177,6 MiB, `server-dist` yaklaşık 25 MiB; ayrıca Skyrim Platform ve istemci JS çıktıları mevcut. `server-dist` indirildi, commit eşleşmesi kontrol edildi ve yerelde başlangıç testi geçti. Paketteki CI test gamemode'u çalışma kopyasında değiştirildi. İstemci çıktıları henüz kurulmadı; artifact'lerin tam oynanabilir RP paketi olduğu varsayılmamalı.
- [Linux CI çalışmasında](https://github.com/skyrim-multiplayer/skymp/actions/runs/32167532600) Ubuntu 24.04, Ubuntu 25.10 ve hazır bağımlılık imajı işleri başarılı; Arch işi CMake yapılandırmasında başarısız. Toplam kırmızı durum bütün Linux sunucu derlemelerinin bozuk olduğu anlamına gelmiyor.
- GitHub Releases içinde görünen `sp-v2.6-beta` 2022 tarihli Skyrim Platform ön sürümü. Güncel SkyMP istemci/sunucu eşleştirmesi için başlangıç kabul edilmemeli.

Belgeler arasında belirgin sürüm farkları var. `ROADMAP.md` son olarak 28 Haziran 2023'te değişmiş. Eski sunucu belgesindeki “yalnızca Windows” ifadesi güncel Linux CI ile çelişiyor; eski Node 17/Clang 15 önerileri de güncel Dockerfile ile örtüşmüyor. Bu yüzden mevcut CMake, kod ve testler uygulamanın durumunu değerlendirmede öncelikli. [Derleme belgesi](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/CONTRIBUTING.md), [Dockerfile](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/Dockerfile), [yol haritası](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/ROADMAP.md).

**Senkronizasyonun geçtiği katmanlar**

| Katman | Görevi | Bu çalışma için kullanım |
| --- | --- | --- |
| `skyrim-platform` | Skyrim/SKSE ile C++ bağlantısı; oyun olaylarını ve API'lerini JS/TS'ye sunar | Eksik oyun olayı, native çağrı, sürüm uyumu, hook ve iş parçacığı sorunları |
| `skymp5-client/src/services`, `sync`, `view` | Yerel olayları paketler; uzaktaki oyuncu ve nesne durumunu oyuna uygular | Hareket, animasyon, ekipman, envanter, büyü ve nesne görünümü |
| `skymp5-server/cpp` | Paket işleme, doğrulama, aktör/nesne durumu, hasar ve kayıt | Sunucu tarafındaki senkronizasyon ve oyun durumu düzeltmeleri |
| `skymp5-server/ts` | Node sunucu başlangıcı, giriş, spawn, ayarlar ve gamemode yükleme | Yerel test oturumu ve deneylerin hazırlanması |
| `libespm`, `papyrus-vm`, `savefile`, `serialization` | Oyun kayıtlarını okuma, script çalıştırma ve veri dönüşümü | Sorun veri biçimi veya Papyrus davranışından kaynaklanıyorsa |
| `unit`, `misc/tests` | Catch2 testleri ve sunucu entegrasyon senaryoları | Hatanın yeniden oluşmasını engelleyen doğrulama |

Tipik akış: **oyun olayı → Skyrim Platform → TypeScript istemci → ağ paketi → C++ sunucu durumu → diğer istemcilerde uygulama**. Sunucunun Node süreci native `scam_native.node` modülünü kullanır. Ana C++ standardı C++20. NPC kontrolü gibi bazı davranışlarda istemci host mekanizması da bulunur; her fizik/AI işlemini sunucu hesaplıyor varsayımı yapılmamalı. [Sunucu başlangıcı](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-server/ts/index.ts), [native derleme](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-server/CMakeLists.txt), [istemci görünümü](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-client/src/view/formView.ts).

Bu hedef için C++ sunucu kodu ve TypeScript istemci kodu birlikte önemli. `mp.makeProperty`, `mp.makeEventSource`, `mp.get/set` API'leri deney hazırlamak için kullanılabilir. Kalıcı protokol/durum düzeltmesi gerekiyorsa yalnızca bir gamemode betiğiyle yetinmek doğru sonucu vermeyebilir. Yeni durumun ilk bağlantıda, yeniden bağlantıda, görünürlük alanına girişte ve sunucu yeniden başladığında da taşınması gerekir. [Script API](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/docs/docs_serverside_scripting_reference.md).

**Bu bilgisayardaki durum**

| Bileşen | Gözlenen durum | Sonuç |
| --- | --- | --- |
| Skyrim | Steam kurulumu, `C:\Games\steamapps\common\Skyrim Special Edition`, EXE `1.7.104.0` | SkyMP uyumu henüz doğrulanmadı |
| Temel oyun verileri | Beş temel ESM mevcut; ayrıca Creation Club dosyaları var | İki istemci ve sunucu aynı dosya sürümlerini ve mod sırasını kullanmalı; mevcut dosyaların etkin yükleme sırası incelenmedi |
| Visual Studio | Community 2026, MSVC `14.51.36231` | Proje CMake kodu özellikle `Visual Studio 17 2022` generator'ünü şart koşuyor |
| CMake | Normal PATH'te yok; VS 2026 içinde `4.3.1-msvc1` var | VS 2022 ile kullanılacak araç zinciri ayrıca hazırlanmalı |
| Node/npm | `v24.11.1` / `11.6.2` | Resmi native sunucu çıktısıyla başlangıç testi geçti; kaynak derlenmedi. Güncel Dockerfile Node 22 kullanıyor |
| Yarn | PATH'te bulunamadı | Derleme öncesinde gerekli |
| Python | Python 3.13 kurulumu bulundu | Eski belgedeki 3.9 önerisiyle eşleşmiyor; bağımlılıkların gereksinimi derlemede doğrulanmalı |
| Git/GitHub CLI | İkisi de var, GitHub CLI oturumu açık | Fork ve ortak çalışma için CLI kullanılabilir |
| Bellek/disk | Yaklaşık 15,7 GiB RAM, C: üzerinde yaklaşık 68 GiB boş alan | İlk native bağımlılık derlemesinin gerçek alan tüketimi ölçülmedi |
| Docker/WSL | Docker PATH'te ve kontrol edilen standart konumda bulunamadı; `wsl.exe` var | WSL dağıtımı ve Docker çalışma durumu doğrulanmadı |

Kontrol edilen standart Steam oyun dizininde SKSE loader veya Platform kurulumu bulunmadı. Bu, farklı bir oyun kopyasında ya da mod yöneticisi profilinde kurulu olmadığını kanıtlamaz.

İstemci doğrulamasında **oyun + SKSE + Platform + istemci eşleşmesi** kaydedilmeli. Kullanıcı Faalgrin ve Keizaal'ın son oyun sürümünde çalıştığını bildirdi. Skyrim Platform README'sinin `1.6.1170`, `1.6.640` ve desteği geriye alınmış `1.5.97` listesinden güncel sürümün çalışamayacağı sonucu çıkmaz. Sabitlenen CommonLibSSE kaydı 2024 tarihli; SKSE'nin resmi sayfasında `1.7.104` için `2.3.1` bulunuyor. Kendi açık kaynak istemci kombinasyonumuzdaki uyum oyun içinde sınanacak; önceden downgrade kararı alınmış değil. Oyun dosyalarında değişiklik yapılmadı. [Platform sürüm listesi](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skyrim-platform/README.md), [CommonLib sabitlemesi](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/overlay_ports/commonlibsse-ng-flatrim/vcpkg.json), [SKSE](https://skse.silverlock.org/).

**Yerel test ortamının kurulma yolu**

1. Aynı SkyMP commit'inden sunucu ve istemci oluşturmak; iki bilgisayarın oyun/runtime/mod sürümlerini kaydetmek.
2. VS 2022 araç zinciri ve Yarn ile kaynak derlemesini hazırlamak. CMake build klasörü kod tarafından `<repo>/build` olarak zorunlu tutuluyor. İlk yapılandırmada `OFFLINE_MODE=ON`, `BUILD_UNIT_TESTS=ON`, `BUILD_FRONT=OFF`, `BUILD_GAMEMODE=OFF`, `INSTALL_CLIENT_DIST=OFF` açıkça sabitlenebilir. Bunlar mevcut varsayılanlarla uyumlu. [CMake seçenekleri](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/CMakeLists.txt).
3. Sunucu ve test tabanını doğrulamak. Hazır sunucu çıktısı için `node scripts/test-local-server.mjs` başlangıç ve manifest testi geçti. Sunucu birim testleri, oyunun iki kopyasını çalıştırmadan hasar/durum/paket davranışını sınayabilir; bazı testler ESM verisi ister. Başarılı kaynak derlemesi sonrasında build dizininden `ctest -C Release --output-on-failure` temel doğrulama olur. Henüz bu komut çalıştırılmadı.
4. İstemci oyun uyumunu ayrı test profiliyle doğrulamak. Test oyuncusu için yeni kayıt kullanmak. Kaynak çıktıları `build/dist/server` ve `build/dist/client` altında oluşuyor; sunucu `launch_server.bat` veya sunucu çalışma dizininden `node dist_back/skymp5-server.js` ile başlıyor.
5. İlk oyuncu host bilgisayara `127.0.0.1:7777` ile, ikinci oyuncu aynı ağdaki host IP'siyle bağlanır. Farklı ağda özel bir VPN ağı veya uygun yönlendirme gerekir; `127.0.0.1` arkadaşın bilgisayarında host sunucusunu göstermez.
6. Dosya tabanlı `world` kaydı iki kişilik deney için yeterli başlangıç. NPC yüklemesi varsayılan olarak kapalı; NPC testleri ayrıca ve sınırlı bir senaryoda açılmalı. [Kayıt sürücüleri](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/docs/docs_database_drivers.md), [ayar üretimi](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/cmake/scripts/generate_server_settings.cmake).

Offline girişte sunucuda `offlineMode: true`, istemcide `gameData.profileId` kullanılıyor. Birinci oyuncuya `1`, ikinciye `2` gibi farklı sayısal kimlikler verilmeli; aynı kimlik aynı karakteri seçebilir. Güncel istemci ayar dosyası `Data/Platform/Plugins/skymp5-client-settings.txt`; host ve port alanları `server-ip`, `server-port`. Bu mod kimlik doğrulamasını atladığı için test ağına erişim sınırlandırılmalı. [Ayar üreticisi](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/cmake/scripts/generate_client_settings.cmake), [giriş akışı](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-server/ts/systems/login.ts).

`offlineMode` bütün dış HTTP isteklerini kapatmıyor. İstemci `master` boş olsa da varsayılan gateway'e dönebiliyor. `server-info-ignore: true` sunucu adresi sorgusunu atlıyor; mod manifesti sorgusu ayrı bir yol. Tamamen yerel kullanım için manifestin yerelden alınması/karşılaştırılması ayrıca doğrulanmalı veya küçük bir lab düzeltmesiyle sağlanmalı. `ignoreLoadOrderMismatch` ile uyuşmazlığı gizlemek yerine dosya ve sıra eşitliği kontrol edilmeli. [SettingsService](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-client/src/services/services/settingsService.ts), [yükleme sırası kontrolü](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-client/src/services/services/loadOrderVerificationService.ts).

Ağ: oyun trafiği varsayılan `7777/UDP`; sunucu HTTP kaynak servisi `3000/TCP`. Ana port değişirse HTTP portu `ana port + 1` oluyor. Eski port belgesi HTTPS diyor, güncel `ui.ts` doğrudan HTTP oluşturuyor. Frontend geliştirme servisi `1234`; bu geliştirme portunun arkadaş bağlantısı için otomatik olarak açılması gerekmiyor. [Güncel HTTP kodu](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-server/ts/ui.ts).

**Arayüz ve özel gamemode bağımlılıkları**

Ana depoda eski bir React arayüz demosu mevcut. Güncel `BUILD_FRONT=ON` yolu ise ayrı `skyrim-multiplayer/skymp5-front` deposuna ve `SKYMP5_FRONT_REPO_PAT` değerine bağlı. Bu dış depo anonim GitHub API isteğinde 404 döndü; özel mi, kaldırılmış mı olduğu doğrulanamadı. Kişisel token oluşturmak tek başına erişim sağlamaz. Güncel browser entegrasyonu yerel `Data/Platform/UI/index.html` yolunu açıyor. Dolayısıyla bütün projeyi derlemenin eksiksiz güncel arayüz de ürettiği varsayılamaz. [Frontend CMake](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-front/CMakeLists.txt), [browser entegrasyonu](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skyrim-platform/src/platform_se/skyrim_platform/BrowserApiNirnLab.cpp).

Çekirdek sunucu testlerini bu dış arayüz deposuna erişmeden geliştirebiliriz. Oyun içi görsel deney için uyumlu hazır UI çıktısı veya küçük yerel bir test arayüzü değerlendirilir. Gamemode yükleyicisi yerel `gamemode.js` çalıştırabiliyor; özel RP sunucularının iş/ekonomi/karakter paketlerini elde etmemiz çekirdek çalışmasının ön koşulu değil. Deney için gereken küçük betikleri kendimiz oluşturabiliriz.

**Sonraki senkronizasyon çalışmaları için adaylar**

Aşağıdaki liste kapsam ve test edilebilirlik değerlendirmesidir; oyun içinde yeniden üretilmiş hata listesi değildir. Güncel perk hedefinden sonraki çalışmalar için saklanmıştır. Açık bir issue, bug'ın güncel sürümde kesin devam ettiğini tek başına kanıtlamaz.

| Konu | Mevcut kanıt | İlk deney ve kapsam |
| --- | --- | --- |
| Bash saldırısının hasar hesabı | Açık [#1813](https://github.com/skyrim-multiplayer/skymp/issues/1813); istemci `isBashAttack` gönderiyor, temel `TES5DamageFormula` içinde ayrı bash hesabı görünmüyor | Aynı silah/kalkanla Nord ve Khajiit bash sonuçlarını ölçmek; normal vuruş, yumruk ve power bash davranışını ayırmak. Küçük/orta kapsamlı ilk aday |
| Kilitli kapı ve sandık senkronizasyonu | Açık [#2494](https://github.com/skyrim-multiplayer/skymp/issues/2494), [#1106](https://github.com/skyrim-multiplayer/skymp/issues/1106); istemci `ObjectReferenceEx.dealWithRef` kilitli nesneye `lock(false, false)` uyguluyor | Sunucuda kilit durumu, anahtar/kilit seviyesi, etkileşim sahipliği, sonuç mesajı, kayıt ve diğer istemciye yansıtma. Orta/yüksek kapsamlı ilk özellik adayı |
| Ekipman/envanterin yeniden girişte tutarlılığı | `PartOne_UpdateEquipmentTest`, `InventoryTest`, `SaveStorageTest` ve istemci container/last-inventory akışı mevcut | İki elde silah, büyülü/iyileştirilmiş eşya, sandığa koy-al, çık-gir ve restart ile somut başarısız durum aramak. Hata henüz doğrulanmadı |
| Hareket, animasyon ve NPC host geçişleri | `movementApply.ts` içinde yerel gecikme telafisi ve `MovementValidationTest` var; `formView.ts` host yönetiyor | Gecikme altında koş-dur, hücre geçişi, NPC host oyuncusunun ayrılması; konum sıçraması ve durum ayrışmasını ölçmek. Başlangıç için daha geniş kapsam |

İlk seçenek için kod/test başlangıç noktaları: [hitService.ts](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-client/src/services/services/hitService.ts), [HitMessage.h](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-server/cpp/messages/HitMessage.h), [TES5DamageFormula.cpp](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-server/cpp/server_guest_lib/formulas/TES5DamageFormula.cpp), [TES5DamageFormulaTest.cpp](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/unit/TES5DamageFormulaTest.cpp), [HitTest.cpp](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/unit/HitTest.cpp). Deneyde kullanılan hasar formülünün temel TES5 mi, özel SweetPie formülü mü olduğu da kaydedilmeli.

Kilit çalışmasının başlangıç noktaları: [ObjectReferenceEx](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-client/src/extensions/objectReferenceEx.ts), sunucudaki `MpObjectReference`, `MpChangeForm`, mesaj tanımları, Papyrus `ObjectReference` uygulaması ve aktivasyon/kayıt testleri. Issue içindeki taslak öneriler uygulama garantisi değildir; aktörün nesneyle etkileşim hakkı, mesafe, envanter ve tekrar gönderilen sonuçların etkisi sunucuda kontrol edilmelidir.

Eski yol haritasından doğrudan “yok” sonucu çıkarmayalım: `DropItem` mesajı ve `unit/DropItemTest.cpp` mevcut; bunun dünyada görünme, ekstra eşya verisi ve fizik davranışının tamamını desteklediği ayrıca gösterilmeli. Hareket tarafında da zaten yerel gecikme telafisi var. Benzer şekilde açık #1989'un eski satırındaki keyword modifier toplama kodu güncel sürümde `max_element` kullanıyor; issue başlığını okuyup aynı düzeltmeyi yeniden yapmak doğru olmaz.

Mevcut PR'larla çakışmayı kontrol etmek gerekli. Özellikle [#2793 ESL/Form ID](https://github.com/skyrim-multiplayer/skymp/pull/2793), [#2726 furniture aktivasyonu](https://github.com/skyrim-multiplayer/skymp/pull/2726) ve [#2789 alan/interest management](https://github.com/skyrim-multiplayer/skymp/pull/2789) açık. İncelemede bu PR'lar birleştirilmedi veya doğrulanmadı.

**İki kişiyle doğrulama döngüsü**

1. Sabit commit'te varsayılan davranışı kaydet: oyun/SKSE/Platform sürümleri, ESM/ESP sırası, sunucu ayarı, kullanılan karakter ve nesne.
2. Tek bir başarısız senaryo seç. Oyuncu A işlemi yaparken B'nin gördüğünü ve sunucudaki durumu karşılaştır.
3. Aynı senaryoyu mümkünse C++ birim testi veya sunucu entegrasyon testiyle yeniden üret. Düzeltme öncesi beklenen sebeple başarısız, sonrası başarılı olmalı.
4. Uygulama, paket şeması, istemcide uygulama ve kayıt yollarından etkilenenleri birlikte değiştir. Paket değişmişse iki istemciyi ve sunucuyu aynı çıktıdan güncelle.
5. İki oyuncuyla tekrar dene: normal işlem, eşzamanlı etkileşim, bir oyuncunun sonradan katılması, hücre dışına çıkıp dönme, bağlantı kesilmesi ve sunucuyu yeniden başlatma.
6. İlgili mevcut testleri ve ardından gerekli bütün CI kontrollerini çalıştır. Kanıt olarak kısa yeniden üretim adımları, önce/sonra sonuçları ve logları PR'a ekle.

İlk deneylerin birisi uygulayıcı, diğeri gözlemci olacak şekilde yürütülmesi ve sonra rollerin değiştirilmesi, yalnızca yerel ekranda çalışan düzeltmeleri yakalamayı kolaylaştırır. Performans konularında mevcut `metricsSystem.ts` ve `netInfoService.ts` ölçümlerine önce bakılmalı.

**GitHub çalışma düzeni ve lisans**

6 Eylül devir hedefi [fallenhak/skymptrtest](https://github.com/fallenhak/skymptrtest); mevcut bilgi ve lab betiklerinin private deposu. Kaynak sabitlemeleri `sources.lock.json` içindedir ve araştırma checkout'ları betikle yeniden hazırlanır. Bu depo henüz upstream geçmişini içeren bir SkyMP kod fork'u değildir. Native geliştirmeye geçerken kendi kaynak fork'unuz `origin`, orijinal depo `upstream` olacak şekilde tam geçmişi korumak mantıklı. İkiniz aynı kaynak fork'unda collaborator olarak, her sorun için ayrı `codex/...` dalında çalışabilirsiniz. Çalışan taban commit'i ve test sürümü sabitlenmeli; upstream güncellemeleri kontrollü alınmalı. Bağımsız düzeltmeler PR üzerinden birleştirilirse başka sunuculara veya upstream'e taşımak kolaylaşır.

Kaynak kod, testler ve örnek ayarlar sürümlenir. Bethesda oyun arşivleri, kişisel kayıtlar, yerel dünya verisi, makineye özgü ayarlar ve erişim anahtarları geliştirme deposuna konulmaz. Fork'un CI'ı yerel/erişilebilir test verisiyle ve gereken işler seçilerek düzenlenmeli; upstream'in dağıtım workflow'ları özel repo ve secret varsayımları içeriyor.

İncelenen commit'te sunucu **AGPLv3**, istemci/Platform/frontend **GPLv3**, bazı yardımcı kütüphaneler **MIT** lisanslı. Fork geliştirmek mümkün; mevcut lisans ve telif bildirimleri korunmalı, dağıtımda ilgili kaynak kodu ve AGPL'nin ağ üzerinden kullanıma ilişkin kaynak erişimi koşulları gözetilmeli. Açık MIT dönüşüm PR'ı #2720 henüz birleşmiş sayılmaz. [Sunucu lisansı](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-server/LICENSE), [istemci lisansı](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-client/LICENSE), [TERMS](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/TERMS.md).

**Önerilen ilk kilometre taşı**

Resmi çıktıyla sunucu başlangıcı doğrulandı. Sıradaki altyapı hedefi iki oyuncunun aynı dünyaya bağlandığını, hareket/envanter durumunu birlikte gördüğünü ve yeniden girişte karakter durumunun korunduğunu göstermek. Kaynak geliştirmesi için VS 2022 araç zinciri ve test tabanı da hazırlanmalı. Ardından oyunun kendi menüsünden alınan tek bir demircilik perkini sunucuda doğrulayan, tarif erişimine etki eden ve yeniden girişte koruyan uçtan uca deney yapılacak. Yeni meslekler için sonraki küçük deney madencilik ağacı. Ayrıntılı kabul koşulları `PERK_SYSTEM_PLAN_TR.md` belgesinde.

Bir sonraki kurulum adımında açık kalanlar: mevcut `1.7.104` kurulumu için istemci paketinin doğrulanması; arkadaşın runtime/mod sürümleri; giriş/test UI dosyaları; native kaynak fork'unun bu devir deposuyla çalışma düzeni. Devir reposunun adı ve sahibi artık belli: `fallenhak/skymptrtest`. Perk menüsü için tercih oyunun kendi menüsü. Çekirdek incelemesi için özel RP sunucularına kaynak erişimi gerekmiyor. Güncel devam adımları [HANDOFF_TR.md](HANDOFF_TR.md) belgesindedir.

# SkyMP TR — ortak proje durumu

Son güncelleme: **7 Eylül 2026**. Geliştirme bu projede devam ediyor; bu belge birden fazla kişi/modelin aynı bağlamla çalışmasını sağlar. Tam kaynak fork'u ve ilk istemci düzeltmesi [PR #1](https://github.com/fallenhak/skymptrtest/pull/1) ile ana dala alındı; [CI geçti](https://github.com/fallenhak/skymptrtest/actions/runs/34056615032). [Ekip akışı](TEAM_WORKFLOW_TR.md).

## 1. Kullanıcının istediği çalışma

- İki arkadaş SkyMP'yi yerelde çalıştırıp açık kaynak üzerinde geliştirme yapmak istiyor. Ortak kaynak deposu **fallenhak/skymptrtest**; private görünürlük korunuyor. Belgeler geliştirmeye katılmayı kolaylaştırıyor; çalışma sürüyor.
- Kullanıcı Faalgrin'de oynuyor; fikir Keizaal deneyiminden de doğmuş. Bu sunucuların özel kaynaklarına erişim yok. Hedef onların paketlerini kopyalamak veya var olan sunucularını değiştirmek değil; açık SkyMP'de eksik/sorunlu davranışları deneyip geliştirmek.
- Kullanıcı 7 Eylül'de önceki sürüm bilgisini düzeltti: **Faalgrin 1.6.1170 kullanıyor**. Launcher güncel Steam kurulumundan ayrı oyun/mod kurulumu hazırlıyor; kullanıcı Nexus hesabı sorulmadığını bildirdi. Yerelde `Faalgrin/Modlist/Stock Game/SkyrimSE.exe` sürümü **1.6.1170.0** olarak doğrulandı. MO2 ve Wabbajack derleme ayarları mevcut; launcher'ın tüm indirme yöntemi henüz incelenmedi. Keizaal'ın runtime'ı ayrıca doğrulanmadı.
- İlk oynanabilir altyapı 1.6.1170 üzerinden denenecek; 1.7 kaynak yaması bekletildi. Kullanıcı teknik olmayan kurulum/oyun ekranı adımlarında yardımcı olmak istiyor; kısa yönlendirmeler isteyin, gereksiz araç çağrıları ve uzun otomasyonlarla kullanım limitini harcamayın.
- Sıra: **oynanabilir yerel altyapı → gerçekten çalışan perkler → RP'ye uygun denge ve meslekler**. Önceki bash/kilit adayları bekleme listesine alındı.
- Skyrim'in mevcut perkleri ve kendi yıldız ağacı menüsü mümkün olduğunca korunmalı. Özel ilerleme sistemi gerekse bile tercih ayrı bir browser perk ekranı yerine oyunun menüsünü düzenlemek.
- Simyacılık ve demircilik yeniden dengelenecek; madencilik, terzilik ve aşçılık gibi meslek ağaçları eklenecek. Sayısal denge, puan bütçesi ve uzmanlaşma kuralları henüz belirlenmedi.

Custom Skills Framework kullanımı, ilk demircilik deneyi ve üç perkli madencilik ağacı **teknik önerilerdir**. Kullanıcı ana hedefi belirledi; bu öneriler henüz uygulanmadı veya ayrıntılı ürün kararı olarak onaylanmadı.

## 2. Şu an gerçekten ne var?

| İş | Durum ve sınırı |
| --- | --- |
| Kaynak fork'u | SkyMP kodu kökte, 2.358 upstream commit'i geçmişte; araştırma kopyalarından bağımsız olarak değişiklikler bu repoda izleniyor |
| Resmi Windows sunucu çıktısı | CI commit'i kontrol edilerek indirildi; native dosyanın SHA-256 değeri kaydedildi |
| Yerel sunucu hazırlama | İki oyuncu kapasitesi, offline giriş, NPC kapalı, dosya tabanlı kayıt, HTTP loopback |
| Başlangıç testi | Native modül/gamemode hazır; beş ESM için HTTP manifesti doğru biçimde dönüyor |
| Tekrarlanabilirlik | Betikler ayrı ve temiz bir klasörde aynı bilgisayarda kaynakları indirdi, sunucuyu hazırladı; bu sunucunun testi geçti |
| TypeScript istemci | Kaynaktan webpack derlemesi ve 14 manifest regresyon testi geçti |
| Yerel manifest düzeltmesi | `server-http-url` doğrudan sunucu erişimi sağlar; hatalı/ulaşılamayan manifest artık boş mod listesi gibi kabul edilmez |
| İstemci paketi | Resmi native çıktılar ve yerel JS derlemesi player-1 klasöründe hazır; oyun dosyaları değiştirilmedi |
| Oyun ön koşulları | SKSE/Address Library indirildi; repodaki widget UI derlendi. İlk 1.7 lab'ında dosya kontrolü ve gerçek MO2 ayar/kayıt yönlendirme testi geçti |
| İlk native deneme | SKSE 2.3.1, 1.7.104 EXE'yi tanıdı; SkyrimPlatformImpl yüklenirken Address Library açma hatası görüldü. Nedeni ve bekletilen yama [native notunda](NATIVE_COMPATIBILITY_TR.md) |
| Yeni çalışma tabanı | Faalgrin Stock Game 1.6.1170 ve SKSE 2.2.8 doğrulandı; temel dosyalarla ayrı test kopyası hazır. Dosya ön koşulları ve gerçek MO2 profil testi geçti; oyun içi bağlantı bekliyor |
| İki oyunculu oyun | Bağlantı, hareket, envanter, hücre geçişi ve yeniden giriş testi yapılmadı |
| Native derleme | C++ derlemesi, birim testleri ve CTest henüz çalıştırılmadı |
| Perk sistemi | İnceleme/tasarım aşamasında; CSF kurulmadı, native menü veya sunucu perk protokolü uygulanmadı |

Sunucunun başlaması tam oynanabilir altyapı anlamına gelmez. Mevcut gamemode yalnızca hazır olma işareti üretir; RP ekonomisi, karakter ekranı veya meslek içeriği sağlamaz. Başlangıç smoke testi kendi sürecini kapatır; sürekli çalışan bir hizmet kurulmadı. Elle oyun denemesi için sunucu ayrıca başlatılır; süreç kimlikleri ve günlükler yalnızca `.local` altında tutulur.

1.6.1170 lab denemesinde MO2 hem Node hem SKSE için Error 5 gösterdi. Başlatma betiklerindeki `Start-Process -Wait` kaldırılıp yalnızca MO2 süreci beklendiğinde Node başlatma ve profil testi geçti. MO2 kaynak kodunun kullandığı `CREATE_BREAKAWAY_FROM_JOB` ile PowerShell bekleme grubu çakışıyordu; antivirüs ayarı değiştirilmedi. Kullanıcı MO2 kullanımına hakim. Elle SKSE açılışı başarılı oldu; 12:17:40’ta sunucu `1 logged as 1`, 12:18:53’te `disconnect 1` kaydetti. Skyrim Platform 2.9.0 SKSE günlüğünde doğru yüklendi. Bu, ilk native yükleme ve offline giriş kanıtıdır; kullanıcının oyun dünyasına erişimi henüz doğrulanmadı. [Ayrıntılar](GAME_LAB_TR.md), [taşınabilir kanıt](evidence/2026-09-07-game-lab.json).

Makineye özgü ham çalışma dosyaları `.local` altında kalır. Taşınabilir test sonucu [evidence/2026-09-06-handoff-verification.json](evidence/2026-09-06-handoff-verification.json) dosyasında; tekrar çalıştırma [LOCAL_SERVER_TR.md](LOCAL_SERVER_TR.md) belgesindedir.

## 3. Depo ve dosyalar nasıl devam ettirilir?

Bu GitHub deposu artık SkyMP'nin tam kaynak ağacını ve upstream geçmişini içerir. [sources.lock.json](../sources.lock.json) içeri aktarılan tabanı/native çıktıları sabitler. `restore-sources.ps1` SkyMP geçmişini doğrular ve gerekirse CSF araştırma kopyasını indirir. Kaynak geliştirme, kökteki `skymp5-client`, `skymp5-server` ve `skyrim-platform` dizinlerinde yapılır.

| Yol | Anlamı |
| --- | --- |
| `sources.lock.json` | İki kaynak commit'i, vcpkg sabitlemesi, resmi CI çıktısı, native hash ve yerel klasörler |
| `scripts/restore-sources.ps1` | Kökte upstream atasını doğrular; CSF araştırma kaynağını getirir; alt modülleri kurmaz |
| `scripts/build-client.ps1` | Yarn 1.22.22 ve lockfile ile TypeScript istemciyi derler; oyuna kopyalamaz |
| `scripts/prepare-local-client.ps1` | Native artifact + derlenen JS + oyuncuya özgü offline ayarları ayrı klasörde hazırlar |
| `scripts/check-client-prerequisites.ps1` | Oyun EXE sürümüne göre SKSE/Address Library ve UI dosyalarını kontrol eder |
| `scripts/prepare-local-server.ps1` | Resmi çıktıyı indirir, CI commit'ini ve native hash'i doğrular; kendi ESM yollarınızla ayar üretir |
| `scripts/start-local-server.ps1` | Hazırlanmış sunucuyu doğru çalışma dizininde başlatır |
| `scripts/test-local-server.mjs` | Native hazır olma ve manifest testi; sonunda kendi sürecini kapatır |
| `.research/upstream-skymp` | İlk araştırma kopyası; artık geliştirme yeri değil |
| `.research/custom-skills` | İncelenen CSF; Git dışında, sığ araştırma checkout'u |
| `.research/artifacts/756fb86/server-dist` | Orijinal CI indirme önbelleği; Git dışında |
| `.local/skymp-756fb86/server` | Makineye özgü sunucu ayarları, çalışma dosyaları ve dünya; Git dışında |

**Ortak geliştirme:** gerçek değişiklikler repo kökünde, `codex/...` dallarında veya ayrı worktree'lerde tutulur. Upstream uzak deposu `upstream`, ortak repo `origin` olur. PR'larda istemci derlemesi ve manifest testleri çalışır. Upstream'in özel secret/deploy varsayımları olan workflow'ları `.github/upstream-workflows` altında korunur. Araştırma önbelleği, indirilen native dosyalar ve dünya kayıtları commit edilmez.

## 4. Doğrulanan sürümler ve ortam

- SkyMP: `756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3`; incelenen ana dal commit'i 18 Ağustos 2026. Platform paket sürümü `2.9.0`.
- Sunucu: [33943379757 numaralı resmi Windows CI çalışması](https://github.com/skyrim-multiplayer/skymp/actions/runs/33943379757), `server-dist`; bu çalışma aynı commit için başarılı. Gözlenen artifact son kullanım tarihi 4 Aralık 2026.
- CSF: `8be7055f2483a261e0c377e2e8fc04b34116c68f`, proje sürümü `3.2.0`; incelenen commit release etiketiyle aynı commit olarak varsayılmamalı. CSF bir araştırma adayı; 1.6.1170 test lab’ına kurulmadı.
- İlk bilgisayar: Windows, PowerShell; Node `24.11.1`, npm `11.6.2`, Python `3.13`; Git ve oturumu açık GitHub CLI mevcut. Bu oturum başka bilgisayara taşınmaz.
- Oyun: `C:\Games\steamapps\common\Skyrim Special Edition`, EXE `1.7.104.0`; beş temel ESM mevcut. Creation Club dosyaları da var; gerçek istemci load order'ı henüz incelenmedi.
- Visual Studio Community 2026 / MSVC `14.51.36231` bulundu. Upstream CMake ise `Visual Studio 17 2022` generator'ünü açıkça şart koşuyor. Yarn PATH'te yok; CMake yalnızca VS 2026 altında bulundu. Native araç zinciri kurulmadı.
- İlk çalışma kökü `C:\Users\kerim\Documents\ChatGPT\SkyMPTR Test`. Başka bilgisayarda aynı yolu oluşturmak gerekmez; betikler kendi repo kökünü bulur.

Bu ortam değerleri gözlem tarihine aittir. Başka makinede veya sonraki sürümde yeniden kontrol edilmelidir. Oyun verilerinin sunucu tarafından okunması, o EXE'nin native istemciyle uyumunu doğrulamaz.

## 5. Yeniden keşfetmeye gerek olmayan teknik bulgular

**Mimari:** C++20 native sunucu Node/TypeScript tarafından çalıştırılıyor; istemci TypeScript ve Skyrim Platform/SKSE üzerinden oyunla konuşuyor. Bazı NPC davranışlarında istemci host rolü var. Her fizik/AI hesabının sunucuda yapıldığı varsayılamaz.

**Hazır çıktıdaki tuzak:** İndirilen `gamemode.js`, sonunda `process.exit` çağıran bir CI testi içeriyordu. Hazırlama betiği çalışma kopyasında bunu küçük bir hazır olma gamemode'uyla değiştiriyor. Orijinal indirme korunuyor. Paketin varsayılan betiği doğrudan kalıcı lab sunucusu olarak kullanılmamalı.

**Ağ ve giriş:** Oyun portu `7777/UDP`; varsayılan HTTP kaynak servisi `3000/TCP`. Oyun portu değişirse HTTP portu `port + 1`. Hazırlanan HTTP servisi `127.0.0.1` üzerinde; arkadaş erişimi için henüz hazır değil. Offline girişte istemciler farklı sayısal `gameData.profileId` kullanmalı; aynı kimlik aynı karakteri seçebilir.

**Offline sınırı:** `offlineMode` bütün dış istekleri kapatmaz. Yeni `server-http-url` ayarı mod manifestini doğrudan yerel HTTP sunucusundan alır; bu davranış 14 testle doğrulandı. `server-info-ignore` sunucu bilgi sorgusunu atlar. Bunlar hazırlanan istemci ayarlarına yazılır. Tam oyun içi akış hâlâ test edilmeli; load order uyuşmazlığı gizlenmez.

**UI ve gamemode:** Upstream `BUILD_FRONT=ON` yolu ayrı `skymp5-front` deposuna/PAT'e bağlı; anonim erişim 404 döndü. Biz `build-front.ps1` ile **bu repodaki** widget arayüzünü `Data/Platform/UI` için derledik. Derleme geçti; oyun içi davranış henüz doğrulanmadı. Giriş/widget arayüzü ile kullanıcının native perk menüsü tercihi farklı konular.

**Perkler:** `DisableSkillAdvanceService` normal skill ilerlemesini kapatıyor. SweetPie'ye özgü menü/perk servisleri koşullu. `addPerk`, `removePerk`, `hasPerk` ve perk puanı API'leri var; `perkEntryRun` satın alma onayı olayı değil. Sunucuda tam vanilla ilerleme sistemi hazır kabul edilemez. Hasar ve üretim koşulları sunucunun kendi kodunda ele alınmalı.

**Craft:** Tarif/istasyon/envanter altyapısı ve `onCraft` var. `EvaluateCraftRecipeConditions` mevcut; “hiç koşul kontrol edilmiyor” demek yanlış olur. Buna karşılık temel factory'de `HasPerk` kaydı yok ve `RecipeItemsMatch` tempering'i dışlıyor. Bir üretim tarifini açmak, demircilik perkinin tüm iyileştirme etkilerini kanıtlamaz.

**CSF:** Gerçek `RE::StatsMenu` menüsünü kullanıyor; `SKILLS.json` ve uygun NIF skydome ile mevcut ve yeni ağaçlar birlikte gösterilebiliyor. Menü seçim/puan hook'ları var; hazır bir sunucu onay protokolü saptanmadı. Gerekirse SKSE adaptörü veya sınırlı CSF değişikliği gerekecek. `3.2.0` notları 1.7.99/Address Library 12 güncellemesi bildiriyor; CSF ile SkyMP birlikteliği hiçbir lab sürümünde henüz test edilmedi.

Kaynak bağlantıları ve semboller [SOURCE_MAP_TR.md](SOURCE_MAP_TR.md) ve [PERK_SYSTEM_PLAN_TR.md](PERK_SYSTEM_PLAN_TR.md) içindedir. Genel issue/PR adayları [SKYMP_CONTEXT_TR.md](SKYMP_CONTEXT_TR.md) içinde saklanmıştır; açık issue listesini yeniden üretilmiş hata listesi gibi sunmayın.

## 6. Sıradaki iş ve tamamlanma ölçütü

**Devam eden görev oynanabilir altyapıdır.** Hedef artık **1.6.1170**. Kullanıcının verdiği `C:\Users\kerim\Games\Faalgrin\Modlist\Stock Game` konumu bu sürümü içeriyor; mevcut indirme önbelleğinde SKSE 2.2.8 arşivi bulundu. `sources.lock.json` ve [oyun lab betikleri](GAME_LAB_TR.md) bu eşleşmeye güncellendi. Sunucu aynı lab'ın beş ESM'sini kullanmalı; Steam 1.7 verileriyle karıştırılmamalı. Native yükleme ve ilk offline giriş kaydedildi. Şimdi CC temizliği sonrası dünyaya giriş ve istemci hata günlükleri doğrulanmalı. Kullanıcı oyun ekranını kontrol edebilir. VS 2022 kurulumu/1.7 native derlemesi şimdilik öncelik değil.

| Deney | Tamamlandı sayılma koşulu | Şu an |
| --- | --- | --- |
| Sunucu başlangıcı | Native hazır işareti + beş ESM manifesti | Geçti |
| İstemci kodu | TypeScript derlemesi + yerel/gateway manifest testleri | Geçti |
| İstemci dosyaları | Ayrı oyun, SKSE/Address Library/UI ve MO2 profili | 1.6.1170 lab hazır; dosya kontrolü ve MO2 ayar/kayıt yönlendirme testi geçti |
| İlk istemci | Bağlanma, karakter oluşturma/seçme, dünyaya girme | Native yükleme ve profil 1 ile giriş geçti; dünyaya giriş/görünüm bekliyor |
| İki oyuncu | Farklı profil kimlikleri; birbirini görme ve hareket | Bekliyor |
| Temel tutarlılık | Envanter/ekipman, sonradan katılma, hücreye dönme | Bekliyor |
| Kalıcılık | Çık-gir ve sunucu restart sonrası aynı karakter durumu | Bekliyor |
| Native geliştirme | Uygun araç zinciri, sabit kaynak derlemesi, ilgili testler | Bekliyor |
| Tek perk | Native menüde seçim → sunucuda puan/koşul kontrolü → gerçek tarif etkisi → yeniden girişte korunma | Önerilen ilk özellik |
| Madencilik | Küçük yeni ağaç; doğrulanmış cevher/XP; iki oyuncuda ortak damar durumu | Önerilen sonraki deney |

Perk modeli için öneri: karakter başına XP, seviye, puan, rank ve veri sürümü sunucuda tutulur. Menü yalnızca onaylanan durumu yansıtır. Aynı istek tekrarlandığında puan/XP ikinci kez işlenmez. Plugin/FormID eşlemesi kararlı kimliklere dayanır. Etkiler hesaplamayı yapan tarafta uygulanır; istemci ve sunucuda aynı çarpan iki kez eklenmez. Tam protokol, perk desteği ve ekonomi dengesi henüz uygulanmış özellik değildir.

## 7. Sonraki modele verilebilecek görev

> AGENTS.md ve docs/HANDOFF_TR.md dosyalarını oku. Mevcut SkyMP sunucu başlangıç testini koruyarak iki oyuncunun bağlanabildiği yerel altyapıyı tamamlamaya devam et. Önce yerel dosyaları ve sources.lock.json sabitlemelerini kullan. Eski README nedeniyle güncel oyun sürümünü uyumsuz varsayma; gerçek istemci paketini doğrula. Doğrulanan sonuçları ve bekleyen adımları belgeye işle. İlk özellik hedefi Skyrim'in kendi perk menüsüyle sunucu tarafından doğrulanan ve kalıcı bir perk sistemi; madencilik, terzilik ve aşçılık sonraki meslek hedefleri. Araştırma önerilerini uygulanmış özellik gibi sunma.

## 8. Ortak geliştirme ve korunan bilgiler

Repo sahibi arkadaşının GitHub erişimini kendisinin düzenleyeceğini belirtti; davet işlemi bu çalışmanın dışında. Kaynak kod, betikler ve belgeler ortak repoda izlenir. Bethesda ESM/BSA dosyaları, kişisel kayıtlar, dünya verisi, token'lar ve indirilmiş binary'ler Git'e eklenmez.

SkyMP sunucu AGPLv3, istemci/Platform GPLv3 ve yardımcı parçalar kendi lisanslarıyla gelir; CSF kaynağı MIT. Kaynak geliştirirken mevcut lisans/telif dosyalarını koruyun. Ayrıntılı lisans bağlantıları teknik bağlam belgesindedir. Kendi test kopyamıza oyun bileşenleri kuruldu; Faalgrin/Steam kaynak klasörlerine yazılmadı, mevcut RP sunucuları değiştirilmedi.

## 9. İlk oyun açılışı ve CC temizliği — 7 Eylül

- Sunucu `127.0.0.1:3000/manifest.json` üzerinden aynı beş master’ı veriyor; oyun portu 7777. İstemci ayarları profil 1 ile otomatik offline giriş yapıyor, IP yazma veya sunucu seçme ekranı gerekmiyor.
- Kullanıcı AE indirme ekranıyla karşılaştı. MO2 `overwrite` içine dört CC paketi indi: `ccbgssse068-bloodfall`, `ccbgssse069-contest`, `ccvsvsse003-necroarts`, `ccvsvsse004-beafarmer` (her biri BSA + ESL). Skyrim/MO2 kullanıcı tarafından kapatıldıktan sonra sekiz dosya yalnızca test lab’ının `backups/cc-cleanup-20260907-122232` klasörüne taşındı. Steam/Faalgrin kopyaları değiştirilmedi; `overwrite/SKSE/Plugins/SkyrimPlatform.ini` korundu.
- Profil `SkyrimPrefs.ini` dosyasının `[General]` bölümüne `bFreebiesSeen=1` eklendi; yükleme sırası beş master’a döndürüldü. AE indirme uyarısının kalkması bir sonraki oyun açılışında kullanıcı tarafından doğrulanmalı. İlgili ayar yeni lab hazırlama betiğine de eklendi.
- İlk Platform günlüğü `DirectoryMonitor(Data/Platform/PluginsDev) failed with code 2` gösterdi. Eksik boş klasör oluşturuldu ve hazırlama betiğine eklendi; yeni oyun günlüğünde hata yokluğu henüz doğrulanmadı.
- Aynı ilk günlükte `Cannot read properties of null (reading 'getFormID')` ve `Equipment.inv.entries[].count ... NUMBER_OUT_OF_RANGE` hataları vardı. Bunların CC indirmesi/ilk yüklemeyle ilişkisi kanıtlanmadı; envanter protokolünü tahminle değiştirmeyin. Temiz açılışta yeniden oluşursa istemci ekipman paketindeki gerçek count değerini ve native dönüştürme yolunu inceleyin.
- Platform, NirnLabUIPlatform dinleyicisini bulamadı ve legacy Tilted UI backend’ine geçti. Mevcut frontend bu backend’de oyun içinde ayrıca doğrulanmalı.
- Temizlik sonrası gerçek MO2 profil testi tekrar geçti (09:22:34 UTC). İlk açılışın SKSE/Platform günlükleri aynı yerel yedekte, temizlik kaydı lab kökünde `cc-cleanup.json`. Sunucu oyun denemesi için çalışmaya devam ediyor; PID’yi varsaymak yerine port/süreç kontrolü yapın.
 
## 10. SkyMP bootstrap kayıt yolu ve otomatik dünyaya giriş — 7 Eylül
 
- **Bulgu:** İstemci sunucuya bağlandığında dünyayı otomatik yükleyemeyip ana menüde kalıyordu. Neden: Upstream resmi derlemesindeki `SkyrimPlatformImpl.dll` ([LoadGame.cpp](file:///c:/Users/kerim/Documents/ChatGPT/SkyMPTR%20Test/skyrim-platform/src/platform_se/skyrim_platform/LoadGame.cpp#L125-L130)), geçici ışınlanma kaydını (`TESMODPLATFORM-<GUID>.ess`) sabit olarak `Documents\My Games\Skyrim Special Edition\Saves\` altına yazmaktadır.
- MO2 profilinde `LocalSaves=true` ve `sLocalSavePath=__MO_Saves\` ayarlandığı için Skyrim motoru kaydı `__MO_Saves\` içinde arıyor ve bulamıyordu. Skyrim menüye ulaştıktan sonra klasöre dosya kopyalamak da Scaleform menüyü yenilemediğinden menüde "Yükle" seçeneği çıkmıyordu.
- **Düzeltme:** Profil `settings.ini` içinde `LocalSaves=false`, `skyrim.ini` ve `skyrimcustom.ini` içinde `sLocalSavePath=Saves\` olarak hizalandı. Profilin özel INI (`LocalSettings=true`) ve 5 master plugin izolasyonu korundu. `test-game-profile.ps1` ve `probe-game-profile.mjs` bu mimariye göre güncellendi ve profil testi başarıyla geçti.
- SkyMP dünyasına giriş artık otomatik gerçekleşebilir; geçici `.ess` dosyası oyuna girildikten 5 saniye sonra native eklenti tarafından kendiliğinden temizlenir.


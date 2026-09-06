# SkyMP TR — kişi/model devir belgesi

Son güncelleme: **6 Eylül 2026**. Araştırma ve ilk sunucu denemesi 5 Eylül'de yapıldı; devir betikleri ve temiz klasördeki sunucu 6 Eylül'de doğrulandı. Bu belge önceki konuşma olmadan devam etmek içindir.

## 1. Kullanıcının istediği çalışma

- İki arkadaş SkyMP'yi yerelde çalıştırıp açık kaynak üzerinde geliştirme yapmak istiyor. Çalışma deposu **fallenhak/skymptrtest**; devir sırasında boş ve private olarak bulundu. Mevcut görünürlük korunuyor.
- Kullanıcı Faalgrin'de oynuyor; fikir Keizaal deneyiminden de doğmuş. Bu sunucuların özel kaynaklarına erişim yok. Hedef onların paketlerini kopyalamak veya var olan sunucularını değiştirmek değil; açık SkyMP'de eksik/sorunlu davranışları deneyip geliştirmek.
- Kullanıcı bu sunucuların güncel oyun sürümünde çalıştığını bildirdi. Eski README'nin sürüm listesi uyumsuzluk kanıtı değildir; downgrade kararı alınmadı.
- Sıra: **oynanabilir yerel altyapı → gerçekten çalışan perkler → RP'ye uygun denge ve meslekler**. Önceki bash/kilit adayları bekleme listesine alındı.
- Skyrim'in mevcut perkleri ve kendi yıldız ağacı menüsü mümkün olduğunca korunmalı. Özel ilerleme sistemi gerekse bile tercih ayrı bir browser perk ekranı yerine oyunun menüsünü düzenlemek.
- Simyacılık ve demircilik yeniden dengelenecek; madencilik, terzilik ve aşçılık gibi meslek ağaçları eklenecek. Sayısal denge, puan bütçesi ve uzmanlaşma kuralları henüz belirlenmedi.

Custom Skills Framework kullanımı, ilk demircilik deneyi ve üç perkli madencilik ağacı **teknik önerilerdir**. Kullanıcı ana hedefi belirledi; bu öneriler henüz uygulanmadı veya ayrıntılı ürün kararı olarak onaylanmadı.

## 2. Şu an gerçekten ne var?

| İş | Durum ve sınırı |
| --- | --- |
| SkyMP ve CSF kaynak incelemesi | Tam commit'lere sabitlendi; iki araştırma kopyası değiştirilmedi |
| Resmi Windows sunucu çıktısı | CI commit'i kontrol edilerek indirildi; native dosyanın SHA-256 değeri kaydedildi |
| Yerel sunucu hazırlama | İki oyuncu kapasitesi, offline giriş, NPC kapalı, dosya tabanlı kayıt, HTTP loopback |
| Başlangıç testi | Native modül/gamemode hazır; beş ESM için HTTP manifesti doğru biçimde dönüyor |
| Tekrarlanabilirlik | Betikler ayrı ve temiz bir klasörde aynı bilgisayarda kaynakları indirdi, sunucuyu hazırladı; bu sunucunun testi geçti |
| İstemci ve oyun kurulumu | SKSE/Platform/SkyMP istemcisi bu çalışma kapsamında kurulmadı; oyun dosyaları değiştirilmedi |
| İki oyunculu oyun | Bağlantı, hareket, envanter, hücre geçişi ve yeniden giriş testi yapılmadı |
| Kaynak derleme | Yapılmadı; C++ birim testleri ve CTest çalıştırılmadı |
| Perk sistemi | İnceleme/tasarım aşamasında; CSF kurulmadı, native menü veya sunucu perk protokolü uygulanmadı |

Sunucunun başlaması tam oynanabilir altyapı anlamına gelmez. Mevcut gamemode yalnızca hazır olma işareti üretir; RP ekonomisi, karakter ekranı veya meslek içeriği sağlamaz. Test sonunda başlatılan süreç kapatıldı; sürekli çalışan bir hizmet kurulmadı.

Makineye özgü ham çalışma dosyaları `.local` altında kalır. Taşınabilir test sonucu [evidence/2026-09-06-handoff-verification.json](evidence/2026-09-06-handoff-verification.json) dosyasında; tekrar çalıştırma [LOCAL_SERVER_TR.md](LOCAL_SERVER_TR.md) belgesindedir.

## 3. Depo ve dosyalar nasıl devam ettirilir?

Bu GitHub deposu mevcut devir belgelerini ve bize ait kurulum/test betiklerini içerir. Açık kaynakların tamamı buraya yeniden kopyalanmadı; [sources.lock.json](../sources.lock.json) ve `restore-sources.ps1` aynı araştırma kaynaklarını yeniden getirir. Henüz upstream geçmişini taşıyan bir native geliştirme fork'u yoktur.

| Yol | Anlamı |
| --- | --- |
| `sources.lock.json` | İki kaynak commit'i, vcpkg sabitlemesi, resmi CI çıktısı, native hash ve yerel klasörler |
| `scripts/restore-sources.ps1` | Sabit commit'leri indirir; var olan farklı checkout'u sıfırlamaz; alt modülleri kurmaz |
| `scripts/prepare-local-server.ps1` | Resmi çıktıyı indirir, CI commit'ini ve native hash'i doğrular; kendi ESM yollarınızla ayar üretir |
| `scripts/start-local-server.ps1` | Hazırlanmış sunucuyu doğru çalışma dizininde başlatır |
| `scripts/test-local-server.mjs` | Native hazır olma ve manifest testi; sonunda kendi sürecini kapatır |
| `.research/upstream-skymp` | İncelenen SkyMP; Git dışında, sığ araştırma checkout'u |
| `.research/custom-skills` | İncelenen CSF; Git dışında, sığ araştırma checkout'u |
| `.research/artifacts/756fb86/server-dist` | Orijinal CI indirme önbelleği; Git dışında |
| `.local/skymp-756fb86/server` | Makineye özgü sunucu ayarları, çalışma dosyaları ve dünya; Git dışında |

**Native geliştirmeye geçerken:** gerçek değişiklikleri yalnızca `.research` altında bırakmayın; bu deponun normal commit'i onları taşımaz. Önce upstream geçmişini ve lisanslarını koruyan, GitHub'a gönderilecek kaynak çalışma dalını/checkout'unu kurun. Bu devir deposu ile native kaynak fork'unun düzenini açıkça belgeleyin. Araştırma kopyalarını zorla sıfırlamayın veya `git add -f .research` ile iç içe repoları ve artifact'leri topluca eklemeyin. Şu anda taşınması gereken yerel C++/TS düzeltmesi yok.

## 4. Doğrulanan sürümler ve ortam

- SkyMP: `756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3`; incelenen ana dal commit'i 18 Ağustos 2026. Platform paket sürümü `2.9.0`.
- Sunucu: [33943379757 numaralı resmi Windows CI çalışması](https://github.com/skyrim-multiplayer/skymp/actions/runs/33943379757), `server-dist`; bu çalışma aynı commit için başarılı. Gözlenen artifact son kullanım tarihi 4 Aralık 2026.
- CSF: `8be7055f2483a261e0c377e2e8fc04b34116c68f`, proje sürümü `3.2.0`; incelenen commit release etiketiyle aynı commit olarak varsayılmamalı. CSF bir araştırma adayı.
- İlk bilgisayar: Windows, PowerShell; Node `24.11.1`, npm `11.6.2`, Python `3.13`; Git ve oturumu açık GitHub CLI mevcut. Bu oturum başka bilgisayara taşınmaz.
- Oyun: `C:\Games\steamapps\common\Skyrim Special Edition`, EXE `1.7.104.0`; beş temel ESM mevcut. Creation Club dosyaları da var; gerçek istemci load order'ı henüz incelenmedi.
- Visual Studio Community 2026 / MSVC `14.51.36231` bulundu. Upstream CMake ise `Visual Studio 17 2022` generator'ünü açıkça şart koşuyor. Yarn PATH'te yok; CMake yalnızca VS 2026 altında bulundu. Native araç zinciri kurulmadı.
- İlk çalışma kökü `C:\Users\kerim\Documents\ChatGPT\SkyMPTR Test`. Başka bilgisayarda aynı yolu oluşturmak gerekmez; betikler kendi repo kökünü bulur.

Bu ortam değerleri gözlem tarihine aittir. Başka makinede veya sonraki sürümde yeniden kontrol edilmelidir. Oyun verilerinin sunucu tarafından okunması, o EXE'nin native istemciyle uyumunu doğrulamaz.

## 5. Yeniden keşfetmeye gerek olmayan teknik bulgular

**Mimari:** C++20 native sunucu Node/TypeScript tarafından çalıştırılıyor; istemci TypeScript ve Skyrim Platform/SKSE üzerinden oyunla konuşuyor. Bazı NPC davranışlarında istemci host rolü var. Her fizik/AI hesabının sunucuda yapıldığı varsayılamaz.

**Hazır çıktıdaki tuzak:** İndirilen `gamemode.js`, sonunda `process.exit` çağıran bir CI testi içeriyordu. Hazırlama betiği çalışma kopyasında bunu küçük bir hazır olma gamemode'uyla değiştiriyor. Orijinal indirme korunuyor. Paketin varsayılan betiği doğrudan kalıcı lab sunucusu olarak kullanılmamalı.

**Ağ ve giriş:** Oyun portu `7777/UDP`; varsayılan HTTP kaynak servisi `3000/TCP`. Oyun portu değişirse HTTP portu `port + 1`. Hazırlanan HTTP servisi `127.0.0.1` üzerinde; arkadaş erişimi için henüz hazır değil. Offline girişte istemciler farklı sayısal `gameData.profileId` kullanmalı; aynı kimlik aynı karakteri seçebilir.

**Offline sınırı:** `offlineMode` bütün dış istekleri kapatmaz. Boş `master` varsayılan gateway'e düşebilir. `server-info-ignore` sunucu bilgi sorgusunu atlasa da mod manifesti yolu ayrı. Tam yerel istemci akışı hâlâ doğrulanmalı. Load order uyuşmazlığını gizlemek yerine dosya/sıra eşleştirmesini çözün.

**UI ve gamemode:** `BUILD_FRONT=ON` ayrı `skymp5-front` deposuna/PAT'e bağlı; anonim erişim 404 döndü. Bunun özel mi, kaldırılmış mı olduğu kesin değil. Güncel browser yolu `Data/Platform/UI/index.html`. Küçük yerel gamemode yazılabilir; özel RP sunucularının kaynak erişimi ön koşul değil. Giriş UI ihtiyacı ile kullanıcının native perk menüsü tercihi farklı konular.

**Perkler:** `DisableSkillAdvanceService` normal skill ilerlemesini kapatıyor. SweetPie'ye özgü menü/perk servisleri koşullu. `addPerk`, `removePerk`, `hasPerk` ve perk puanı API'leri var; `perkEntryRun` satın alma onayı olayı değil. Sunucuda tam vanilla ilerleme sistemi hazır kabul edilemez. Hasar ve üretim koşulları sunucunun kendi kodunda ele alınmalı.

**Craft:** Tarif/istasyon/envanter altyapısı ve `onCraft` var. `EvaluateCraftRecipeConditions` mevcut; “hiç koşul kontrol edilmiyor” demek yanlış olur. Buna karşılık temel factory'de `HasPerk` kaydı yok ve `RecipeItemsMatch` tempering'i dışlıyor. Bir üretim tarifini açmak, demircilik perkinin tüm iyileştirme etkilerini kanıtlamaz.

**CSF:** Gerçek `RE::StatsMenu` menüsünü kullanıyor; `SKILLS.json` ve uygun NIF skydome ile mevcut ve yeni ağaçlar birlikte gösterilebiliyor. Menü seçim/puan hook'ları var; hazır bir sunucu onay protokolü saptanmadı. Gerekirse SKSE adaptörü veya sınırlı CSF değişikliği gerekecek. `3.2.0` notları 1.7.99/Address Library 12 güncellemesi bildiriyor; bizim 1.7.104 + SkyMP kombinasyonumuz test edilmedi.

Kaynak bağlantıları ve semboller [SOURCE_MAP_TR.md](SOURCE_MAP_TR.md) ve [PERK_SYSTEM_PLAN_TR.md](PERK_SYSTEM_PLAN_TR.md) içindedir. Genel issue/PR adayları [SKYMP_CONTEXT_TR.md](SKYMP_CONTEXT_TR.md) içinde saklanmıştır; açık issue listesini yeniden üretilmiş hata listesi gibi sunmayın.

## 6. Sıradaki iş ve tamamlanma ölçütü

**Bir sonraki görev oynanabilir altyapıdır.** Önce mevcut sunucuyu doğrulayın; aynı SkyMP commit'ine ait istemci paketini inceleyin. Oyuncunun gerçek oyun kopyasını/mod yöneticisi profilini ve SKSE/Platform eşleşmesini belirleyin. Gerekli istemci ayarları, mod manifesti ve giriş UI akışını yerel test profili için hazırlayın. Arkadaşın runtime/mod bilgisi henüz paylaşılmadı; ikinci oyuncu testi bu bilgi ve erişim gerektirir.

| Deney | Tamamlandı sayılma koşulu | Şu an |
| --- | --- | --- |
| Sunucu başlangıcı | Native hazır işareti + beş ESM manifesti | Geçti |
| İlk istemci | Bağlanma, karakter oluşturma/seçme, dünyaya girme | Bekliyor |
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

Arkadaşın GitHub kullanıcı adı henüz verilmedi; collaborator daveti gönderilmedi. Private repoyu klonlayabilmesi için sahibi erişim vermeli. Geliştirme betikleri ve belgeler taşındı; Bethesda ESM/BSA dosyaları, kişisel kayıtlar, dünya verisi, token'lar ve indirilmiş binary'ler Git'e eklenmedi. Sabitlemeler ve kurulum betikleri bunların yerine yeniden hazırlama yolunu sağlar.

SkyMP sunucu AGPLv3, istemci/Platform GPLv3 ve yardımcı parçalar kendi lisanslarıyla gelir; CSF kaynağı MIT. Kaynak geliştirirken mevcut lisans/telif dosyalarını koruyun. Ayrıntılı lisans bağlantıları teknik bağlam belgesindedir. Bu devir işlemi herhangi bir upstream PR'ı birleştirmedi, oyuna mod kurmadı veya mevcut RP sunucusunu değiştirmedi.

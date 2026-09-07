# Ortak geliştirme düzeni

6 Eylül 2026: `fallenhak/skymptrtest` artık SkyMP kaynak kodunu ve upstream geçmişini içerir. Geliştirme bu projede sürüyor; belgeler ekibe katılan kişilerin aynı bağlamdan çalışmasını sağlar. Arkadaşın GitHub erişimini repo sahibi düzenleyecek.

## İlk kurulum

```powershell
gh repo clone fallenhak/skymptrtest
Set-Location skymptrtest
git remote add upstream https://github.com/skyrim-multiplayer/skymp.git
.\scripts\build-client.ps1
node --test skymp5-client/tests/settingsService.test.cjs
```

Kaynak kod köktedir; `.research/upstream-skymp` altında geliştirme yapmayın. CSF araştırmasına ihtiyaç varsa `restore-sources.ps1` kullanılabilir. Native C++ derlemesine geçerken `git submodule update --init --depth 1` ile sabit vcpkg alt modülünü hazırlayın ve upstream `CONTRIBUTING.md` gereksinimlerini uygulayın. Native derleme bu bilgisayarda henüz tamamlanmadı.

## Günlük çalışma

`main` ortak taban; her özellik için ayrı bir `codex/...` dalı kullanın. Örneğin:

```powershell
git switch main
git pull --ff-only
git switch -c codex/perk-recipe-prototype
```

Değişiklikleri ilgili kaynak dosyaları ve testlerle commit edin; dalı `origin`e gönderip PR açın. PR açıklamasında oyuncunun hangi işlemi yaptığı, önce/sonra davranışı ve hangi testlerin gerçekten çalıştırıldığı yer alsın. Büyük perk işleri için önce dar bir senaryonun çalışmasını gösterin.

Aynı bilgisayarda iki kişi/model eşzamanlı çalışacaksa ayrı worktree kullanın:

```powershell
git worktree add ..\skymptrtest-perks -b codex/perks main
```

Worktree'lerin `.local` ve `node_modules` klasörleri ayrıdır. Aynı makinede iki sunucu başlatılacaksa portlar da farklı olmalı. Dünya verisini worktree veya dal geçişi sırasında silmeyin. Başka birinin devam eden dalını sıfırlamayın.

## Kontroller

`.github/workflows/lab-checks.yml`, istemciyi ve repodaki oyun arayüzünü sabit Yarn lockfile ile derler, manifest testlerini çalıştırır ve lab betiklerini sözdizimi açısından kontrol eder. Bu kontrol Skyrim kurulumu veya özel sunucu secret'ı istemez. C++ derlemesini, oyun içi bağlantıyı veya perk etkilerini doğrulamaz.

Upstream'in periyodik build/deploy workflow'ları `.github/upstream-workflows` altında korunur ve otomatik çalışmaz. Native build işlerini kendi gereksinimlerimize göre ayrıca etkinleştireceğiz. `sources.lock.json` içindeki native çıktı ile kaynak değişikliklerini eşleştirin; istemci JS'si değiştiğinde iki oyuncu da aynı derlemeyi kullanmalı.

## Yerel test paketleri

```powershell
.\scripts\prepare-local-server.ps1 -SkyrimDirectory 'C:\Games\steamapps\common\Skyrim Special Edition'
node .\scripts\test-local-server.mjs
.\scripts\prepare-local-client.ps1 -ProfileId 1
.\scripts\check-client-prerequisites.ps1 -SkyrimDirectory 'C:\Games\steamapps\common\Skyrim Special Edition'
```

Mevcut sunucu/profil varsa hazırlama betikleri üzerine yazmaz. `start-local-server.ps1` mevcut sunucuyu açar. İkinci oyuncu için `-ProfileId 2 -ServerAddress '<sunucunun LAN/VPN adresi>'` kullanılır. Hazırlanan istemci bir mod dosyası paketidir; henüz kurulmuş/çalışan Skyrim kopyası değildir. İki oyuncu aynı ESM sürümü ve load order kullanmalı.

İstemci ayarındaki `server-http-url` manifestin doğrudan sunucudan alınmasını sağlar. Sunucunun HTTP servisi başlangıçta `127.0.0.1` dinler; arkadaş bağlantısı öncesinde `uiListenHost` ve ağ erişimi test ağı için düzenlenmeli. Offline giriş profil kimliğine dayandığı için herkese açık yayın amacıyla kullanılmamalı.

Ayrı 1.6.1170 oyunu, MO2 profili ve gerçek profil yönlendirme testi için [GAME_LAB_TR.md](GAME_LAB_TR.md) adımlarını kullanın. Ortak karar ve ilerleme için [HANDOFF_TR.md](HANDOFF_TR.md), perk tasarımı için [PERK_SYSTEM_PLAN_TR.md](PERK_SYSTEM_PLAN_TR.md) güncel tutulur. Tamamlanmamış oyun içi testler CI geçmesiyle tamamlanmış sayılmaz.

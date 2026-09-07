# SkyMP TR Test

SkyMP üzerinde ortak geliştirme, iki kişilik yerel testler ve RP perk/meslek sistemi denemeleri için kaynak deposu. SkyMP'nin C++/TypeScript kodu ve upstream Git geçmişi bu repoda bulunur.

**Durum — 7 Eylül 2026:** Tam kaynak kodu ve ekip akışı ana dalda; istemci derlemesi ve 14 manifest testi GitHub CI'da geçti. Repodaki oyun arayüzü de yerelde derlendi. Ayrı MO2 profilinde ayar/kayıt yönlendirmesi doğrulandı. 1.7.104 oyun denemesi CommonLib sürüm tanıma hatasına ulaştı. Faalgrin'in gerçek runtime'ı 1.6.1170 olarak doğrulanınca ilk oynanabilir altyapı bu sürüme yönlendirildi. İki oyuncu ve perk etkileri henüz doğrulanmadı.

## Buradan devam edin

1. [Ortak çalışma düzeni](docs/TEAM_WORKFLOW_TR.md): klonlama, dal/worktree, PR ve test akışı.
2. [Güncel proje durumu](docs/HANDOFF_TR.md): hedefler, tamamlanan işler, açık noktalar ve sıradaki görev.
3. [Ayrı oyun kurulumu](docs/GAME_LAB_TR.md) ve [yerel sunucu](docs/LOCAL_SERVER_TR.md): hazırlama, başlatma ve doğrulama komutları.
4. [Perk ve meslek planı](docs/PERK_SYSTEM_PLAN_TR.md): native menü, kalıcılık, RP dengesi ve yeni ağaçlar.
5. [Teknik bağlam](docs/SKYMP_CONTEXT_TR.md) ve [kaynak kod haritası](docs/SOURCE_MAP_TR.md): mimari, sürümler ve ilgili kod.

Projeye katılan kişi/model önce [AGENTS.md](AGENTS.md) ve güncel durum belgesini okuyabilir. Geliştirme aynı repoda devam eder; önceki konuşmaya erişim gerekmez.

## Depoyu ve kaynakları hazırlama

GitHub hesabınızın bu private repoya erişimi olmalı. Windows PowerShell'de:

```powershell
gh repo clone fallenhak/skymptrtest
Set-Location skymptrtest
.\scripts\build-client.ps1
.\scripts\build-front.ps1
node --test skymp5-client/tests/settingsService.test.cjs
```

SkyMP kaynakları doğrudan repo kökündedir; 2.358 upstream commit'i korunmuştur. [sources.lock.json](sources.lock.json) içindeki SkyMP commit'i içeri aktarılan tabandır; geliştirme commit'leri bunun üzerine gelir. CSF araştırma kopyası gerektiğinde `restore-sources.ps1` ile hazırlanır. C++ derleme gereksinimleri upstream [CONTRIBUTING.md](CONTRIBUTING.md) içindedir; TypeScript derlemesi bunları gerektirmez.

## Sunucuyu deneme

GitHub CLI oturumu, Git, Node ve kendi Skyrim SE kurulumunuz gerekir. Doğrulanan Node sürümü `v24.11.1`.

```powershell
.\scripts\prepare-local-server.ps1 -SkyrimDirectory 'C:\Games\steamapps\common\Skyrim Special Edition'
node .\scripts\test-local-server.mjs
.\scripts\start-local-server.ps1
```

Oyun yolunu kendi kurulumunuza göre değiştirin. Hazırlama betiği mevcut sunucu/dünya klasörünün üzerine yazmaz. Test kendi başlattığı sunucuyu kapatır; sürekli çalıştırmak için son komutu kullanın. İstemci kurulumu bu adımların parçası değil.

Derlenen istemciyle ayrı bir test paketi hazırlamak ve eksikleri görmek için:

```powershell
.\scripts\prepare-local-client.ps1 -ProfileId 1
.\scripts\check-client-prerequisites.ps1 -SkyrimDirectory 'C:\Games\steamapps\common\Skyrim Special Edition'
```

Paket `.local/skymp-756fb86/clients/player-1` altına yazılır; oyun klasörünü değiştirmez. Tam oyun testi için [1.6.1170 lab adımlarını](docs/GAME_LAB_TR.md) uygulayın; sunucu ve istemci aynı ESM kopyalarını kullanmalı. `server-http-url`, yerel manifest adresini belirtir. İkinci oyuncu ve ağ ayarları ortak çalışma belgesindedir.

Betikler temiz bir çalışma klasöründe, aynı Windows bilgisayarında doğrulandı. Sonuç ve test sınırları [test kaydında](docs/evidence/2026-09-06-handoff-verification.json). Başka bilgisayarda deneme henüz yapılmadı.

İndirilen CI çıktısının gözlenen son kullanım tarihi **4 Aralık 2026**. Süre dolarsa aynı kaynak commit'inden derleme gerekir; farklı bir çıktıyı sessizce eşdeğer kabul etmeyin. Oyun verileri, dünya kayıtları, erişim anahtarları ve indirme önbelleği Git dışında kalır. Upstream lisans bilgileri [teknik bağlamda](docs/SKYMP_CONTEXT_TR.md) kayıtlıdır.

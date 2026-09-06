# SkyMP TR Test

SkyMP üzerinde iki kişilik yerel geliştirme ve RP perk/meslek sistemi denemeleri için araştırma, kurulum betikleri ve devir deposu.

**Durum — 6 Eylül 2026:** Resmi sunucu çıktısı yerelde açılıyor; başlangıç ve oyun verisi manifest testi geçti. İki oyun istemcisinin bağlandığı, karakterlerin kalıcı olduğu veya perklerin çalıştığı henüz doğrulanmadı. İlk öncelik oynanabilir altyapı; ardından Skyrim'in kendi perk menüsünü koruyarak sunucu kontrollü ilerleme.

## Buradan devam edin

1. [Devir belgesi](docs/HANDOFF_TR.md): kullanıcı hedefleri, tamamlanan işler, açık noktalar ve bir sonraki görev.
2. [Yerel kurulum](docs/LOCAL_SERVER_TR.md): hazırlama, başlatma ve doğrulama komutları.
3. [Perk ve meslek planı](docs/PERK_SYSTEM_PLAN_TR.md): native menü, kalıcılık, RP dengesi ve yeni ağaçlar.
4. [Teknik bağlam](docs/SKYMP_CONTEXT_TR.md) ve [kaynak kod haritası](docs/SOURCE_MAP_TR.md): mimari, sürümler, ilgili kod ve sonraki senkronizasyon adayları.

Bir modelle devam ediyorsanız önce [AGENTS.md](AGENTS.md) ve devir belgesini okutun. Önceki konuşmaya erişim gerekmez.

## Depoyu ve kaynakları hazırlama

GitHub hesabınızın bu private repoya erişimi olmalı. Windows PowerShell'de:

```powershell
gh repo clone fallenhak/skymptrtest
Set-Location skymptrtest
.\scripts\restore-sources.ps1
```

Bu depo şu ana kadarki bize ait betikleri ve bilgi birikimini sürümler. SkyMP ve Custom Skills Framework kaynakları [sources.lock.json](sources.lock.json) içindeki **tam commit kimliklerinden** `.research` altına indirilir. Kaynaklar değiştirilmedi; bu depo henüz upstream geçmişini içeren bir SkyMP kod fork'u değil. Kaynak indirme betiği derleme veya mod kurulumu yapmaz.

## Sunucuyu deneme

GitHub CLI oturumu, Git, Node ve kendi Skyrim SE kurulumunuz gerekir. Doğrulanan Node sürümü `v24.11.1`.

```powershell
.\scripts\prepare-local-server.ps1 -SkyrimDirectory 'C:\Games\steamapps\common\Skyrim Special Edition'
node .\scripts\test-local-server.mjs
.\scripts\start-local-server.ps1
```

Oyun yolunu kendi kurulumunuza göre değiştirin. Hazırlama betiği mevcut sunucu/dünya klasörünün üzerine yazmaz. Test kendi başlattığı sunucuyu kapatır; sürekli çalıştırmak için son komutu kullanın. İstemci kurulumu bu adımların parçası değil.

Betikler temiz bir çalışma klasöründe, aynı Windows bilgisayarında doğrulandı. Sonuç ve test sınırları [test kaydında](docs/evidence/2026-09-06-handoff-verification.json). Başka bilgisayarda deneme henüz yapılmadı.

İndirilen CI çıktısının gözlenen son kullanım tarihi **4 Aralık 2026**. Süre dolarsa aynı kaynak commit'inden derleme gerekir; farklı bir çıktıyı sessizce eşdeğer kabul etmeyin. Oyun verileri, dünya kayıtları, erişim anahtarları ve indirme önbelleği Git dışında kalır. Upstream lisans bilgileri [teknik bağlamda](docs/SKYMP_CONTEXT_TR.md) kayıtlıdır.

# Yerel SkyMP sunucusu

5 Eylül 2026: resmi `main@756fb86` Windows sunucu çıktısı bu bilgisayarda hazırlandı. **Sunucu başlangıç testi geçti:** native modül yüklendi, yerel gamemode çalıştı, beş temel ESM okundu ve HTTP manifesti doğrulandı. Node `24.11.1` kullanıldı. Bu sonuç oyun istemcisinin bağlandığını veya perk senkronizasyonunun çalıştığını göstermiyor. Test bittiğinde sunucu süreci kapatıldı.

Sunucuyu proje kökünden PowerShell ile başlatmak için:

```powershell
.\scripts\start-local-server.ps1
```

Sunucu başlangıcını otomatik doğrulayıp süreci kapatmak için:

```powershell
node .\scripts\test-local-server.mjs
```

Başlangıç script'i çalışırken terminali açık tutun; Ctrl+C ile durdurun. Test script'i mevcut bir sunucu oturumunu kullanmak için tasarlanmadı; aynı portlarda başka sunucu çalıştırılmamalı.

Yerel çalışma dizini `C:\Users\kerim\Documents\ChatGPT\SkyMPTR Test\.local\skymp-756fb86\server`. Ayarlar burada `server-settings.json`, dünya kaydı `world`, deney betiği `gamemode.js` altında. `provenance.json` kullanılan commit, CI çalışması ve native modül hash'ini; `smoke-test.log` test çıktısını içeriyor. `.local` ve `.research` Git dışında tutuluyor.

Başlangıç ayarları: oyun portu `7777/UDP`, HTTP `127.0.0.1:3000`, iki oyuncu kapasitesi, offline giriş ve dosya tabanlı dünya kaydı. NPC'ler kapalı. Temel ESM'ler kurulu oyundan mutlak yollarla okunuyor; oyun dosyaları kopyalanmadı veya değiştirilmedi. HTTP servisi şu anda yalnızca bu bilgisayardan erişilebilir. Arkadaş bağlantısı hazırlanırken gereken kaynak erişimi, istemci ayarı ve ağ erişimi birlikte düzenlenmeli.

Başka bilgisayarda aynı sunucuyu hazırlamak için GitHub CLI, açık bir GitHub oturumu ve Node gerekir. O bilgisayardaki Skyrim klasörünü vererek proje kökünden çalıştırın:

```powershell
.\scripts\prepare-local-server.ps1 -SkyrimDirectory 'C:\Games\steamapps\common\Skyrim Special Edition'
node .\scripts\test-local-server.mjs
```

Hazırlama script'i mevcut çalışma dizininin üzerine yazmaz. Kaynak commit'i, CI çıktısı ve native SHA-256 değeri `sources.lock.json` dosyasından alınır. Betikler ayrı, temiz bir klasörde aynı bilgisayarda kaynak indirme ve sunucu hazırlama için çalıştırıldı; hazırlanan sunucunun başlangıç testi **6 Eylül 2026'da geçti**. Başka bir fiziksel bilgisayarda deneme henüz yapılmadı. [Test kaydı](evidence/2026-09-06-handoff-verification.json).

Kaynak çıktı: [başarılı resmi CI çalışması](https://github.com/skyrim-multiplayer/skymp/actions/runs/33943379757), `server-dist`. İndirilen paketin varsayılan `gamemode.js` dosyası sonunda sunucuyu kapatan bir CI testi içeriyordu; çalışma kopyasında küçük bir başlangıç gamemode'u ile değiştirildi. Orijinal indirme önbelleği korundu. CI artifact süresi 4 Aralık 2026'da dolacak görünüyor; uzun vadeli geliştirme için kendi kaynak derlememiz ve saklanan çıktılarımız gerekli.

Başlangıç logunda upstream'in `punycode` kullanım uyarısı ve varsayılan hasar çarpanı ayarına dönüş uyarısı var. Başlangıcı engellemediler. `master` boş olsa da logda varsayılan gateway adresi görünüyor; offline kontrolü sunucu listeleme ve normal hesap girişini atlıyor. Bütün ağ trafiğinin tamamen dış servissiz olduğu henüz doğrulanmadı.

Tam iki oyunculu altyapı için sıradaki doğrulamalar: uyumlu istemci/SKSE/Platform paketini test profiline kurmak; gerekli UI dosyalarını hazırlamak; farklı profil kimlikleriyle bağlanmak; hareket ve envanteri iki tarafta görmek; çık-gir ve sunucu yeniden başlatmada karakter kaydını kontrol etmek. Native kaynak geliştirmesi için VS 2022 ve Yarn kurulumu hâlâ gerekli.

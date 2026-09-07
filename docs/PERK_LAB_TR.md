# Üç ağaç için geçici perk deneyi

Kullanıcı kararı: önce Demircilik, Simya ve Efsunlama. CSF `SKILLS.json` halen bu üç **vanilla** ağacı seçer. Ağaç görselleri, düğüm adları ve açıklamaları yeniden tasarlanmadı. Aşağıdaki kurallar yeni sunucu `PerkSystem` uygulamasına aittir; nihai meslek ekonomisi değildir.

Yeni perk profili: **3 puan**, Demircilik **30**, Simya **20**, Efsunlama **20**. Var olan profil otomatik sıfırlanmaz veya puan verilerek değiştirilmez.

| Ağaç | İzin verilen perk | Form ID | Asgari seviye | Ön koşul |
|---|---|---|---|---|
| Demircilik | Steel Smithing | 0005218E | 20 | Yok |
| Demircilik | Dwarven Smithing | 000CB40F | 30 | Steel Smithing |
| Simya | Alchemist 1 | 000BE127 | 15 | Yok |
| Simya | Alchemist 2 | 000C07CA | 20 | Alchemist 1 |
| Efsunlama | Enchanter 1 | 000BEE97 | 15 | Yok |
| Efsunlama | Enchanter 2 | 000C07CE | 20 | Enchanter 1 |

Menü diğer vanilla düğümleri de gösterir. Menüden çıkışta yalnızca izin verilen seçimler kalır; diğerleri sunucu eşitlemesiyle kaldırılır ve puan sunucudaki değere döner. Aynı perk tekrar istendiğinde ikinci kez puan harcanmaz. İstemcinin gönderdiği `profileId` kullanılmaz; giriş oturumundaki profil esas alınır.

## Yeniden üretme

1. `scripts/build-client.ps1` çalıştırın.
2. `skymp5-server` içinde `npm exec --yes --package=yarn@1.22.22 -- yarn install --frozen-lockfile`, ardından `npm run build-ts` çalıştırın.
3. Test sunucusu ve lab Skyrim/MO2 kapalıyken `scripts/deploy-lab-code.ps1` çalıştırın. Bu adım sunucu JS/map, gamemode ve istemci JS dosyalarını birlikte kopyalar, öncekileri `.local/code-backups` altında yedekler. Native DLL, dünya, ayar ve kayıt dosyalarını değiştirmez.
4. `node scripts/test-local-server.mjs` başlangıç/manifest testidir; kendi sunucusunu kapatır.
5. `scripts/start-local-server.ps1` ve `scripts/start-game-lab.ps1` ile oyuna girin.

Temiz hazırlama betiği halen upstream sunucu JS çıktısını kurar; **yerel kod derlemesi ve 3. adım atlanmamalıdır**. Başlatıcı release paketi bu değişikliği henüz içermez.

## Oyun kabul testi — henüz yapılmadı

- Görünümü kaydedilmemiş karakterde oluşturma menüsü açılmalı. Tamamlanmış karaktere tekrar girişte açılmamalı.
- Yeni perk profilinde üç seviye ve 3 puan görünmeli.
- Çelik işçiliğini seçip menüden çıkın: 2 puan kalmalı. Cüce işçiliğini seçin: 1 puan kalmalı. Simya ilk rütbesini seçin: 0 puan kalmalı.
- Tekrar bağlantıda seçimler ve puan korunmalı. Efsunlama dalı farklı test profiliyle denenebilir; mevcut karakteri sıfırlamayın.
- İzin verilmeyen bir düğüm seçilebiliyorsa menü kapanınca geri alınmalı.
- Üretim/iksir/efsunlama sonucunu iki oyuncuyla ölçmek **ayrı testtir**. Perkin yerel görünmesi, native menünün açılması veya JSON kaydı sunucudaki üretim hesabının doğru olduğunu kanıtlamaz. C++ üretim/perk etkilerine bu değişiklikte müdahale edilmedi.

Otomatik testler: `node --test skymp5-server/tests/rpSystems.test.cjs skymp5-client/tests/perkSyncService.test.cjs skymp5-client/tests/settingsService.test.cjs`.

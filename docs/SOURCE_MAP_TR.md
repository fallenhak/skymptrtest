# Kaynak kod haritası

5 Eylül 2026 araştırmasının sabit commit'lerine bağlı okuma haritası. Bağlantılar ana dalın daha sonra değişmesinden etkilenmez. Yerel kopyalar `scripts/restore-sources.ps1` ile hazırlanır; sürümler [sources.lock.json](../sources.lock.json) içindedir.

## SkyMP

Yerel geliştirme tabanı artık repo köküdür. Aşağıdaki GitHub bağlantıları incelenen upstream commit'ini gösterir; bizim değişikliklerimiz kökteki dosyalarda bulunur. Örneğin `settingsService.ts` dosyasına yerel manifest yolu ve hata kontrolü eklenmiştir.

| Konu | Kaynak | Neden bakılmalı? |
| --- | --- | --- |
| Sunucu başlangıcı | [skymp5-server/ts/index.ts](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-server/ts/index.ts) | Native modül, sistemler ve çalışma akışı |
| Offline giriş | [skymp5-server/ts/systems/login.ts](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-server/ts/systems/login.ts) | profileId ve karakter giriş akışı |
| Spawn | [skymp5-server/ts/systems/spawn.ts](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-server/ts/systems/spawn.ts) | İlk karakter/dünya girişini incelemek için |
| HTTP servisi | [skymp5-server/ts/ui.ts](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-server/ts/ui.ts) | Manifest, kaynak servisi, port ve listen host |
| İstemci ayarları | [skymp5-client/src/services/services/settingsService.ts](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-client/src/services/services/settingsService.ts) | server-ip/server-port, master ve ayar okuma |
| Load order | [skymp5-client/src/services/services/loadOrderVerificationService.ts](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-client/src/services/services/loadOrderVerificationService.ts) | İstemci mod kontrolü ve manifest yolu |
| Ayar üretimi | [cmake/scripts/generate_client_settings.cmake](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/cmake/scripts/generate_client_settings.cmake) | Güncel istemci ayar biçimi |
| Yerel ilerleme engeli | [skymp5-client/src/services/services/disableSkillAdvanceService.ts](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-client/src/services/services/disableSkillAdvanceService.ts) | XP ve skill kullanım katsayılarının sıfırlanması |
| Menü kısıtlaması | [skymp5-client/src/services/services/enforceLimitationsService.ts](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-client/src/services/services/enforceLimitationsService.ts) | setInChargen çağrısı; menü davranışı oyun içinde test edilmeli |
| SweetPie menüsü | [skymp5-client/src/services/services/sweetTaffySkillMenuService.ts](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-client/src/services/services/sweetTaffySkillMenuService.ts) | Mod adına bağlı özel menü; genel zorunluluk değil |
| SweetPie statik perkleri | [skymp5-client/src/services/services/sweetTaffyStaticPerksService.ts](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-client/src/services/services/sweetTaffyStaticPerksService.ts) | Koşullu perk verme davranışı |
| SweetPie dinamik perkleri | [skymp5-client/src/services/services/sweetTaffyDynamicPerksService.ts](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-client/src/services/services/sweetTaffyDynamicPerksService.ts) | Koşullu perk güncelleme davranışı |
| Oyun API'si | [skyrim-platform/src/platform_se/codegen/convert-files/skyrimPlatform.ts](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skyrim-platform/src/platform_se/codegen/convert-files/skyrimPlatform.ts) | addPerk/removePerk/hasPerk, perk puanı ve perkEntryRun tanımları |
| Sunucu aktör değerleri | [skymp5-server/cpp/server_guest_lib/ActorValues.h](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-server/cpp/server_guest_lib/ActorValues.h) | Hazır durumun kapsamı; tam skill progression varsaymayın |
| Kalıcı aktör/nesne durumu | [skymp5-server/cpp/server_guest_lib/MpChangeForms.h](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-server/cpp/server_guest_lib/MpChangeForms.h) | Kayıt alanları; dosya adı çoğul MpChangeForms |
| Dinamik alanlar | [skymp5-server/cpp/server_guest_lib/DynamicFields.h](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-server/cpp/server_guest_lib/DynamicFields.h) | Karakter ilerleme verisi için araştırılabilecek JSON alanları; tasarım uygulanmadı |
| Üretim | [skymp5-server/cpp/server_guest_lib/CraftService.cpp](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-server/cpp/server_guest_lib/CraftService.cpp) | RecipeItemsMatch, EvaluateCraftRecipeConditions, tempering sınırı |
| Üretim olayı | [skymp5-server/cpp/server_guest_lib/gamemode_events/CraftEvent.cpp](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-server/cpp/server_guest_lib/gamemode_events/CraftEvent.cpp) | onCraft ve başarılı üretim işlemleri |
| Koşul işlevleri | [skymp5-server/cpp/server_guest_lib/condition_functions/ConditionFunctionFactory.cpp](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-server/cpp/server_guest_lib/condition_functions/ConditionFunctionFactory.cpp) | Desteklenen koşullar; incelenen kayıtlarda HasPerk yok |
| Sunucu hasarı | [skymp5-server/cpp/server_guest_lib/formulas/TES5DamageFormula.cpp](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-server/cpp/server_guest_lib/formulas/TES5DamageFormula.cpp) | Vanilla yerel perk etkisi sunucu formülüne otomatik taşınmaz |
| Derleme seçenekleri | [CMakeLists.txt](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/CMakeLists.txt) | Build klasörü ve VS 2022 generator koşulları |
| Geliştirici belgesi | [CONTRIBUTING.md](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/CONTRIBUTING.md) | Upstream derleme akışı; bazı sürüm tavsiyeleri eskimiş |
| Gamemode API | [docs/docs_serverside_scripting_reference.md](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/docs/docs_serverside_scripting_reference.md) | mp.makeProperty, mp.makeEventSource ve mp.get/set |
| Browser entegrasyonu | [skyrim-platform/src/platform_se/skyrim_platform/BrowserApiNirnLab.cpp](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skyrim-platform/src/platform_se/skyrim_platform/BrowserApiNirnLab.cpp) | Güncel yerel UI dosyasının açılması |

## Custom Skills Framework

Yerel taban: `.research/custom-skills`. Bu aday henüz SkyMP'ye entegre edilmedi.

| Konu | Kaynak | Neden bakılmalı? |
| --- | --- | --- |
| Native menü | [src/CustomSkills/CustomSkillsManager.cpp](https://github.com/Exit-9B/CustomSkills/blob/8be7055f2483a261e0c377e2e8fc04b34116c68f/src/CustomSkills/CustomSkillsManager.cpp) | OpenStatsMenu ve özel SKILLS grubu |
| Ağaç kurulumu | [src/CustomSkills/Hooks/MenuSetup.cpp](https://github.com/Exit-9B/CustomSkills/blob/8be7055f2483a261e0c377e2e8fc04b34116c68f/src/CustomSkills/Hooks/MenuSetup.cpp) | StatsMenu kurulumuna müdahale; değişken ağaç sayısı |
| Grup şeması | [docs/schema/CustomSkill.json](https://github.com/Exit-9B/CustomSkills/blob/8be7055f2483a261e0c377e2e8fc04b34116c68f/docs/schema/CustomSkill.json) | Standart skill adlarıyla custom skill nesnelerini birleştirme |
| Skill şeması | [docs/schema/skill.json](https://github.com/Exit-9B/CustomSkills/blob/8be7055f2483a261e0c377e2e8fc04b34116c68f/docs/schema/skill.json) | Global, XP ve perk düğümü tanımları |
| Perk seçimi | [src/CustomSkills/Hooks/SkillProgress.cpp](https://github.com/Exit-9B/CustomSkills/blob/8be7055f2483a261e0c377e2e8fc04b34116c68f/src/CustomSkills/Hooks/SkillProgress.cpp) | SelectPerk/puan hook'ları; ağ onayı API'si olduğu varsayılmamalı |
| Olay API'si | [include/CustomSkills/Events.h](https://github.com/Exit-9B/CustomSkills/blob/8be7055f2483a261e0c377e2e8fc04b34116c68f/include/CustomSkills/Events.h) | İncelenen SkillIncreaseEvent; sunucu perk satın alma onayı değil |

Yazarın [örnekleri](https://github.com/Exit-9B/CustomSkills/wiki/Examples) mevcut 18 ağacın yanına yeni ağaç eklenmesini gösteriyor. [3.2.0 sürüm notları](https://github.com/Exit-9B/CustomSkills/releases/tag/v3.2.0) 1.7.99/Address Library 12 güncellemesini bildiriyor. Bu iki dış sayfa zamanla güncellenebilir; kendi runtime kombinasyonumuzun çalıştığına ilişkin oyun içi test yerine geçmez.

## Test başlangıç noktaları

SkyMP'nin `unit` klasöründeki `TES5DamageFormulaTest.cpp`, `HitTest.cpp`, `DropItemTest.cpp`, `InventoryTest.cpp` ve `SaveStorageTest.cpp` mevcut davranışı incelemek için başlangıç noktalarıdır. Bu devir sırasında upstream birim testleri çalıştırılmadı. Oynanabilir altyapı ve perk kabul koşulları [HANDOFF_TR.md](HANDOFF_TR.md) içinde.

# Skyrim'in kendi menüsüyle RP perk ve meslek sistemi

5 Eylül 2026. Bu belge kullanıcı tercihlerine göre geliştirme sırasını günceller: **çalışan yerel altyapı → mevcut perklerin gerçek etkileri ve kalıcılığı → RP dengesi → yeni meslek ağaçları**. Bash ve kilit senkronizasyonu şu an ilk özellik hedefi değil.

Kullanıcı Faalgrin ve Keizaal'ın güncel sürümde çalıştığını belirtti. Eski README sürüm listesi bir uyumsuzluk kararı değildir. Kendi istemci paketimizi güncel kurulumda deneyerek sürüm eşleştirmesini doğrulayacağız; önceden downgrade kararı alınmış değil.

**İstenen deneyim**

Skyrim'in mevcut yetenekleri ve yıldız ağacı biçimindeki perk menüsü temel alınacak. Perklerin koşulları, etkileri, kademe sayıları, puan maliyetleri ve ilerleme hızı RP ihtiyacına göre değiştirilebilecek. Madencilik, terzilik ve aşçılık kendi ağaçlarına sahip olabilecek. Simyacılık ve demircilik mevcut ağaçların üzerine geliştirilecek. Oyuncu perk aldığında menüdeki görünüm, oyun davranışı ve sunucudaki karakter kaydı aynı durumu gösterecek.

**Kaynak kodundan doğrulanan durum**

- SkyMP istemcisi `DisableSkillAdvanceService` içinde karakterin yerel seviye XP katsayısını ve 18 yeteneğin kullanım katsayılarını sıfırlıyor. Bu, bütün perk kayıtlarının silindiği anlamına gelmiyor. [Kaynak](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-client/src/services/services/disableSkillAdvanceService.ts).
- SweetPie'ye özgü menü ve otomatik perk servisleri mod adına göre koşullu çalışıyor. Bunlar kendi lab'ımızda zorunlu bir arayüz tercihi değil. Ayrıca `EnforceLimitationsService` tarafından çağrılan `setInChargen` menü/gelişim davranışı açısından test edilmeli. [Özel menü](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-client/src/services/services/sweetTaffySkillMenuService.ts), [kısıtlama servisi](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-client/src/services/services/enforceLimitationsService.ts).
- İstemci API'sinde perk ekleme/çıkarma, perk sorgulama ve puan okuma/yazma imkânları var. `perkEntryRun` olayı ise bir perk etkisinin çalışmasını bildiriyor; menüden perk satın alma onayı olarak değerlendirilmemeli. İncelenen API'de doğrudan sunucu onayını bekleyen bir satın alma callback'i saptanmadı. [API tanımları](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skyrim-platform/src/platform_se/codegen/convert-files/skyrimPlatform.ts).
- Sunucunun `ActorValues` durumu sağlık/magicka/stamina ve yenilenme değerleri etrafında; bütün yerel yetenek ağacını ve perk ilerlemesini otomatik taşıyan hazır bir sistem varsayılamaz. [ActorValues](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-server/cpp/server_guest_lib/ActorValues.h).
- Sunucuda craft tarifi, istasyon ve envanter işleme altyapısı mevcut; `onCraft` olayı deneyler için kullanılabilir. Bununla birlikte temel condition factory içinde `HasPerk` kaydı bulunmuyor ve `RecipeItemsMatch` tempering tariflerini açıkça dışlıyor. Perkli üretim ve eşya iyileştirme ayrıca ele alınmalı. [CraftService](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-server/cpp/server_guest_lib/CraftService.cpp), [condition factory](https://github.com/skyrim-multiplayer/skymp/blob/756fb86b05ab5c2fae4f9bc3c6d52ca580b8bdd3/skymp5-server/cpp/server_guest_lib/condition_functions/ConditionFunctionFactory.cpp).

Menüyü açıp yerel perkleri vermek tek başına yeterli değil: örneğin oyun motoru yerel hasarı değiştirse de SkyMP sunucusunun hasar formülü kendi hesabını yapıyor. Her perk ailesinin etkisi, hesaplamayı yapan tarafta uygulanmalı; aynı çarpanı hem istemcide hem sunucuda iki kez uygulamamalıyız.

**Oyunun menüsünü koruyarak yeni ağaç ekleme**

Somut aday **Custom Skills Framework (CSF)**. İncelenen kaynak `Exit-9B/CustomSkills@8be7055f2483a261e0c377e2e8fc04b34116c68f`, sürüm 3.2.0. Kod gerçek `RE::StatsMenu` menüsünü açıyor ve ağaç sayısını/görünümünü değiştiriyor. Şeması aynı tanımda standart yetenek adlarıyla yeni yetenek nesnelerini birlikte kabul ediyor. Bu nedenle mevcut ağaçları koruyup yeni meslekleri aynı oyun menüsüne ekleme yaklaşımı teknik olarak destekleniyor. [StatsMenu entegrasyonu](https://github.com/Exit-9B/CustomSkills/blob/8be7055f2483a261e0c377e2e8fc04b34116c68f/src/CustomSkills/CustomSkillsManager.cpp), [menü kurulum kodu](https://github.com/Exit-9B/CustomSkills/blob/8be7055f2483a261e0c377e2e8fc04b34116c68f/src/CustomSkills/Hooks/MenuSetup.cpp), [ayar şeması](https://github.com/Exit-9B/CustomSkills/blob/8be7055f2483a261e0c377e2e8fc04b34116c68f/docs/schema/CustomSkill.json).

Yazarın verdiği Constellations örneği normal 18 yeteneği 21'e çıkarıyor; özel `SKILLS.json` ve ağaç sayısına uygun NIF skydome modeli kullanıyor. Yeni ağacı eklemek yalnızca menüye yazı koymak değil: düğümler, bağlantılar, perk kayıtları ve uygun görsel model gerekiyor. [Yazarın örnekleri](https://github.com/Exit-9B/CustomSkills/wiki/Examples).

CSF bir çok oyunculu perk sistemi sağlamıyor. Menüdeki seçim ve puan işlemini SkyMP'ye bağlayan bir katman geliştirmeliyiz. Mevcut `SelectPerk`/puan hook'ları başlangıç noktası olabilir; doğrudan ağ onayı beklemeye uygun hazır bir API olduğu varsayılmamalı. Gerekirse küçük bir SKSE eklentisi veya sınırlı CSF uyarlaması gerekir. Oyun menüsünün açıkken ağı ve dünyayı durdurmaması da test edilmelidir. [Perk seçim kodu](https://github.com/Exit-9B/CustomSkills/blob/8be7055f2483a261e0c377e2e8fc04b34116c68f/src/CustomSkills/Hooks/SkillProgress.cpp).

CSF 3.2.0'ın 2 Eylül 2026 sürüm notu Skyrim 1.7.99/Address Library 12 güncellemesini bildiriyor. Yeni runtime'a yönelik çalışma mevcut; kendi 1.7.104 + SkyMP kombinasyonumuzdaki birlikte çalışma henüz oyun içinde denenmedi. Modern CSF deposu MIT lisanslı. CSF kurulmadı veya kodu değiştirilmedi. [Sürüm notu](https://github.com/Exit-9B/CustomSkills/releases/tag/v3.2.0).

**Önerilen ilerleme ve etki modeli**

Oyuncu, bildiği Skyrim menüsünden perk seçer. İstemci seçimi bir istek olarak sunucuya yollar. Sunucu karakterin XP/seviyesini, ön koşulları, perk sırasını ve kalan puanını kontrol eder; seçimi bir kez kaydeder; sonucu istemciye geri yollar. Onaylanan perk ve puanlar yerel menüye uygulanır. Bağlantı kesildiğinde, karakter değiştiğinde veya sunucu yeniden başladığında sunucudaki kayıt yeniden yüklenir.

İlerleme verisi karakter başına saklanmalı: yetenek kimliği, XP, seviye, boş puan, seçilmiş perk/rank, şema sürümü ve değişiklik numarası. Madencilik gibi yeni yeteneklerin menüde kullandığı global değişkenler yerel görüntüleme kopyası olur; oyuncuların ortak ilerleme kaydı olarak kullanılmaz. Kayıtlarda değişen load-order sayıları yerine kararlı yetenek/perk kimlikleri ve plugin/FormID eşlemesi tutulur.

XP, doğrulanmış işlemlerden üretilir: gerçekten üretilen eşya, tüketilen malzeme, toplanan cevher. Tek bir isteğin tekrar gönderilmesi aynı ödülü veya perki ikinci kez kazandırmamalı. Bunun üretim ortamına uygun kesin protokolü ilk prototipten sonra oluşturulur.

Perkleri etki ailelerine göre kademeli desteklemeliyiz: tarif açma ve üretim koşulları; hasar/savunma; kaynak tüketimi ve verim; iksir/büyü gücü ve süresi; animasyon/aktif kullanım. Skyrim'in tüm perk entry-point davranışlarını aynı anda eksiksiz taklit etmek ilk kilometre taşı değil. Mevcut perk listesi korunabilir; her perkin destek durumu ayrı kaydedilir ve doğrulanmamış etki çalışıyor diye sunulmaz.

**Mesleklerin taslak kapsamı**

| Ağaç | Gelişim sağlayan eylem | Örnek perkler | Gerekli oyun sistemi |
| --- | --- | --- | --- |
| Simyacılık | Doğrulanmış iksir üretimi | İksir gücü, etki uzmanlığı, malzeme verimi | Dinamik iksir verisi, etki/süre ve envanter senkronizasyonu |
| Demircilik | Ekipman üretimi ve iyileştirme | Malzeme sınıfları, kalite, tamir | Tarif koşulları, tempering ve eşya kalite verisi |
| Madencilik | Başarılı cevher çıkarma | Damar bilgisi, daha iyi verim, nadir cevher | Ortak damar durumu, araç kontrolü, tükenme/yenilenme ve loot |
| Terzilik | Kumaş/deri ürün üretimi | Kumaş işleme, gelişmiş kıyafet, cep kapasitesi | Malzeme/ürün kayıtları, tarifler ve eşya etkileri |
| Aşçılık | Yemek hazırlama | Yeni tarifler, porsiyon verimi, uzun süren yemek etkileri | Üretim doğrulaması ve tüketim/buff sistemi |

Bunlar tasarım örnekleri; onaylanmış denge değerleri değil. Meslek XP'sini savaş seviyesinden ayrı tutabilen ve uzmanlaşmayı destekleyen bir yapı önerilir. Hangi mesleklerin birlikte öğrenilebildiği, toplam perk bütçesi, yeniden dağıtma, kalite basamakları ve oyuncular arası ticaret daha sonra birlikte kararlaştırılır. Bütün oyuncuların bütün dalları tamamlamasını zorunlu kılan bir ilerleme modeli baştan sabitlenmemeli.

**Uygulama sırası ve kabul koşulları**

1. **Çalışan temel:** Yerel sunucu başlangıcı geçti. İki istemci bağlantısı, hareket, envanter ve yeniden girişte karakter kaydı; ayrıca kaynak derleme zinciri tamamlanacak.
2. **Native menü ve tek perk:** Bir demircilik perkini kendi menüsünden seçme, sunucuda bir kez puan düşme, ilgili tarifin gerçekten açılması ve yeniden girişte korunması. Mevcut yerel XP engeli doğrudan kaldırılmadan sunucu kontrollü ilerleme kurulacak. Tempering etkisi ayrıca doğrulanmadan perkin tamamı destekleniyor denmeyecek.
3. **İlk yeni meslek:** Üç perkli küçük bir madencilik ağacı. Ağaç Skyrim menüsünde görünecek; perk verimi değiştirecek; iki oyuncu aynı damarı kullandığında cevher, XP ve damar durumu tutarlı kalacak.
4. **RP genişlemesi:** Simyacılık/demircilik etkilerini tamamlamak; terzilik ve aşçılık eklemek; uzmanlaşma ve ekonomi dengesini gerçek testlerle ayarlamak.

İkinci ve üçüncü adım henüz uygulanmadı. Çalışan sunucu başlangıcının nasıl tekrarlandığı `LOCAL_SERVER_TR.md` belgesinde bulunuyor. Bundan sonraki ilk özellik çalışması perk sistemi olacak; eski genel senkronizasyon adayları bekleme listesinde kalacak.

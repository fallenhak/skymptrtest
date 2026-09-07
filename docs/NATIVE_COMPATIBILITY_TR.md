# Skyrim 1.7.104 başlangıç hatası

7 Eylül 2026: Ayrı oyun kurulumu ve MO2 profil yönlendirmesi hazır. İlk gerçek oyun başlatmasında SKSE **2.3.1** doğru `1.7.104.0` EXE'yi buldu ve Skyrim Platform yüklemesine ulaştı. Kullanıcının ekran görüntüsünde `SkyrimPlatformImpl.dll`, `include/REL/ID.h(219): failed to open address library file` hatası gösterdi. Oyun dünyasına giriş gerçekleşmedi.

## Kaynakta saptanan neden

`overlay_ports/commonlibsse-ng-flatrim/portfile.cmake`, CommonLib'i `b93280e832f263dbef44e44cbe2936622a02f91a` commit'ine sabitliyor. Bu sürümün [Module.h](https://github.com/CharmedBaryon/CommonLibSSE/blob/b93280e832f263dbef44e44cbe2936622a02f91a/include/REL/Module.h) dosyasında `load_version` yalnızca minor sürüm **6** için AE seçiyor; **7** default kolundan SE oluyor. Aynı kontrol test enjeksiyonunda da bulunuyor.

[ID.h](https://github.com/CharmedBaryon/CommonLibSSE/blob/b93280e832f263dbef44e44cbe2936622a02f91a/include/REL/ID.h) AE için `versionlib-<sürüm>.bin` ve biçim 2, SE için `version-<sürüm>.bin` ve biçim 1 seçiyor. Bizde doğru `versionlib-1-7-104-0.bin` bulunuyor. Kodun 1.7'yi SE sayması yanlış dosya/biçim seçimine yol açıyor. Dosyanın adını değiştirmek runtime sınıfını, relocation kimliklerini ve biçim beklentisini düzeltmez.

## Hazırlanan düzeltme ve sınırı

`docs/patch-candidates/05-detect-skyrim-1-7-as-ae.patch`, iki sürüm sınıflandırmasına da `case 7` ekleyen bir adaydır. Sabit kaynak dosyasına uygulanması ve iki kolun değişmesi kontrol edildi. **Aktif overlay portuna eklenmedi**; 1.7 çalışması bekletiliyor.

**Yeni native DLL henüz derlenmedi veya oyunda denenmedi.** Hazır resmi CI DLL'si bu yamayı içermez. Bu düzeltme ilk dosya seçimi hatasını hedefler; 1.7'deki bütün yapı yerleşimleri, hook'lar ve oyun akışları için uyumluluk garantisi değildir. Örneğin `Hooks.cpp` içindeki `skse64_1_6_1170.dll` sabiti ayrıca değerlendirilmelidir.

## Sıradaki iş

Kullanıcı 7 Eylül'de önceki bilgisini düzeltti: Faalgrin **1.6.1170** kullanıyor; launcher güncel Steam kurulumundan ayrı bir oyun kopyası hazırlıyor. Yerel `Faalgrin/Modlist/Stock Game/SkyrimSE.exe` gerçekten **1.6.1170.0** olarak okundu. İlk oynanabilir altyapı artık bu runtime üzerinden denenecek. VS 2022 kurulumu şu aşamada kullanıcıdan istenmiyor. 1.7 desteğine dönülürse proje CMake'inin VS 2022 koşulu ve yeni native derleme gereksinimi tekrar ele alınır.

Önce doğru Address Library seçimi ve native DLL yüklemesi, sonra yerel sunucu bağlantısı, karakter oluşturma ve dünyaya giriş doğrulanacak. Perk çalışmalarına bu temel testlerden sonra geçilecek.

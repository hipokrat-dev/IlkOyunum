# Vücut Yolculuğu — Oyun Tasarım Belgesi

## Özet
Bilgisayar (macOS/Windows) için 3D, eğitici macera oyunu. Unity 6, URP.
Oyuncu, küçülerek insan vücuduna giren bir çırağı yönetir; her bölüm bir organdır.

## Hikâye
Profesör, insan vücudunu incelemek için küçülebilen bir mini araç (kapsül) geliştirir.
Çırağı yanlışlıkla bir düğmeye basar, araçla birlikte küçülür ve bir insanın vücuduna girer.
Profesör telsizden yol gösterir ve organlar hakkında bilgi verir. Öğretici bilgiler
düz metin olarak değil, profesörün telsiz konuşmalarıyla oyunun içinde verilir.

## Bölümler (modüller)
| # | Bölüm | Oynanış | Öğretilen |
|---|---|---|---|
| 1 | Ağız | Dişler arasında gezip çürük mikroplarını temizlemek, tükürük dalgalarından kaçmak | Diş çeşitleri, dil, tükürük, çürük oluşumu |
| 2 | Yemek borusu + mide | Kayarak iniş (koşu), midede asit havuzlarından kaçış | Yutma, peristaltik hareket, mide asidi |
| 3 | Bağırsak | Kıvrımlı tünellerde yol bulma, iyi bakterilere yardım / kötülerle savaş | Emilim, villuslar, faydalı bakteriler |
| 4 | Kan damarları + kalp | Kan akışıyla ilerleme, kalp ritmine göre geçiş | Kan hücreleri, kalp odacıkları |
| 5 | Akciğer | Alveollerde oksijen toplama, nefes ritmine uyma | Alveol, O2/CO2 değişimi |
| 6 | Beyin (final) | Sinir sinyallerini doğru yere ulaştırma, bulmaca | Nöronlar, beyin bölgeleri |

Her bölüm sonunda bir **Vücut Kartı** açılır (o organla ilgili 3 ilginç bilgi); kartlar koleksiyon oluşturur.

## Varsayılan kararlar (değiştirilebilir)
- **Hedef kitle:** 8–14 yaş çocuklar/gençler; aileler.
- **Görsel tarz:** Hiper gerçekçi. Islak ve parlak dokular, gerçek anatomiye uygun dişler/dil/diş eti, mikroskop görüntülerine benzeyen bakteri kolonileri, sinematik ışık ve kamera (far ışığı, alan derinliği, film greni). Çizgi film öğesi (gözlü/gülen mikrop vb.) yok.
- **Savaş:** Şiddetsiz — araç "temizleme köpüğü" atar, mikroplar köpükle eriyip kaybolur.
- **Kontroller:** WASD / ok tuşları ile sürüş, Boşluk ile köpük atma.
- **Kamera:** Aracı arkadan-üstten takip eden 3. şahıs kamera.
- **Dil:** Türkçe (ileride İngilizce eklenebilir).

## İlk hedef (MVP): Bölüm 1 — Ağız
- Dil zemini, alt ve üst çenede anatomik dizilimle dişler (kesici, köpek, küçük azı, azı); dişlerin dibinde çürük bakterisi kolonileri ve plak lekeleri.
- Araç sürülebilir, köpük atılabilir; mikrop sayacı (ör. 10 mikrop).
- Ara sıra gelen tükürük dalgası aracı iter.
- Profesörün telsiz mesajları (ekranın altında konuşma kutusu).
- 10 mikrop temizlenince: Vücut Kartı (Ağız) → "Yutkunma refleksi geliyor!" → bölüm sonu.
- Bölüm çalışma anında kodla kurulur (`Assets/Scripts/Agiz/AgizBolumu.cs`).
- Dişler, diş eti, bakteri kolonileri ve dil/mukoza dokuları Blender'da üretilir:
  `Araclar/blender/agiz_modelleri.py` çalıştırılınca `Assets/Resources/Agiz` klasörüne FBX ve PNG dokular yazılır.
- Araç da Blender'da üretilir: `Araclar/blender/arac_modeli.py` → `Assets/Resources/Ortak` (tüm bölümlerde kullanılır).
- Tükürük dalgası ve köpük hâlâ basit şekillerden oluşuyor; sıradaki görsel iş bunlar.

- Oyuncu deneyimi: başlangıç ekranı, ESC ile duraklatma, oyun kolu desteği, kodla üretilen sesler
  (motor, köpük, erime, dalga, telsiz, kalp atışı), kolonilerin üstünde soluk ışık, ekran dışındaki
  en yakın koloniye yön işareti, bölüm sonunda süre ve 1–3 yıldız (dalgaya yakalanmadan ve hızlı bitirene 3).

## Bölüm 2 — Yemek borusu ve mide (oynanabilir)
- **Yemek borusu:** araç tüpte kendiliğinden kayar; A/D ile duvarda sağa sola kayıp lokma parçalarından
  kaçılır ya da köpükle dağıtılır. Arkadan gelen kasılma dalgası (peristaltik hareket) aracı hızlandırır.
- **Mide:** asit gölleri kalkanı eritir; köpük gölü birkaç saniyeliğine nötrler (antasit gibi) ve üstünden
  geçilir. Mide kasılınca göller taşar. Kalkan biterse araç son güvenli noktaya döner. Hedef: mide çıkışı.
- Yıldız: kalkanı hiç bitirmeden ve en çok 2 lokmaya çarparak bitirene 3 yıldız.
- Kod: `Assets/Scripts/Mide`. Tüm bölümlerin ortak akışı `Assets/Scripts/Ortak/BolumTemeli.cs` içinde.
- Lokmalar, asit gölleri ve mide duvarları şimdilik kodla üretilen şekiller; Blender modeli yok.

Sıradaki bölümler aynı altyapı üzerine tek tek eklenir.

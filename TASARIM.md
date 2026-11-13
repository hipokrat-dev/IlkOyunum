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
- **Görsel tarz:** Sevimli, çizgi film; yuvarlak hatlar, parlak renkler, gülen/yaramaz mikroplar. Ürkütücü/gerçekçi değil.
- **Savaş:** Şiddetsiz — araç "temizleme köpüğü" atar, mikroplar köpükle eriyip kaybolur.
- **Kontroller:** WASD / ok tuşları ile sürüş, Boşluk ile köpük atma.
- **Kamera:** Aracı arkadan-üstten takip eden 3. şahıs kamera.
- **Dil:** Türkçe (ileride İngilizce eklenebilir).

## İlk hedef (MVP): Bölüm 1 — Ağız
- Pembe dil zemini, etrafta büyük beyaz dişler; dişlerin dibinde yeşil mikroplar.
- Araç sürülebilir, köpük atılabilir; mikrop sayacı (ör. 10 mikrop).
- Ara sıra gelen tükürük dalgası aracı iter.
- Profesörün telsiz mesajları (ekranın altında konuşma kutusu).
- 10 mikrop temizlenince: Vücut Kartı (Ağız) → "Yutkunma refleksi geliyor!" → bölüm sonu.
- Başlangıçta basit şekillerle (küre, kapsül, küp) yapılır, sonra güzelleştirilir.

Ağız bölümü bitip oynanabilir olunca diğer bölümler aynı altyapı üzerine tek tek eklenir.

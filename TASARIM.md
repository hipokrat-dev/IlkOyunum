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
| 5 | Akciğer (gezinti) | Oynanmaz: hiper gerçekçi, kısa bir sinematik gezinti; alveoller izlenip tanınır | Alveol, O2/CO2 değişimi |
| 6 | Böbrek | Süzülme ve geri emilim: yararlı maddeleri geri kazan, atıklardan kaçın | Nefron, süzme, idrar oluşumu |
| 7 | Mesane ve çıkış (final) | Mesanede çıkışı tıkayan kristalleri temizle, idrarla vücuttan çık | Mesane, idrar yolu |

Rota (2026-11-13'te kullanıcı kararıyla değişti): ağız → yemek borusu/mide → bağırsak → kan/kalp → akciğer
(yalnızca gezinti) → böbrek → mesane → vücuttan çıkış. Eski plandaki oynanabilir Akciğer ve Beyin (final)
bölümleri rotadan çıkarıldı.

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

## Bölüm 3 — Bağırsak (oynanabilir)
- Kıvrıla kıvrıla ilerleyen, duvarları salınan villuslarla kaplı tünel.
- Yol boyunca 5 dost (mavi) bakteri kolonisi; araç yaklaşınca kırmızı zararlı bakteriler koloniyi kuşatır ve
  koloninin sağlığı azalır. Köpükle kırmızılar temizlenirse koloni kurtulur; köpük mavilere de zarar verir.
- Hedef: tünelin sonundaki kapak. Yıldız: 5 koloni → 3, en az 3 koloni → 2.
- Kod: `Assets/Scripts/Bagirsak`. Villuslar ve dost bakteriler kodla üretilen şekiller.

## Bölüm 4 — Kan damarları ve kalp (oynanabilir)
- **Toplardamar:** kan akışı aracı kalbe taşır; WASD ile damarın içinde süzülünür. Alyuvarlar çarpınca
  yavaşlatır. Akyuvarlar kapsülü mikrop sanıp yakalar; köpük değince kapsülü tanıyıp sakinleşirler.
- **Kalp:** sağ kulakçık → sağ karıncık → akciğer atardamarı. Kapakçıklar kalp atışıyla açılıp kapanır
  (açıkken yeşil, kapalıyken kırmızı ışık); kapak açıkken BOŞLUK ile atılıp geçilir, kapalıysa araç geri seker.
- Yıldız: akyuvara hiç yakalanmadan ve en çok 1 kapak kaçırarak bitirene 3 yıldız.
- Kod: `Assets/Scripts/Kalp`.

## Bölüm 5 — Akciğer gezintisi (oynanmaz)
- Araç kan akışıyla alveollerin arasından geçer; oyuncu izler, profesör anlatır. Alveoller nefesle dolup
  boşalır, alyuvarlar oksijenlenince koyu kırmızıdan parlak kırmızıya döner. Yıldız yok.
- Alveol kümeleri ve dokusu Blender'da üretilir: `Araclar/blender/akciger_modelleri.py` → `Assets/Resources/Akciger`.

## Bölüm 6 — Böbrek (oynanabilir)
- Böbrek atardamarı → glomerulus (kılcal damar yumağı) → süzgeçten geçiş → nefron kanalı.
- Kanalda yararlı maddelere (mavi su, sarı glikoz, beyaz tuz) dokunulur: kana geri döner (geri emilim).
  Dikenli atıklara (kahverengi üre, mor zehir) dokunulmaz. Yıldız: yararlıların %75'i ve en çok 2 atık → 3.

## Bölüm 7 — Mesane ve çıkış (final, oynanabilir)
- Araç idrar borusunun ağzından mesaneye düşer ve idrarın yüzeyinde yüzer (kaldırma kuvveti).
- Kristaller ağır olduğu için dipte durur. DAL (SHIFT / ekrandaki DAL düğmesi) basılı tutulunca kapsül içine
  sıvı alıp batar, bırakılınca yükselir. Dipteki 7 kristal köpükle eritilir (her biri 3 vuruş).
- Mesane 110 saniyede dolar. Kristaller bitince ya da mesane dolunca çıkış açılır.
- Çıkış animasyonu: kas halkası açılır, akıntı aracı çeker, araç idrar yolundan gün ışığına doğru hızlanır.
- Final ekranı: "Yolculuk tamamlandı", puanlı altı bölümün toplam yıldızı (18 üzerinden).

## Ana menü, kartlar, tablet ve seslendirme
- **Ana menü** (`AnaMenu` sahnesi, oyunun ilk sahnesi): bölüm seçimi (kazanılan yıldızlarla) ve Vücut Kartı
  koleksiyonu. İlerleme `PlayerPrefs`'te tutulur (yıldız, biten bölüm, toplanan kartlar).
- **Tablet (Android):** ekranın sol yarısında dokunulan yerde beliren sürüş çubuğu; sağ yarının tamamı köpük.
  Duraklat, DAL ve bölüm sonu düğmeleri ekranda. Arayüz her en-boy oranında ekranı doldurur, göstergeler
  güvenli alana yerleşir. Paket: menüden *Vücut Yolculuğu → Derleme → Android Paketi (APK) Oluştur*.
- **Seslendirme:** profesörün bütün cümleleri ElevenLabs'te "Yumi - Soothing Female Narrator" sesiyle
  (Multilingual v2) üretildi; `Assets/Resources/Seslendirme/<anahtar>.mp3`. Oyun sesi telsiz efektinden geçirir.
  Bir cümlenin metni değişirse anahtarı da değişir ve o cümle yeniden seslendirilmelidir:
  `python3 Araclar/seslendirme/satirlari_cikar.py` eksikleri listeler, `bol.py` toplu kaydı cümlelere böler.

Eksikler: 2–4, 6 ve 7. bölümlerde Blender modelleri (şimdilik kodla üretilen şekiller); seslendirmede
klavye tuşlarının adı geçiyor (tablette ekran düğmeleri kullanılıyor).

# Vücut Yolculuğu — profesörün seslendirilecek bütün telsiz cümlelerini koddan çıkarır.
#
#   python3 Araclar/seslendirme/satirlari_cikar.py
#
# Assets/Scripts altındaki Telsiz(...) ve BolumuBitir(...) çağrılarındaki cümleleri (ve akciğer
# gezintisinin anlatım dizisini) bulur, her biri için oyunun kullandığı anahtarı hesaplar
# (Seslendirme.Anahtar ile aynı: UTF-8 üzerinden FNV-1a 32 bit) ve satirlar.json dosyasına yazar.
# Ses dosyaları Assets/Resources/Seslendirme/<anahtar>.mp3 adıyla kaydedilmelidir.

import glob, json, os, re

KOK = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CIKTI = os.path.join(KOK, "Araclar", "seslendirme", "satirlar.json")
SES_KLASORU = os.path.join(KOK, "Assets", "Resources", "Seslendirme")


def anahtar(metin):
    h = 2166136261
    for b in metin.encode("utf-8"):
        h ^= b
        h = (h * 16777619) & 0xFFFFFFFF
    return "s_%08x" % h


def cozumle(ham):
    return ham.replace('\\"', '"').replace("\\'", "'").replace("\\n", "\n").replace("\\\\", "\\")


DIZGI = re.compile(r'"((?:[^"\\]|\\.)*)"')


def cagri_dizgileri(kod, ad):
    """ad( ... ) çağrılarının içindeki bütün dizgi sabitleri."""
    sonuc = []
    for m in re.finditer(r"\b%s\(" % ad, kod):
        derinlik, i, tirnakta = 1, m.end(), False
        while i < len(kod) and derinlik > 0:
            c = kod[i]
            if tirnakta:
                if c == "\\":
                    i += 1
                elif c == '"':
                    tirnakta = False
            elif c == '"':
                tirnakta = True
            elif c == "(":
                derinlik += 1
            elif c == ")":
                derinlik -= 1
            i += 1
        sonuc += [cozumle(d) for d in DIZGI.findall(kod[m.end():i - 1])]
    return sonuc


satirlar, gorulen = [], set()
for yol in sorted(glob.glob(os.path.join(KOK, "Assets", "Scripts", "**", "*.cs"), recursive=True)):
    kod = open(yol, encoding="utf-8").read()
    bolum = os.path.basename(os.path.dirname(yol))
    bulunan = cagri_dizgileri(kod, "Telsiz") + cagri_dizgileri(kod, "BolumuBitir")
    bulunan += [cozumle(d) for d in re.findall(r'\(\d+f, "((?:[^"\\]|\\.)*)"\)', kod)]
    for metin in bulunan:
        if len(metin) < 12 or metin in gorulen:      # kısa sabitler (ör. renk kodu) cümle değildir
            continue
        gorulen.add(metin)
        a = anahtar(metin)
        satirlar.append({"anahtar": a, "bolum": bolum, "metin": metin,
                         "var": os.path.exists(os.path.join(SES_KLASORU, a + ".mp3"))})

json.dump(satirlar, open(CIKTI, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
eksik = [s for s in satirlar if not s["var"]]
print("cümle: %d   karakter: %d   seslendirilmemiş: %d (%d karakter)" % (
    len(satirlar), sum(len(s["metin"]) for s in satirlar), len(eksik), sum(len(s["metin"]) for s in eksik)))
for bolum in sorted({s["bolum"] for s in satirlar}):
    print("  %-10s %d" % (bolum, sum(1 for s in satirlar if s["bolum"] == bolum)))

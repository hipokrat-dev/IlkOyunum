# Vücut Yolculuğu — birden çok cümle içeren bir seslendirme kaydını cümlelere böler.
#
#   python3 Araclar/seslendirme/bol.py <kayit.mp3> <anahtar1,anahtar2,...>
#
# Kayıtta cümleler arasında uzun (yaklaşık 2,5 sn) sessizlik olmalıdır (ElevenLabs'te
# <break time="2.5s" /> ile). Parçalar Assets/Resources/Seslendirme/<anahtar>.mp3 olarak yazılır.
# Bulunan parça sayısı anahtar sayısıyla eşleşmezse hiçbir şey yazılmaz.

import json, os, re, subprocess, sys

KOK = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
HEDEF = os.path.join(KOK, "Assets", "Resources", "Seslendirme")
SATIRLAR = {s["anahtar"]: s["metin"] for s in json.load(open(os.path.join(KOK, "Araclar", "seslendirme", "satirlar.json"), encoding="utf-8"))}

kayit, anahtarlar = sys.argv[1], sys.argv[2].split(",")
esik = float(sys.argv[3]) if len(sys.argv) > 3 else 1.4

cikti = subprocess.run(["ffmpeg", "-hide_banner", "-i", kayit, "-af", "silencedetect=noise=-38dB:d=%s" % esik, "-f", "null", "-"],
                       capture_output=True, text=True).stderr
sure = float(subprocess.run(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", kayit],
                            capture_output=True, text=True).stdout)
baslar = [float(x) for x in re.findall(r"silence_start: ([\d.]+)", cikti)]
bitisler = [float(x) for x in re.findall(r"silence_end: ([\d.]+)", cikti)]

# Konuşma parçaları: sessizlikler arasındaki bölümler
parcalar, konum = [], 0.0
for i, bas in enumerate(baslar):
    if bas - konum > 0.3:
        parcalar.append((konum, bas))
    konum = bitisler[i] if i < len(bitisler) else sure
if sure - konum > 0.3:
    parcalar.append((konum, sure))

if len(parcalar) != len(anahtarlar):
    print("ESLESMEDI: %d parça bulundu, %d cümle bekleniyordu" % (len(parcalar), len(anahtarlar)))
    for a, b in parcalar:
        print("   %.2f - %.2f" % (a, b))
    sys.exit(1)

os.makedirs(HEDEF, exist_ok=True)
supheli = 0
for (a, b), anahtar in zip(parcalar, anahtarlar):
    hedef = os.path.join(HEDEF, anahtar + ".mp3")
    # Sona kısa bir sessizlik eklenir (dosya sonundaki yarım çerçeve kodlayıcıyı bozmasın)
    subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-ss", "%.3f" % max(0, a - 0.08),
                    "-to", "%.3f" % min(b + 0.1, sure), "-i", kayit, "-af", "apad=pad_dur=0.25", "-ac", "1", "-b:a", "96k", hedef],
                   check=True)
    metin = SATIRLAR.get(anahtar, "")
    hiz = len(metin) / (b - a)                       # saniyede karakter; normal konuşma 11-19
    isaret = "" if 9 <= hiz <= 22 else "   <-- ŞÜPHELİ"
    supheli += bool(isaret)
    print("%s  %5.1f sn  %4.1f kr/sn  %s%s" % (anahtar, b - a, hiz, metin[:50], isaret))
print("TAMAM %d parça, şüpheli %d" % (len(parcalar), supheli))
sys.exit(2 if supheli else 0)

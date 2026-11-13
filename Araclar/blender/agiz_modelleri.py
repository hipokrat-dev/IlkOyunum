# Vücut Yolculuğu — Ağız bölümü modelleri ve dokuları.
#
# Çalıştırma (proje kökünden):
#   /Applications/Blender.app/Contents/MacOS/Blender -b --python Araclar/blender/agiz_modelleri.py -- Assets/Resources/Agiz [onizleme.png]
#
# Üretilenler:
#   AgizModelleri.fbx : Dis_Azi, Dis_KucukAzi, Dis_Kopek, Dis_Kesici, DisEti, Bakteri_1..3
#   <dis>_albedo/_normal/_ao.png, dil_albedo/_normal.png, mukoza_albedo/_normal.png
#
# Eksenler: Blender'da Z yukarı, dişlerin dil tarafı -Y. Unity'ye aktarılınca dil tarafı +Z olur.
# Çene ölçüleri (A, B, ARKA) Assets/Scripts/Agiz/AgizBolumu.cs ile aynı olmalı.

import bpy, math, os, random, sys
from mathutils import Vector, noise

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import ortak
from ortak import *

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
CIKTI = os.path.abspath(argv[0] if argv else "Assets/Resources/Agiz")
ONIZLEME = os.path.abspath(argv[1]) if len(argv) > 1 else None
ortak.ayarla(CIKTI)

A, B, ARKA = 11.0, 16.0, -12.0

DISLER = {
    "Dis_Azi":      dict(W=1.6,  D=1.5,  H=2.6, eh=0.5,  ev=0.5),
    "Dis_KucukAzi": dict(W=1.2,  D=1.25, H=2.6, eh=0.62, ev=0.5),
    "Dis_Kopek":    dict(W=1.0,  D=1.05, H=3.5, eh=0.85, ev=1.5),
    "Dis_Kesici":   dict(W=1.25, D=0.8,  H=3.1, eh=0.6,  ev=0.3),
}


# ----------------------------------------------------------------- Dişler

def dis_noktasi(ad, fu, fv):
    P = DISLER[ad]
    W, D, H, eh, ev = P["W"], P["D"], P["H"], P["eh"], P["ev"]
    u = fu * 2 * math.pi - math.pi

    if fv < 0.22:                       # kök
        k = fv / 0.22
        rf = 0.72 * (k ** 0.45) * (0.55 + 0.45 * k)
        z = -1.3 * (1 - k)
    else:                               # kuron
        w = (fv - 0.22) / 0.78
        a = w * math.pi / 2
        rf = (0.72 + 0.28 * smooth(0, 0.42, w)) * (max(0.0, math.cos(a)) ** ev)
        z = H * (math.sin(a) ** 0.85)

    x = W * rf * spow(math.cos(u), eh)
    y = D * rf * spow(math.sin(u), eh)
    nx, ny, zf = x / W, y / D, max(0.0, z / H)
    tepe = smooth(0.7, 0.95, zf)

    if ad == "Dis_Azi":
        tumsek = sum(gauss(nx - sx * 0.42, 0.07) * gauss(ny - sy * 0.42, 0.07)
                     for sx in (-1, 1) for sy in (-1, 1))
        oluk = (gauss(ny, 0.012) * smooth(0.9, 0.5, abs(nx)) +
                gauss(nx, 0.012) * smooth(0.9, 0.4, abs(ny)))
        z += H * tepe * (0.13 * tumsek - 0.14 * min(1.0, oluk))
        y *= 1 - 0.05 * gauss(nx, 0.03) * smooth(0.35, 0.9, zf)
    elif ad == "Dis_KucukAzi":
        tumsek = 0.16 * gauss(nx, 0.12) * gauss(ny - 0.4, 0.07) + 0.09 * gauss(nx, 0.12) * gauss(ny + 0.42, 0.07)
        oluk = gauss(ny, 0.015) * smooth(0.85, 0.4, abs(nx))
        z += H * tepe * (tumsek - 0.12 * oluk)
    elif ad == "Dis_Kopek":
        if y > 0:
            y *= 1 + 0.08 * gauss(nx, 0.1)
    elif ad == "Dis_Kesici":
        y *= lerp(1.0, 0.3, smooth(0.2, 1.0, zf))
        x *= 0.88 + 0.12 * smooth(0.0, 0.7, zf)
        if y < 0:
            y *= 1 - 0.4 * gauss(zf - 0.62, 0.06) * (1 - min(1.0, nx * nx))
        z += H * 0.008 * math.cos(nx * 3 * math.pi) * smooth(0.9, 1.0, zf)

    p = Vector((x, y, z))
    p += Vector(noise.noise_vector(p * 0.8)) * 0.03      # hafif asimetri
    return p, (fu, fv)


def dis_yap(ad):
    obj = izgara_nesnesi(ad, 96, 64, lambda fu, fv: dis_noktasi(ad, fu, fv), True)
    obj.data.materials.append(mine_malzemesi(ad, DISLER[ad]["H"]))
    return obj


# ----------------------------------------------------------------- Malzemeler

def mine_malzemesi(ad, H):
    m, agac, bsdf = yeni_malzeme("Mine_" + ad)
    koord = dugum(agac, "ShaderNodeTexCoord")
    ayir = dugum(agac, "ShaderNodeSeparateXYZ")
    bagla(agac, koord.outputs["Object"], ayir.inputs[0])
    yukseklik = matematik(agac, "DIVIDE", ayir.outputs["Z"], H)

    # Kökten kesici kenara renk geçişi: sarımsı kök/boyun -> fildişi gövde -> yarı saydam kenar
    govde = rampa(agac, [(0.0, (0.74, 0.58, 0.36)), (0.07, (0.86, 0.75, 0.55)),
                         (0.38, (0.93, 0.9, 0.81)), (1.0, (0.95, 0.94, 0.9))])
    bagla(agac, yukseklik, govde.inputs[0])

    # Oluklarda ve girintilerde lekelenme
    geo = dugum(agac, "ShaderNodeNewGeometry")
    leke = rampa(agac, [(0.44, (0.5, 0.36, 0.2)), (0.5, (1, 1, 1))])
    bagla(agac, geo.outputs["Pointiness"], leke.inputs[0])
    renk = karistir(agac, "MULTIPLY", 0.55, govde.outputs[0], leke.outputs[0])

    benek = dugum(agac, "ShaderNodeTexNoise")
    benek.inputs["Scale"].default_value = 2.5
    benek.inputs["Detail"].default_value = 5
    ton = rampa(agac, [(0.3, (0.9, 0.88, 0.84)), (0.7, (1, 1, 1))])
    bagla(agac, benek.outputs["Fac"], ton.inputs[0])
    renk = karistir(agac, "MULTIPLY", 1.0, renk, ton.outputs[0])
    bagla(agac, renk, bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.2

    # Mine yüzeyindeki ince büyüme çizgileri ve mikro pürüz
    dalga = dugum(agac, "ShaderNodeTexWave", wave_type="BANDS", bands_direction="Z")
    dalga.inputs["Scale"].default_value = 14
    dalga.inputs["Distortion"].default_value = 1.5
    puruz = dugum(agac, "ShaderNodeTexNoise")
    puruz.inputs["Scale"].default_value = 30
    toplam = matematik(agac, "ADD", matematik(agac, "MULTIPLY", dalga.outputs["Fac"], 0.06), puruz.outputs["Fac"])
    kabart = dugum(agac, "ShaderNodeBump")
    kabart.inputs["Strength"].default_value = 0.12
    kabart.inputs["Distance"].default_value = 0.05
    bagla(agac, toplam, kabart.inputs["Height"])
    bagla(agac, kabart.outputs[0], bsdf.inputs["Normal"])
    return m


def simit_koordinati(agac):
    """UV'yi 4B simit yüzeyine sarar: bu koordinatla örneklenen her doku kusursuz döşenir."""
    koord = dugum(agac, "ShaderNodeTexCoord")
    ayir = dugum(agac, "ShaderNodeSeparateXYZ")
    bagla(agac, koord.outputs["UV"], ayir.inputs[0])
    au = matematik(agac, "MULTIPLY", ayir.outputs["X"], 2 * math.pi)
    av = matematik(agac, "MULTIPLY", ayir.outputs["Y"], 2 * math.pi)
    birles = dugum(agac, "ShaderNodeCombineXYZ")
    bagla(agac, matematik(agac, "COSINE", au), birles.inputs[0])
    bagla(agac, matematik(agac, "SINE", au), birles.inputs[1])
    bagla(agac, matematik(agac, "COSINE", av), birles.inputs[2])
    return birles.outputs[0], matematik(agac, "SINE", av)


def doku4(agac, tur, vektor, w, olcek, **ozellik):
    n = dugum(agac, tur, **ozellik)
    bagla(agac, vektor, n.inputs["Vector"])
    bagla(agac, w, n.inputs["W"])
    n.inputs["Scale"].default_value = olcek
    return n


def dil_malzemesi():
    m, agac, bsdf = yeni_malzeme("Dil")
    v, w = simit_koordinati(agac)

    # İpliksi (filiform) papillalar: sık, küçük tümsekler
    ince = doku4(agac, "ShaderNodeTexVoronoi", v, w, 13.0, voronoi_dimensions="4D", feature="F1")
    h_ince = aralik(agac, ince.outputs["Distance"], 0.25, 0.95, 1.0, 0.0)

    # Mantarsı (fungiform) papillalar: seyrek, büyük, kırmızı kabarcıklar
    iri = doku4(agac, "ShaderNodeTexVoronoi", v, w, 3.6, voronoi_dimensions="4D", feature="F1")
    hucre = dugum(agac, "ShaderNodeSeparateColor")
    bagla(agac, iri.outputs["Color"], hucre.inputs[0])
    secili = matematik(agac, "GREATER_THAN", hucre.outputs[0], 0.62)
    h_iri = matematik(agac, "MULTIPLY", aralik(agac, iri.outputs["Distance"], 0.3, 0.6, 1.0, 0.0), secili)

    bulut = doku4(agac, "ShaderNodeTexNoise", v, w, 1.6, noise_dimensions="4D")
    bulut.inputs["Detail"].default_value = 4

    pembe, pas, kirmizi = (0.62, 0.2, 0.23), (0.83, 0.6, 0.58), (0.7, 0.13, 0.15)
    kaplama = matematik(agac, "MULTIPLY", matematik(agac, "MULTIPLY", h_ince, bulut.outputs["Fac"]), 0.75)
    renk = karistir(agac, "MIX", kaplama, pembe, pas)
    renk = karistir(agac, "MIX", matematik(agac, "MULTIPLY", h_iri, 0.85), renk, kirmizi)
    bagla(agac, renk, bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.4

    yukseklik = matematik(agac, "ADD", matematik(agac, "MULTIPLY", h_ince, 0.45), h_iri)
    kabart = dugum(agac, "ShaderNodeBump")
    kabart.inputs["Strength"].default_value = 1.0
    kabart.inputs["Distance"].default_value = 0.06
    bagla(agac, yukseklik, kabart.inputs["Height"])
    bagla(agac, kabart.outputs[0], bsdf.inputs["Normal"])
    return m


def mukoza_malzemesi():
    m, agac, bsdf = yeni_malzeme("Mukoza")
    v, w = simit_koordinati(agac)

    bulut = doku4(agac, "ShaderNodeTexNoise", v, w, 1.4, noise_dimensions="4D")
    bulut.inputs["Detail"].default_value = 6
    ton = rampa(agac, [(0.3, (0.5, 0.13, 0.16)), (0.72, (0.72, 0.3, 0.31))])
    bagla(agac, bulut.outputs["Fac"], ton.inputs[0])

    # İnce kılcal damar ağı
    damar = doku4(agac, "ShaderNodeTexNoise", v, w, 2.4, noise_dimensions="4D")
    damar.inputs["Detail"].default_value = 2
    sapma = matematik(agac, "ABSOLUTE", matematik(agac, "SUBTRACT", damar.outputs["Fac"], 0.5))
    cizgi = aralik(agac, sapma, 0.0, 0.012, 1.0, 0.0)
    renk = karistir(agac, "MIX", matematik(agac, "MULTIPLY", cizgi, 0.3), ton.outputs[0], (0.36, 0.07, 0.13))
    bagla(agac, renk, bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.3

    kaba = doku4(agac, "ShaderNodeTexNoise", v, w, 4.0, noise_dimensions="4D")
    ince = doku4(agac, "ShaderNodeTexNoise", v, w, 16.0, noise_dimensions="4D")
    yukseklik = matematik(agac, "ADD", kaba.outputs["Fac"], matematik(agac, "MULTIPLY", ince.outputs["Fac"], 0.25))
    kabart = dugum(agac, "ShaderNodeBump")
    kabart.inputs["Strength"].default_value = 0.6
    kabart.inputs["Distance"].default_value = 0.08
    bagla(agac, yukseklik, kabart.inputs["Height"])
    bagla(agac, kabart.outputs[0], bsdf.inputs["Normal"])
    return m


# ----------------------------------------------------------------- Doku pişirme

def doseme_pisir(ad, malzeme):
    bpy.ops.mesh.primitive_plane_add(size=2)
    duzlem = bpy.context.active_object
    duzlem.data.materials.append(malzeme)
    pisir(duzlem, ad + "_albedo", "DIFFUSE", kenar=0)
    pisir(duzlem, ad + "_normal", "NORMAL", kenar=0)
    bpy.data.objects.remove(duzlem)


# ----------------------------------------------------------------- Diş eti

def cene_yolu():
    """Alt çenenin U biçimli yolu (Blender XY; ağzın önü -Y)."""
    n = []
    for i in range(40):
        n.append(Vector((A, -lerp(ARKA - 2, 0, i / 40), 0)))
    for i in range(160):
        r = math.pi * i / 160
        n.append(Vector((A * math.cos(r), -B * math.sin(r), 0)))
    for i in range(41):
        n.append(Vector((-A, -lerp(0, ARKA - 2, i / 40), 0)))
    return n


def dis_konumlari():
    """(konum, yarı genişlik) — AgizBolumu.DisleriKur ile aynı dizilim."""
    sonuc = []
    for yon in (1, -1):
        for z in (-8.5, -4.5, -0.5):
            sonuc.append((Vector((yon * A, -z, 0)), DISLER["Dis_Azi"]["W"]))
        for derece, tur in ((14, "Dis_KucukAzi"), (32, "Dis_KucukAzi"), (50, "Dis_Kopek"),
                            (66, "Dis_Kesici"), (82, "Dis_Kesici")):
            r = math.radians(derece)
            sonuc.append((Vector((yon * A * math.cos(r), -B * math.sin(r), 0)), DISLER[tur]["W"]))
    return sonuc


def dis_eti_yap():
    yol = cene_yolu()
    disler = dis_konumlari()
    uzunluk = [0.0]
    for i in range(1, len(yol)):
        uzunluk.append(uzunluk[-1] + (yol[i] - yol[i - 1]).length)
    IC, DIS, NO = -2.7, 3.8, 30

    def nokta(fu, fv):
        i = min(len(yol) - 1, int(round(fu * (len(yol) - 1))))
        p = yol[i]
        teget = (yol[min(i + 1, len(yol) - 1)] - yol[max(i - 1, 0)]).normalized()
        disari = Vector((teget.y, -teget.x, 0))
        if disari.dot(p - Vector((0, 4, 0))) < 0:
            disari = -disari
        o = lerp(IC, DIS, fv)

        # Diş aralarında yükselen diş eti (papilla), diş boynunda alçalan kenar
        yakin = min((p - k).length / w for k, w in disler)
        tepe = 0.42 + 0.36 * smooth(0.8, 1.2, yakin)
        if o < 0:
            h = lerp(tepe, -0.3, smooth(1.2, -IC, -o))
        else:
            h = tepe + 3.0 * smooth(1.4, DIS, o) ** 1.3
        konum = p + disari * o + Vector((0, 0, h))
        konum.z += 0.05 * noise.noise(konum * 0.9)
        return konum, (uzunluk[i] / 7.0, (o - IC) / 7.0)

    obj = izgara_nesnesi("DisEti", len(yol) - 1, NO, nokta, False)
    # Yüzeyin üstü görünür taraf olmalı
    if sum(p.normal.z for p in obj.data.polygons) < 0:
        obj.data.flip_normals()
    return obj


# ----------------------------------------------------------------- Bakteri kolonileri

def koloni_yap(ad, tohum):
    rnd = random.Random(tohum)
    mb = bpy.data.metaballs.new(ad + "_mb")
    mb.resolution = 0.06
    mb.threshold = 0.6
    gecici = bpy.data.objects.new(ad + "_mb", mb)
    bpy.context.scene.collection.objects.link(gecici)

    for _ in range(6):                               # biyofilm tabanı
        e = mb.elements.new(type="ELLIPSOID")
        e.co = (rnd.uniform(-0.6, 0.6), rnd.uniform(-0.5, 0.5), -0.42)
        e.radius = rnd.uniform(0.55, 0.85)
        e.size_x, e.size_y, e.size_z = 1.0, 1.0, 0.3

    for _ in range(4):                               # ikişerli bölünen kok zincirleri
        p = Vector((rnd.uniform(-0.6, 0.6), rnd.uniform(-0.5, 0.5), rnd.uniform(-0.25, 0.0)))
        aci, egim = rnd.uniform(0, 6.28), rnd.uniform(0.0, 0.5)
        for i in range(rnd.randint(6, 11)):
            e = mb.elements.new(type="BALL")
            e.co = p
            e.radius = rnd.uniform(0.3, 0.34)
            adim = 0.3 if i % 2 == 0 else 0.44
            aci += rnd.uniform(-0.5, 0.5)
            egim = max(-0.2, min(0.7, egim + rnd.uniform(-0.3, 0.3)))
            p = p + Vector((math.cos(aci), math.sin(aci), egim)) * adim

    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    mesh = bpy.data.meshes.new_from_object(gecici.evaluated_get(dg))
    mesh.name = ad
    for v in mesh.vertices:
        v.co += Vector(noise.noise_vector(v.co * 9.0)) * 0.012
    for yuz in mesh.polygons:
        yuz.use_smooth = True
    obj = bpy.data.objects.new(ad, mesh)
    bpy.context.scene.collection.objects.link(obj)
    bpy.data.objects.remove(gecici)
    bpy.data.metaballs.remove(mb)
    return obj


# ----------------------------------------------------------------- Önizleme

def onizleme_al(nesneler, yol):
    sahne = bpy.context.scene
    for i, obj in enumerate(nesneler):
        obj.location = ((i - (len(nesneler) - 1) / 2) * 3.6, 0, 0)
    bpy.ops.mesh.primitive_plane_add(size=60, location=(0, 0, -1.4))
    zemin = bpy.context.active_object
    isik_verisi = bpy.data.lights.new("Isik", "AREA")
    isik_verisi.energy, isik_verisi.size = 2500, 8
    isik = bpy.data.objects.new("Isik", isik_verisi)
    isik.location = (-5, -9, 9)
    isik.rotation_euler = (math.radians(50), 0, math.radians(-30))
    sahne.collection.objects.link(isik)
    kamera = bpy.data.objects.new("Kamera", bpy.data.cameras.new("Kamera"))
    kamera.location = (0, -30, 9)
    kamera.rotation_euler = (math.radians(74), 0, 0)
    kamera.data.lens = 50
    sahne.collection.objects.link(kamera)
    sahne.camera = kamera
    sahne.cycles.samples = 48
    sahne.render.resolution_x, sahne.render.resolution_y = 1280, 520
    sahne.render.filepath = yol
    bpy.ops.render.render(write_still=True)
    for obj in (zemin, isik, kamera):
        bpy.data.objects.remove(obj)
    for obj in nesneler:
        obj.location = (0, 0, 0)


# ----------------------------------------------------------------- Ana akış

sahne = sahne_hazirla()

disler = [dis_yap(ad) for ad in DISLER]
koloniler = [koloni_yap("Bakteri_%d" % (i + 1), 11 + i * 7) for i in range(3)]
bakteri_m, _, bakteri_bsdf = yeni_malzeme("Bakteri")
bakteri_bsdf.inputs["Base Color"].default_value = (0.62, 0.66, 0.3, 1)
bakteri_bsdf.inputs["Roughness"].default_value = 0.25
for k in koloniler:
    k.data.materials.append(bakteri_m)

if ONIZLEME:
    onizleme_al(disler + koloniler, ONIZLEME)

# Nesneler üst üste durursa birbirlerini gölgeler; pişirme sırasında ayır.
for i, obj in enumerate(disler + koloniler):
    obj.location = (i * 50.0, 0, 0)

for d in disler:
    kisa = d.name.lower()
    pisir(d, kisa + "_albedo", "DIFFUSE")
    pisir(d, kisa + "_normal", "NORMAL")
    pisir(d, kisa + "_ao", "AO", ornek=48)

doseme_pisir("dil", dil_malzemesi())
doseme_pisir("mukoza", mukoza_malzemesi())

for obj in disler + koloniler:
    obj.location = (0, 0, 0)
dis_eti = dis_eti_yap()

fbx_aktar(disler + koloniler + [dis_eti], "AgizModelleri.fbx")

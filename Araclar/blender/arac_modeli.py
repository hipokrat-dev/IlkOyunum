# Vücut Yolculuğu — Çırağın mini aracı (tüm bölümlerde kullanılır).
#
# Çalıştırma (proje kökünden):
#   /Applications/Blender.app/Contents/MacOS/Blender -b --python Araclar/blender/arac_modeli.py -- Assets/Resources/Ortak [onizleme.png]
#
# Üretilenler:
#   AracModeli.fbx : Arac_Govde, Arac_Cam, Arac_Lastik, Arac_Metal, Arac_Far (malzemeye göre ayrılmış parçalar)
#   arac_govde_albedo/_normal/_ao.png
#
# Eksenler: Blender'da Z yukarı, aracın önü -Y (Unity'de +Z). Araç zemine oturur (z = 0 zemin).
# Boyutlar AracKontrol'deki çarpıştırıcıyla uyumlu: uzunluk ~2.4, genişlik ~1.9 (paletlerle).

import bpy, math, os, sys
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import ortak
from ortak import *

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
CIKTI = os.path.abspath(argv[0] if argv else "Assets/Resources/Ortak")
ONIZLEME = os.path.abspath(argv[1]) if len(argv) > 1 else None
ortak.ayarla(CIKTI)
sahne = sahne_hazirla()

GOVDE_Z = 0.78


# ----------------------------------------------------------------- Parça yardımcıları

def ilkel(tur, ad, konum, olcek=(1, 1, 1), donus=(0, 0, 0), **secenek):
    getattr(bpy.ops.mesh, "primitive_%s_add" % tur)(location=konum, rotation=donus, **secenek)
    obj = bpy.context.active_object
    obj.scale = olcek
    obj.name = ad
    return obj


def birlestir(ad, nesneler):
    """Parçaları tek mesh yapar, dönüşümleri uygular, keskin kenarları açıya göre işaretler."""
    bpy.ops.object.select_all(action="DESELECT")
    for obj in nesneler:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = nesneler[0]
    if len(nesneler) > 1:
        bpy.ops.object.join()
    obj = bpy.context.active_object
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    obj.name = ad
    obj.data.name = ad
    obj.data.polygons.foreach_set("use_smooth", [True] * len(obj.data.polygons))
    obj.data.set_sharp_from_angle(angle=math.radians(38))
    return obj


def hizala(normal):
    """Yerel +Z eksenini verilen yöne çeviren Euler dönüşü."""
    return Vector((0, 0, 1)).rotation_difference(normal).to_euler()


# ----------------------------------------------------------------- Gövde

def govde_noktasi(fu, fv):
    u = fu * 2 * math.pi - math.pi
    a = fv * math.pi                                  # 0: burun, pi: kuyruk
    r = max(0.0, math.sin(a)) ** (0.6 if a < math.pi / 2 else 1.1)
    cx, cz = spow(math.cos(u), 0.8), spow(math.sin(u), 0.8)
    x = 0.62 * r * cx
    y = -1.2 * math.cos(a)
    z = GOVDE_Z + r * (0.47 if cz > 0 else 0.4) * cz + 0.06 * smooth(0.5, 1.0, fv)
    return Vector((x, y, z)), (fu, fv)


def govde_malzemesi():
    m, agac, bsdf = yeni_malzeme("AracGovde")
    koord = dugum(agac, "ShaderNodeTexCoord")
    ayir = dugum(agac, "ShaderNodeSeparateXYZ")
    bagla(agac, koord.outputs["Object"], ayir.inputs[0])
    X, Y, Z = ayir.outputs["X"], ayir.outputs["Y"], ayir.outputs["Z"]

    def cizgi(uzaklik, kalinlik=0.012):
        return aralik(agac, uzaklik, 0.0, kalinlik, 1.0, 0.0)

    def halka(eksen, aralik_boyu):                    # eksen boyunca eşit aralıklı panel çizgileri
        f = matematik(agac, "FRACT", matematik(agac, "ADD", matematik(agac, "DIVIDE", eksen, aralik_boyu), 0.5))
        uzak = matematik(agac, "MULTIPLY", matematik(agac, "ABSOLUTE", matematik(agac, "SUBTRACT", f, 0.5)), aralik_boyu)
        return cizgi(uzak)

    def serit(a, b):                                  # y ekseninde [a, b] aralığı
        return matematik(agac, "MULTIPLY", matematik(agac, "GREATER_THAN", Y, a), matematik(agac, "LESS_THAN", Y, b))

    # Boya: açık gri üst gövde, koyu karın, turuncu şeritler
    karin = aralik(agac, Z, 0.55, 0.61, 1.0, 0.0)
    renk = karistir(agac, "MIX", karin, (0.84, 0.85, 0.86), (0.13, 0.14, 0.16))
    turuncu = matematik(agac, "MAXIMUM", serit(0.3, 0.52), serit(0.58, 0.62))
    turuncu = matematik(agac, "MULTIPLY", turuncu, matematik(agac, "SUBTRACT", 1.0, karin))
    renk = karistir(agac, "MIX", turuncu, renk, (0.93, 0.36, 0.04))

    # Panel birleşim çizgileri
    dikis = matematik(agac, "MAXIMUM", halka(Y, 0.42),
                      matematik(agac, "MAXIMUM",
                                cizgi(matematik(agac, "ABSOLUTE", matematik(agac, "SUBTRACT", Z, 0.61))),
                                cizgi(matematik(agac, "ABSOLUTE", matematik(agac, "SUBTRACT", Z, 1.02)))))
    renk = karistir(agac, "MIX", matematik(agac, "MULTIPLY", dikis, 0.85), renk, (0.04, 0.04, 0.05))

    # Kullanılmışlık: kir ve soluk lekeler
    kir = dugum(agac, "ShaderNodeTexNoise")
    kir.inputs["Scale"].default_value = 3.5
    kir.inputs["Detail"].default_value = 7
    ton = rampa(agac, [(0.38, (0.72, 0.7, 0.68)), (0.68, (1, 1, 1))])
    bagla(agac, kir.outputs["Fac"], ton.inputs[0])
    renk = karistir(agac, "MULTIPLY", 1.0, renk, ton.outputs[0])
    bagla(agac, renk, bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.35

    puruz = dugum(agac, "ShaderNodeTexNoise")
    puruz.inputs["Scale"].default_value = 45
    yukseklik = matematik(agac, "SUBTRACT", matematik(agac, "MULTIPLY", puruz.outputs["Fac"], 0.06), dikis)
    kabart = dugum(agac, "ShaderNodeBump")
    kabart.inputs["Strength"].default_value = 0.6
    kabart.inputs["Distance"].default_value = 0.02
    bagla(agac, yukseklik, kabart.inputs["Height"])
    bagla(agac, kabart.outputs[0], bsdf.inputs["Normal"])
    return m


# ----------------------------------------------------------------- Paletler

def palet_yap(ad, x):
    L, R, W, KALIN, Z0 = 1.5, 0.21, 0.27, 0.08, 0.29
    cevre = 2 * L + 2 * math.pi * R

    def nokta(fu, fv):
        s = (fu % 1.0) * cevre                         # stadyum biçimli yol boyunca
        if s < L:                                      # alt düz kısım (önden arkaya)
            merkez, n = Vector((0, -L / 2 + s, Z0 - R)), Vector((0, 0, -1))
        elif s < L + math.pi * R:                      # arka yay
            aci = (s - L) / R
            n = Vector((0, math.sin(aci), -math.cos(aci)))
            merkez = Vector((0, L / 2, Z0)) + n * R
        elif s < 2 * L + math.pi * R:                  # üst düz kısım
            merkez, n = Vector((0, L / 2 - (s - L - math.pi * R), Z0 + R)), Vector((0, 0, 1))
        else:                                          # ön yay
            aci = (s - 2 * L - math.pi * R) / R
            n = Vector((0, -math.sin(aci), math.cos(aci)))
            merkez = Vector((0, -L / 2, Z0)) + n * R

        b = fv * 2 * math.pi                           # bant kesiti: yuvarlatılmış dikdörtgen
        yanal = (W / 2) * spow(math.cos(b), 0.4)
        radyal = (KALIN / 2) * spow(math.sin(b), 0.4)
        if math.sin(b) > 0.5:                          # dış yüzeyde tırtıllar
            radyal += 0.028 * smooth(-0.25, 0.25, math.sin(fu * 2 * math.pi * 34))
        return merkez + n * radyal + Vector((x + yanal, 0, 0)), (fu * 8, fv)

    return izgara_nesnesi(ad, 340, 16, nokta, True)


# ----------------------------------------------------------------- Kurulum

govde = izgara_nesnesi("Arac_Govde", 96, 64, govde_noktasi, True)
govde.data.materials.append(govde_malzemesi())

cam = [ilkel("uv_sphere", "Kubbe", (0, -0.3, 1.08), (0.4, 0.56, 0.34), segments=48, ring_count=24)]

lastik = [ilkel("uv_sphere", "Kabin", (0, -0.3, 1.02), (0.33, 0.47, 0.26), segments=32, ring_count=16),
          ilkel("cylinder", "Namlu", (0, -1.2, 0.6), donus=(math.radians(90), 0, 0), radius=0.085, depth=0.42, vertices=32)]
metal = [ilkel("uv_sphere", "NamluYuvasi", (0, -1.0, 0.6), (0.15, 0.17, 0.13), segments=24, ring_count=12),
         ilkel("torus", "NamluHalkasi", (0, -1.41, 0.6), donus=(math.radians(90), 0, 0),
               major_radius=0.09, minor_radius=0.024, major_segments=32, minor_segments=10),
         ilkel("cone", "Itici", (0, 1.22, 0.86), donus=(math.radians(-90), 0, 0),
               radius1=0.17, radius2=0.29, depth=0.34, vertices=40),
         ilkel("cube", "SirtKanadi", (0, 0.78, 1.2), (0.018, 0.26, 0.17)),
         ilkel("cylinder", "Anten", (0.2, 0.45, 1.42), radius=0.011, depth=0.5, vertices=10)]
far = [ilkel("uv_sphere", "IticiIsigi", (0, 1.2, 0.86), (0.14, 0.05, 0.14), segments=20, ring_count=10),
       ilkel("uv_sphere", "AntenUcu", (0.2, 0.45, 1.68), (0.028, 0.028, 0.028), segments=12, ring_count=8)]

for yon in (-1, 1):
    x = yon * 0.74
    lastik.append(palet_yap("Palet", x))
    lastik.append(ilkel("cube", "PaletGovdesi", (x, 0, 0.29), (0.1, 0.72, 0.13)))
    for y in (-0.75, 0.0, 0.75):
        metal.append(ilkel("cylinder", "Teker", (x, y, 0.29), donus=(0, math.radians(90), 0),
                           radius=0.165, depth=0.3, vertices=28))
    for y in (-0.42, 0.42):
        metal.append(ilkel("cube", "Destek", (yon * 0.6, y, 0.46), (0.14, 0.06, 0.045)))
    metal.append(ilkel("cube", "YanKanat", (yon * 0.42, 0.85, 0.9), (0.2, 0.2, 0.016)))

    # Farlar: burun yüzeyine oturur
    u = 0.22 if yon > 0 else math.pi - 0.22
    fu, fv = (u + math.pi) / (2 * math.pi), 0.17
    p, _ = govde_noktasi(fu, fv)
    komsu_u, _ = govde_noktasi(fu + 0.01, fv)
    komsu_v, _ = govde_noktasi(fu, fv + 0.01)
    normal = (komsu_u - p).cross(komsu_v - p).normalized()
    if normal.dot(p - Vector((0, 0, GOVDE_Z))) < 0:
        normal = -normal
    far.append(ilkel("uv_sphere", "FarCami", p + normal * 0.01, (0.1, 0.1, 0.05), hizala(normal), segments=24, ring_count=12))
    metal.append(ilkel("torus", "FarCercevesi", p + normal * 0.015, donus=hizala(normal),
                       major_radius=0.105, minor_radius=0.022, major_segments=28, minor_segments=10))

parcalar = [govde,
            birlestir("Arac_Cam", cam),
            birlestir("Arac_Lastik", lastik),
            birlestir("Arac_Metal", metal),
            birlestir("Arac_Far", far)]

if ONIZLEME:
    def duz_malzeme(ad, renk, puruz, metalik=0.0, saydam=0.0):
        m, _, bsdf = yeni_malzeme(ad)
        bsdf.inputs["Base Color"].default_value = (*renk, 1)
        bsdf.inputs["Roughness"].default_value = puruz
        bsdf.inputs["Metallic"].default_value = metalik
        bsdf.inputs["Transmission Weight"].default_value = saydam
        return m
    parcalar[1].data.materials.append(duz_malzeme("Cam", (0.7, 0.9, 0.95), 0.02, saydam=1.0))
    parcalar[2].data.materials.append(duz_malzeme("Lastik", (0.03, 0.03, 0.035), 0.6))
    parcalar[3].data.materials.append(duz_malzeme("Metal", (0.6, 0.62, 0.65), 0.25, 1.0))
    parcalar[4].data.materials.append(duz_malzeme("Far", (1, 0.95, 0.85), 0.1))

    bpy.ops.mesh.primitive_plane_add(size=40)
    zemin = bpy.context.active_object
    isik_verisi = bpy.data.lights.new("Isik", "AREA")
    isik_verisi.energy, isik_verisi.size = 900, 6
    isik = bpy.data.objects.new("Isik", isik_verisi)
    isik.location = (-3, -4, 5)
    isik.rotation_euler = (math.radians(40), 0, math.radians(-35))
    sahne.collection.objects.link(isik)
    kamera = bpy.data.objects.new("Kamera", bpy.data.cameras.new("Kamera"))
    kamera.location = (-3.6, -4.4, 2.5)
    kamera.rotation_euler = hizala(-(Vector((0, 0, 0.6)) - kamera.location).normalized())
    kamera.data.lens = 50
    sahne.collection.objects.link(kamera)
    sahne.camera = kamera
    dunya = bpy.data.worlds.new("Dunya")
    sahne.world = dunya
    dunya.use_nodes = True
    dunya.node_tree.nodes["Background"].inputs[0].default_value = (0.25, 0.27, 0.3, 1)
    sahne.cycles.samples = 64
    sahne.render.resolution_x, sahne.render.resolution_y = 1280, 720
    sahne.render.filepath = ONIZLEME
    bpy.ops.render.render(write_still=True)
    for obj in (zemin, isik, kamera):
        bpy.data.objects.remove(obj)

pisir(govde, "arac_govde_albedo", "DIFFUSE")
pisir(govde, "arac_govde_normal", "NORMAL")
pisir(govde, "arac_govde_ao", "AO", ornek=48)

fbx_aktar(parcalar, "AracModeli.fbx")

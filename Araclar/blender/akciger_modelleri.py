# Vücut Yolculuğu — Akciğer gezintisi modelleri ve dokuları.
#
# Çalıştırma (proje kökünden):
#   /Applications/Blender.app/Contents/MacOS/Blender -b --python Araclar/blender/akciger_modelleri.py -- Assets/Resources/Akciger [onizleme.png]
#
# Üretilenler:
#   AkcigerModelleri.fbx : AlveolKumesi_1..3 (üzüm salkımı gibi alveol kesecikleri + bronşiyol sapı)
#   alveol_albedo/_normal.png : kılcal damar ağıyla sarılı alveol duvarı (döşenebilir)

import bpy, bmesh, math, os, random, sys
from mathutils import Vector, Matrix, noise

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import ortak
from ortak import *

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
CIKTI = os.path.abspath(argv[0] if argv else "Assets/Resources/Akciger")
ONIZLEME = os.path.abspath(argv[1]) if len(argv) > 1 else None
ortak.ayarla(CIKTI)
sahne = sahne_hazirla()


def alveol_malzemesi():
    m, agac, bsdf = yeni_malzeme("Alveol")
    v, w = simit_koordinati(agac)

    # İncecik, yarı saydam pembe duvar
    bulut = doku4(agac, "ShaderNodeTexNoise", v, w, 1.2, noise_dimensions="4D")
    bulut.inputs["Detail"].default_value = 5
    ton = rampa(agac, [(0.3, (0.86, 0.56, 0.55)), (0.75, (0.97, 0.78, 0.74))])
    bagla(agac, bulut.outputs["Fac"], ton.inputs[0])

    # Duvarı ağ gibi saran kılcal damarlar: kalın ana damarlar + ince dallar
    def ag(olcek, kalinlik):
        hucre = doku4(agac, "ShaderNodeTexVoronoi", v, w, olcek, voronoi_dimensions="4D", feature="DISTANCE_TO_EDGE")
        return aralik(agac, hucre.outputs["Distance"], 0.0, kalinlik, 1.0, 0.0)

    kalin, ince = ag(1.5, 0.07), ag(3.6, 0.045)
    damar = matematik(agac, "MAXIMUM", kalin, matematik(agac, "MULTIPLY", ince, 0.75))
    renk = karistir(agac, "MIX", matematik(agac, "MULTIPLY", damar, 0.92), ton.outputs[0], (0.62, 0.06, 0.07))
    bagla(agac, renk, bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.3

    # Damarlar yüzeyden kabarık durur; aralarında ince hücre dokusu
    doku = doku4(agac, "ShaderNodeTexNoise", v, w, 9.0, noise_dimensions="4D")
    yukseklik = matematik(agac, "ADD", damar, matematik(agac, "MULTIPLY", doku.outputs["Fac"], 0.12))
    kabart = dugum(agac, "ShaderNodeBump")
    kabart.inputs["Strength"].default_value = 0.8
    kabart.inputs["Distance"].default_value = 0.08
    bagla(agac, yukseklik, kabart.inputs["Height"])
    bagla(agac, kabart.outputs[0], bsdf.inputs["Normal"])
    return m


def kume_yap(ad, tohum):
    """Bronşiyol sapının ucunda toplanmış alveol kesecikleri (her kesecik kendi UV'siyle)."""
    rnd = random.Random(tohum)
    bm = bmesh.new()
    bm.loops.layers.uv.new("UVMap")

    def kure_ekle(merkez, yaricap, ezme):
        once = set(bm.verts)
        bmesh.ops.create_uvsphere(bm, u_segments=28, v_segments=18, radius=1.0, calc_uvs=True)
        yeni = [v for v in bm.verts if v not in once]
        donus = Matrix.Rotation(rnd.uniform(0, 6.28), 4, Vector((rnd.random(), rnd.random(), rnd.random())).normalized())
        for v in yeni:
            p = Vector((v.co.x * yaricap, v.co.y * yaricap, v.co.z * yaricap * ezme))
            p += Vector(noise.noise_vector(p * 1.3 + merkez)) * 0.08 * yaricap      # hafif düzensizlik
            v.co = donus @ p + merkez

    # Kesecikler: sapın ucundaki bir çekirdeğin çevresinde, dışa doğru sıkışık dizilir
    for i in range(rnd.randint(22, 28)):
        yon = Vector((rnd.gauss(0, 1), rnd.gauss(0, 1), rnd.gauss(0, 1) + 0.5)).normalized()
        uzaklik = rnd.uniform(0.9, 1.9)
        kure_ekle(yon * uzaklik + Vector((0, 0, 1.6)), rnd.uniform(0.62, 0.95), rnd.uniform(0.85, 1.0))

    # Bronşiyol sapı (aşağı doğru incelen tüp)
    once = set(bm.verts)
    bmesh.ops.create_cone(bm, cap_ends=False, segments=20, radius1=0.42, radius2=0.3, depth=3.0, calc_uvs=True)
    for v in [v for v in bm.verts if v not in once]:
        v.co.z -= 1.2
        v.co.x += 0.25 * math.sin(v.co.z * 0.9)

    for yuz in bm.faces:
        yuz.smooth = True
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(ad)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(ad, mesh)
    sahne.collection.objects.link(obj)
    return obj


malzeme = alveol_malzemesi()
kumeler = [kume_yap("AlveolKumesi_%d" % (i + 1), 5 + i * 13) for i in range(3)]
for k in kumeler:
    k.data.materials.append(malzeme)

doseme_pisir("alveol", malzeme)

if ONIZLEME:
    # Önizlemede pişirilmiş dokuyu kullan (sahnedeki görünümle aynı)
    on, agac, bsdf = yeni_malzeme("AlveolOnizleme")
    resim = dugum(agac, "ShaderNodeTexImage", image=bpy.data.images.load(os.path.join(CIKTI, "alveol_albedo.png")))
    bagla(agac, resim.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.3
    bsdf.inputs["Subsurface Weight"].default_value = 0.3
    for i, k in enumerate(kumeler):
        k.data.materials.clear()
        k.data.materials.append(on)
        k.location = ((i - 1) * 6.5, 0, 0)
    isik_verisi = bpy.data.lights.new("Isik", "AREA")
    isik_verisi.energy, isik_verisi.size = 3000, 8
    isik = bpy.data.objects.new("Isik", isik_verisi)
    isik.location = (-6, -9, 8)
    isik.rotation_euler = (math.radians(55), 0, math.radians(-30))
    sahne.collection.objects.link(isik)
    kamera = bpy.data.objects.new("Kamera", bpy.data.cameras.new("Kamera"))
    kamera.location = (0, -19, 2.5)
    kamera.rotation_euler = (math.radians(88), 0, 0)
    sahne.collection.objects.link(kamera)
    sahne.camera = kamera
    dunya = bpy.data.worlds.new("Dunya")
    sahne.world = dunya
    dunya.use_nodes = True
    dunya.node_tree.nodes["Background"].inputs[0].default_value = (0.12, 0.05, 0.05, 1)
    sahne.cycles.samples = 48
    sahne.render.resolution_x, sahne.render.resolution_y = 1280, 560
    sahne.render.filepath = ONIZLEME
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(isik)
    bpy.data.objects.remove(kamera)
    for k in kumeler:
        k.location = (0, 0, 0)

fbx_aktar(kumeler, "AkcigerModelleri.fbx")

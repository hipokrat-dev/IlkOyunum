# Vücut Yolculuğu — Blender scriptlerinin ortak yardımcıları (mesh kurma, düğümler, doku pişirme, FBX).

import bpy, bmesh, math, os

CIKTI = "."
DOKU = 1024


def ayarla(cikti):
    global CIKTI
    CIKTI = os.path.abspath(cikti)
    os.makedirs(CIKTI, exist_ok=True)


def sahne_hazirla():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sahne = bpy.context.scene
    sahne.render.engine = "CYCLES"
    sahne.cycles.device = "CPU"
    return sahne


def spow(x, e):
    return math.copysign(abs(x) ** e, x)


def smooth(a, b, x):
    t = min(1.0, max(0.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)


def gauss(x, s):
    return math.exp(-x * x / s)


def lerp(a, b, t):
    return a + (b - a) * t


# ----------------------------------------------------------------- Mesh yardımcıları

def izgara_nesnesi(ad, nu, nv, nokta, kapali_u):
    """nokta(fu, fv) -> (Vector, (u, v)). Izgarayı kurar, çakışan noktaları kaynatır."""
    bm = bmesh.new()
    uv_kat = bm.loops.layers.uv.new("UVMap")
    satirlar, uvler = [], []
    for j in range(nv + 1):
        satir, uvs = [], []
        for i in range(nu + 1):
            p, uv = nokta(i / nu, j / nv)
            satir.append(bm.verts.new(p))
            uvs.append(uv)
        satirlar.append(satir)
        uvler.append(uvs)
    for j in range(nv):
        for i in range(nu):
            kose = [(i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1)]
            try:
                yuz = bm.faces.new([satirlar[b][a] for a, b in kose])
            except ValueError:
                continue
            for dongu, (a, b) in zip(yuz.loops, kose):
                dongu[uv_kat].uv = uvler[b][a]
            yuz.smooth = True
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    if kapali_u and bm.calc_volume(signed=True) < 0:      # normaller dışarı baksın
        bmesh.ops.reverse_faces(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(ad)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(ad, mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj


# ----------------------------------------------------------------- Düğüm yardımcıları

def dugum(agac, tur, **ozellik):
    n = agac.nodes.new(tur)
    for k, v in ozellik.items():
        setattr(n, k, v)
    return n


def bagla(agac, cikis, giris):
    agac.links.new(cikis, giris)


def yeni_malzeme(ad):
    m = bpy.data.materials.new(ad)
    m.use_nodes = True
    agac = m.node_tree
    bsdf = next(n for n in agac.nodes if n.type == "BSDF_PRINCIPLED")
    return m, agac, bsdf


def rampa(agac, duraklar):
    r = dugum(agac, "ShaderNodeValToRGB")
    el = r.color_ramp.elements
    el[0].position, el[0].color = duraklar[0][0], (*duraklar[0][1], 1)
    el[1].position, el[1].color = duraklar[-1][0], (*duraklar[-1][1], 1)
    for konum, renk in duraklar[1:-1]:
        e = el.new(konum)
        e.color = (*renk, 1)
    return r


def karistir(agac, tur, fac, a, b):
    """fac/a/b: soket ya da sabit değer. Sonuç soketini döndürür."""
    m = dugum(agac, "ShaderNodeMix", data_type="RGBA", blend_type=tur)
    for giris, deger in ((m.inputs[0], fac), (m.inputs[6], a), (m.inputs[7], b)):
        if isinstance(deger, bpy.types.NodeSocket):
            bagla(agac, deger, giris)
        elif isinstance(deger, (int, float)):
            giris.default_value = deger
        else:
            giris.default_value = (*deger, 1)
    return m.outputs[2]


def matematik(agac, islem, a, b=None):
    m = dugum(agac, "ShaderNodeMath", operation=islem)
    for giris, deger in ((m.inputs[0], a), (m.inputs[1], b)):
        if deger is None:
            continue
        if isinstance(deger, bpy.types.NodeSocket):
            bagla(agac, deger, giris)
        else:
            giris.default_value = deger
    return m.outputs[0]


def aralik(agac, deger, a, b, c=0.0, d=1.0):
    m = dugum(agac, "ShaderNodeMapRange")
    bagla(agac, deger, m.inputs[0])
    m.inputs[1].default_value, m.inputs[2].default_value = a, b
    m.inputs[3].default_value, m.inputs[4].default_value = c, d
    return m.outputs[0]


# ----------------------------------------------------------------- Doku pişirme ve dışa aktarma

def pisir(obj, dosya_adi, tur, kenar=8, ornek=8):
    malzeme = obj.data.materials[0]
    agac = malzeme.node_tree
    resim = bpy.data.images.new(dosya_adi, DOKU, DOKU, alpha=False)
    if tur != "DIFFUSE":
        resim.colorspace_settings.name = "Non-Color"
    hedef = dugum(agac, "ShaderNodeTexImage", image=resim)
    for n in agac.nodes:
        n.select = False
    hedef.select = True
    agac.nodes.active = hedef

    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.context.scene.cycles.samples = ornek
    if tur == "DIFFUSE":
        bpy.ops.object.bake(type="DIFFUSE", pass_filter={"COLOR"}, margin=kenar)
    else:
        bpy.ops.object.bake(type=tur, margin=kenar)

    resim.filepath_raw = os.path.join(CIKTI, dosya_adi + ".png")
    resim.file_format = "PNG"
    resim.save()
    agac.nodes.remove(hedef)
    print("PISIRILDI", dosya_adi)


def fbx_aktar(nesneler, dosya_adi):
    """Nesneleri (malzemesiz) Unity eksenleriyle FBX olarak yazar."""
    bpy.ops.object.select_all(action="DESELECT")
    for obj in nesneler:
        obj.data.materials.clear()
        obj.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(CIKTI, dosya_adi), use_selection=True, object_types={"MESH"},
        apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y", bake_space_transform=True,
        mesh_smooth_type="FACE", add_leaf_bones=False, bake_anim=False)
    print("TAMAMLANDI", os.path.join(CIKTI, dosya_adi))

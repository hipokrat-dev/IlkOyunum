import bpy, math, sys, os, json
from mathutils import Vector, noise
from pathlib import Path
ROOT=str(Path(__file__).resolve().parent)
if not (Path(ROOT)/'ortak.py').exists(): ROOT='/Users/gokhanyener/ilkoyunum/Araclar/blender'
sys.path.insert(0,ROOT)
import ortak
from ortak import *
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
OUT=os.path.abspath(args[0]) if args else str(Path(ROOT).parents[1]/'Assets/Resources/AgizV2')
ortak.ayarla(OUT)
ns={ '__file__':ROOT+'/agiz_modelleri.py' }
source=open(ROOT+'/agiz_modelleri.py').read().split('sahne = sahne_hazirla()')[0]
exec(source.replace('ortak.ayarla(CIKTI)',''),ns)
ortak.ayarla(OUT)
sahne_hazirla()
DIMS={'Azi':(1.6,1.5,2.6),'KucukAzi':(1.2,1.25,2.6),'Kopek':(1,1.05,3.5),'Kesici':(1.25,.8,3.1)}

def point(kind,u,v):
    W,D,H=DIMS[kind]; a=u*2*math.pi
    # Separate cervical wall and occlusal cap preserve a full, lobed crown.
    split=.61
    if v<.15:
        t=v/.15; r=.72*math.sin(t*math.pi*.5); z=-.85*(1-t)
    elif v<split:
        t=(v-.15)/(split-.15); r=.72+.24*math.sin(t*math.pi*.7);z=H*.73*t
    else:
        t=(v-split)/(1-split);r=(.72+.24*math.sin(math.pi*.7))*(1-t);z=H*.73
    e=.64 if kind in ('Azi','KucukAzi') else .7
    x=W*r*spow(math.cos(a),e);y=D*r*spow(math.sin(a),e)
    nx=x/W;ny=y/D
    if v>=split:
        if kind=='Azi':
            cusps=sum((.22 if sx==sy else .18)*math.exp(-((nx-sx*.49)**2+(ny-sy*.48)**2)/.15) for sx in (-1,1) for sy in (-1,1))
            groove=.055*math.exp(-((nx+.075*math.sin(ny*7))**2)/.012)+.045*math.exp(-((ny-.06*math.sin(nx*6))**2)/.012)
            z+=H*(.10*(1-(r/.914)**2)+cusps-groove)
        elif kind=='KucukAzi':
            cusps=.25*math.exp(-(nx*nx/.28+(ny-.45)**2/.10))+.19*math.exp(-(nx*nx/.28+(ny+.45)**2/.10))
            z+=H*(.05*(1-r/.914)+cusps-.035*math.exp(-ny*ny/.012))
        elif kind=='Kopek':
            z+=H*.28*(1-(r/.914)**1.25)
        else:
            z+=H*.25*(1-(r/.914)**3);x*=1+.12*(1-r/.914)
    if kind=='Kesici':
        f=smooth(.3,1,max(0,z/H));y*=1-.74*f
        z+=.025*math.cos(nx*math.pi*3)*smooth(.8,1,max(0,z/H))
    if kind=='Kopek' and y<0:y*=1+.11*math.exp(-nx*nx/.14)
    # Narrow developmental grooves along the crown, with small asymmetry.
    if kind in ('Azi','KucukAzi'):
        y*=1-.055*math.exp(-nx*nx/.025)*smooth(.2,.65,z/H)
    p=Vector((x,y,z));p+=Vector(noise.noise_vector(p*.9))*0.016
    return p,(u,v)

objs=[];stats={}
for kind in DIMS:
    near=izgara_nesnesi('Dis_'+kind+'_LOD0',48,32,lambda u,v,k=kind:point(k,u,v),True)
    bpy.context.view_layer.objects.active=near
    sub=near.modifiers.new('CrownPolish','SUBSURF');sub.levels=1;bpy.ops.object.modifier_apply(modifier=sub.name)
    dec=near.modifiers.new('MobileCrown','DECIMATE');dec.ratio=.28;bpy.ops.object.modifier_apply(modifier=dec.name)
    mat=ns['mine_malzemesi']('Dis_'+kind,DIMS[kind][2])
    near.data.materials.append(mat)
    for suffix,mode in [('albedo','DIFFUSE'),('normal','NORMAL'),('ao','AO')]:
        pisir(near,'dis_'+kind.lower()+'_'+suffix,mode,ornek=8 if mode!='AO' else 16)
    objs.append(near)
    for lod,nu,nv in [(1,24,16),(2,16,10)]:
        obj=bpy.data.objects.new('Dis_'+kind+'_LOD'+str(lod),near.data.copy());obj.data.name=obj.name;bpy.context.collection.objects.link(obj);bpy.context.view_layer.objects.active=obj
        dec=obj.modifiers.new('DistanceLOD','DECIMATE');dec.ratio=.25 if lod==1 else .09;bpy.ops.object.modifier_apply(modifier=dec.name);objs.append(obj)
    W,D,H=DIMS[kind]
    def cuff(u,v,W=W,D=D):
        a=u*2*math.pi;r=lerp(.75,1.19,v)
        z=(.54+.22*abs(math.cos(a))**4)*(1-smooth(.1,1,v))+.12*smooth(0,1,v)
        return Vector((W*r*spow(math.cos(a),.64),D*r*spow(math.sin(a),.64),z)),(u*2,v*.35)
    obj=izgara_nesnesi('Collet_'+kind,48,6,cuff,False)
    if sum(p.normal.z for p in obj.data.polygons)<0:obj.data.flip_normals()
    objs.append(obj)

# Matched, seamless physical texture sets (colour, normals, wetness), baked from Blender shaders.
def tissue(name,tongue):
    m,g,b=yeni_malzeme(name);vec,w=simit_koordinati(g)
    broad=doku4(g,'ShaderNodeTexNoise',vec,w,1.1,noise_dimensions='4D');broad.inputs['Detail'].default_value=3
    tone=rampa(g,[(.22,(.28,.038,.055)),(.52,(.46,.085,.105)),(.78,(.63,.19,.22))] if tongue else [(.2,(.26,.026,.048)),(.5,(.47,.075,.11)),(.8,(.65,.16,.2))])
    bagla(g,broad.outputs['Fac'],tone.inputs[0]);color=tone.outputs[0]
    if tongue:
        vor=doku4(g,'ShaderNodeTexVoronoi',vec,w,6.5,voronoi_dimensions='4D',feature='F1')
        bump=aralik(g,vor.outputs['Distance'],.18,.76,1,0)
        # Bright tips, red creases: irregular fungiform surface rather than white noise.
        color=karistir(g,'MIX',matematik(g,'MULTIPLY',bump,.24),color,(.65,.22,.24))
        fine=doku4(g,'ShaderNodeTexNoise',vec,w,28,noise_dimensions='4D')
        height=matematik(g,'ADD',bump,matematik(g,'MULTIPLY',fine.outputs['Fac'],.07))
        wet=aralik(g,vor.outputs['Distance'],.15,.7,.52,.88)
        strength=.7;distance=.10
    else:
        coarse=doku4(g,'ShaderNodeTexNoise',vec,w,3.2,noise_dimensions='4D');coarse.inputs['Detail'].default_value=4
        fine=doku4(g,'ShaderNodeTexNoise',vec,w,20,noise_dimensions='4D')
        height=matematik(g,'ADD',coarse.outputs['Fac'],matematik(g,'MULTIPLY',fine.outputs['Fac'],.16))
        wet=aralik(g,broad.outputs['Fac'],.2,.8,.62,.88)
        strength=.5;distance=.07
    bagla(g,color,b.inputs['Base Color']);b.inputs['Roughness'].default_value=.25
    bn=dugum(g,'ShaderNodeBump');bn.inputs['Strength'].default_value=strength;bn.inputs['Distance'].default_value=distance
    bagla(g,height,bn.inputs['Height']);bagla(g,bn.outputs[0],b.inputs['Normal'])
    bpy.ops.mesh.primitive_plane_add(size=2);plane=bpy.context.object;plane.data.materials.append(m)
    pisir(plane,name+'_albedo','DIFFUSE',kenar=0);pisir(plane,name+'_normal','NORMAL',kenar=0)
    output=next(n for n in g.nodes if n.type=='OUTPUT_MATERIAL');em=dugum(g,'ShaderNodeEmission');bagla(g,wet,em.inputs[0]);bagla(g,em.outputs[0],output.inputs['Surface'])
    pisir(plane,name+'_wet','EMIT',kenar=0)
    # URP metallic-smoothness packing: R=0 (organic dielectric), A=linear smoothness.
    im=bpy.data.images.get(name+'_wet');pix=list(im.pixels)
    for i in range(0,len(pix),4):val=pix[i];pix[i:i+4]=(0,0,0,val)
    packed=bpy.data.images.new(name+'_mask',im.size[0],im.size[1],alpha=True);packed.colorspace_settings.name='Non-Color';packed.pixels=pix
    packed.filepath_raw=OUT+'/'+name+'_mask.png';packed.file_format='PNG';packed.save()
    os.remove(OUT+'/'+name+'_wet.png');bpy.data.objects.remove(plane,do_unlink=True)

tissue('dil',True);tissue('mukoza',False)
for obj in objs:
    obj.data.calc_loop_triangles();stats[obj.name]=len(obj.data.loop_triangles)
bpy.ops.wm.save_as_mainfile(filepath=OUT+'/AgizV2.blend')
fbx_aktar(objs,'Disler.fbx')
open(OUT+'/mesh-budget.json','w').write(json.dumps(stats,indent=2))
print('MOUTH_V2_COMPLETE',json.dumps(stats))

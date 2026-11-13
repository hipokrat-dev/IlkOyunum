import bpy
from pathlib import Path
src=Path(__file__).resolve().parents[2]/'Assets/Resources'
out=src/'Mobil';out.mkdir(exist_ok=True)
for folder,name,ratio in [('Agiz','AgizModelleri',.25),('Ortak','AracModeli',.4),('Akciger','AkcigerModelleri',.3)]:
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 bpy.ops.import_scene.fbx(filepath=str(src/folder/(name+'.fbx')))
 bpy.ops.object.select_all(action='SELECT')
 bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
 before=after=0
 for obj in list(bpy.context.scene.objects):
  if obj.type!='MESH':continue
  before+=len(obj.data.polygons)
  bpy.context.view_layer.objects.active=obj
  mod=obj.modifiers.new('MobilOptimizasyon','DECIMATE');mod.ratio=ratio
  mod.use_collapse_triangulate=True
  bpy.ops.object.modifier_apply(modifier=mod.name)
  after+=len(obj.data.polygons)
 bpy.ops.object.select_all(action='SELECT')
 bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,object_types={'MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',bake_anim=False,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,mesh_smooth_type='FACE')
 print('MOBIL_MODEL',name,'poligon',before,'->',after,flush=True)

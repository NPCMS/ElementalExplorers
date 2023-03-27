import bpy
import json

jsonOutputDict = {
    "buildings": []
}

# function that returns a dictionary of instance transforms, runs on currently slected footprint without geometry nodes
# applied
def GenerateInstancesDictForFootprint():
    # Add geo nodes
    gnmod = obj.modifiers.new("Buildify", "NODES")

    # Add correct node group
    gnmod.node_group = bpy.data.node_groups['building']

    # Create a dictionary to hold the instance data
    instance_dict = {}

    depsgraph = bpy.context.evaluated_depsgraph_get()
    for inst in depsgraph.object_instances:
        if inst.is_instance:
            # have correctly found instance, check if it's in the dict
            if inst.object.name not in instance_dict:
                instance_dict[inst.object.name] = []

            positionX = inst.matrix_world.to_translation()[0]
            positionY = inst.matrix_world.to_translation()[2]
            positionZ = inst.matrix_world.to_translation()[1]

            rotationX = inst.matrix_world.to_euler()[0]
            rotationY = inst.matrix_world.to_euler()[2]
            rotationZ = inst.matrix_world.to_euler()[1]

            scaleX = inst.matrix_world.to_scale()[0]
            scaleY = inst.matrix_world.to_scale()[2]
            scaleZ = inst.matrix_world.to_scale()[1]

            instance_dict[inst.object.name].append({
                "position": [positionX, positionY, positionZ],
                "eulerAngles": [rotationX, rotationY, rotationZ],
                "scale": [scaleX, scaleY, scaleZ]
            })
    return instance_dict

# Read JSON file input
with open('C:/Users/uq20042/Documents/ElementalExplorers/Non_Unity/Blender/inputs/input.json', 'r') as file:
    data = json.load(file)

# Parse footprints
for fp in data['footprints']:
    verts = fp['verts']
    facesUnparsed = fp['faces']
    faces = []
    height = fp['height']
    levels = fp['levels']
    # faces must be parsed into tuples
    for i in range(0, len(facesUnparsed), 3):
        faces.append(tuple(facesUnparsed[i:i + 3]))

    # Create Mesh Datablock
    mesh = bpy.data.meshes.new("test")
    mesh.from_pydata(verts, [], faces)

    # Create Object and link to scene
    obj = bpy.data.objects.new("testobj", mesh)
    bpy.context.scene.collection.objects.link(obj)

    # select object
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)

    # get instances into dict
    instances = GenerateInstancesDictForFootprint()
    # get into correct format
    formattedInstances = {
        "prefabs": []
    }

    for key, value in instances.items():
        formattedInstances["prefabs"].append({
            "name": key,
            "transforms": value
        })

    # add dict to jsonOutputDict
    jsonOutputDict["buildings"].append(formattedInstances)

# save to file
with open("C:/Users/uq20042/Documents/ElementalExplorers/Non_Unity/Blender/outputs/output.json", "w") as f:
    json.dump(jsonOutputDict, f, indent=4)

#bpy.ops.export_scene.gltf(filepath="C:/Users/George.000/Desktop/My project/blenderTest/outputs/test.gltf", export_format="GLTF_EMBEDDED", check_existing=False, use_selection=True)

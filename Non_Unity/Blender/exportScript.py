import bpy
import os

# Define the directory where the exports will be saved
EXPORT_DIR = "C:/Users/George.000/Desktop/test"

def export_objects(collection, parent_path=""):
    # Get the path for the current collection
    collection_path = os.path.join(parent_path, collection.name) if parent_path else collection.name
    # Create the directory for the collection if it doesn't already exist
    os.makedirs(os.path.join(EXPORT_DIR, collection_path), exist_ok=True)
    
    # Export each object in the collection
    for obj in collection.objects:
        if obj.type == 'MESH':
            export_path = os.path.join(EXPORT_DIR, collection_path, obj.name + ".fbx")
            obj.select_set(True)
            bpy.ops.export_scene.fbx(filepath=export_path, use_selection=True, axis_up="Y",bake_space_transform=True )
            bpy.ops.object.select_all(action='DESELECT')

    # Recursively export objects in sub-collections
    for sub_collection in collection.children:
        export_objects(sub_collection, collection_path)

# Get the collection to export
collection_to_export = bpy.context.collection

# Export objects in the collection and its sub-collections
export_objects(collection_to_export)

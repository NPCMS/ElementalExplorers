using System;
using System.Collections.Generic;
using UnityEngine;

// max 1023 instances
// class adapted from https://toqoz.fyi/thousands-of-meshes.html
// instantiate this class then apply it to empty game object
public class DrawMeshInstancedDirect : MonoBehaviour
{
    [SerializeField] private int _population;
    [SerializeField] private float _range;
    [SerializeField] private Material _material;
    [SerializeField] private Matrix4x4[] _matrices;
    [SerializeField] private Mesh _mesh;
    [SerializeField] private List<Vector3> _position;
    [SerializeField] private List<Vector3> _rotation;
    [SerializeField] private List<Vector3> _scale;
    [SerializeField] private bool _initialised = false;

    public void Setup(int population, Material material, Mesh mesh, List<Vector3> position,
        List<Vector3> rotation, List<Vector3> scale)
    {
        _population = population;
        _material = material;
        _mesh = mesh;
        _position = position;
        _rotation = rotation;
        _scale = scale;
        _matrices = new Matrix4x4[_population];

        for (int i = 0; i < _population; i++)
        {
            // Build matrix.
            Quaternion quaternionRotation = Quaternion.Euler(_rotation[i]);

            Matrix4x4 mat = Matrix4x4.TRS(_position[i], quaternionRotation, _scale[i]);

            _matrices[i] = mat;
        }

        _initialised = true;
    }


    // must be called every frame
    private void Update()
    {
        if (_initialised)
            Graphics.DrawMeshInstanced(_mesh, 0, _material, _matrices, _population);
    }
}
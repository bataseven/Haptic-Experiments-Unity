using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshColliderUpdater : MonoBehaviour
{
    SkinnedMeshRenderer meshRenderer;
    MeshCollider meshCollider;
    // Start is called before the first frame update
    void Start()
    {
        meshRenderer = GetComponent<SkinnedMeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        UpdateCollider();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCollider();
    }
    
     public void UpdateCollider() {
         Mesh colliderMesh = new Mesh();
         meshRenderer.BakeMesh(colliderMesh);
         meshCollider.sharedMesh = null;
         meshCollider.sharedMesh = colliderMesh;
     }
}

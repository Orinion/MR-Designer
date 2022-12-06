using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR.Interaction.Toolkit;

public class ModelController : MonoBehaviour
{
    Mesh model;
    public MeshEditMode mode = MeshEditMode.Initial;
    XRSimpleInteractable interactable;

    List<Vector3> verticies = new();
    List<Triangle> triangles = new();

    public int vCount => verticies.Count;


    [SerializeField]
    private GameObject previewNodePrefab;
    [SerializeField]
    private TMP_Text InfoText;
    [SerializeField]
    private Color selectedColor;
    private Material selectedMaterial;
    private Material defaultMaterial;
    [SerializeField]
    private Material lineMaterial;

    private Transform currentInteractor;
    private GameObject previewNode;


    // markers for single nodes
    private Dictionary<int, GameObject> singleNodes = new();
    // selected nodes for triangle
    private List<int> selectedNodes = new();
    private List<LineRenderer> previewLines = new();


    public void SetMode(int newMode)
    {
        if (mode.Equals(MeshEditMode.Initial) && newMode != (int)MeshEditMode.DeleteNode) return;
        mode = (MeshEditMode)newMode;
    }

    public enum MeshEditMode
    {
        Initial,  // Less than 4 nodes, just add new nodes to create initial 3D Object
        AddNode,  // Adds node without connecting
        AddNodeAndFace, // Adds Node and add to closest Face
        AddTriangle, // Connect 3 Verticies to create a Triangle
        MoveNode, // Grab/Move closest Node
        DeleteNode,// Delete closest Node
    }

    public class Triangle
    {
        int index1, index2, index3;

        public Triangle(int i1, int i2, int i3)
        {
            Assert.IsTrue(i1 != i2 && i1 != i3 && i2 != i3, "Duplicate index!");
            index1 = i1;
            index2 = i2;
            index3 = i3;
        }

        public Triangle(int[] indicies)
        {
            index1 = indicies[0];
            index2 = indicies[1];
            index3 = indicies[2];

            Assert.IsTrue(index1 != index2 && index1 != index3 && index2 != index3, "Duplicate index!");
        }

        public int[] ToArray()
        {
            return new int[]{ index1, index2, index3 };
        }

        public int[] ToDoubleArray()
        {
            // Since only the front side of the triangle is drawn, return two triangles.
            return new int[] { index1, index2, index3, index1, index3,index2};
        }

        public bool Equals(Triangle other)
        {
            return other.Contains(index1) && other.Contains(index2) && other.Contains(index3);
        }

        public bool Contains(int index)
        {
            return index1 == index || index2 == index || index3 == index;
        }

        public void Fix(int index)
        {
            Assert.IsTrue(index1 != index && index2 != index && index3 != index, "Removed index!");
            if (index1 > index) index1 -= 1;
            if (index2 > index) index2 -= 1;
            if (index3 > index) index3 -= 1;
        }

    }

    private Vector3 getCenter(int[] indicies)
    {
        var vectors = indicies.Select(x => verticies[x]);
        Vector3 sum = Vector3.zero;
        foreach(var v in vectors)
            sum += v;
        return sum / indicies.Length;
    }

    private Vector3 getCenter(Triangle t)
    {
        return getCenter(t.ToArray());
    }

    private void UpdateMesh()
    {
        model.Clear();
        model.vertices = verticies.ToArray();
        model.triangles = triangles.SelectMany(x => x.ToDoubleArray()).ToArray(); // convert list of arrays to single array
    }

    private void AddTriangleInitial()
    {
        if (vCount < 3) return; // Not enough verticies to create triangles
        if (vCount == 3)
        {
            triangles = new List<Triangle>{ new Triangle (0, 1, 2) };
        }
        if (vCount == 4)
        {
            triangles = new List<Triangle> { new Triangle(0, 1, 2), new Triangle(0, 1, 3), new Triangle(0, 2, 3), new Triangle(1, 2, 3) };
        }
        if (vCount >= 4)
            mode = MeshEditMode.AddNode; // leave initial mode;
        UpdateMesh();
    }
    private int IndexOf(Vector3 position) => verticies.IndexOf(position);

    private int getClosest(Vector3 position)
    {
        int DistanceToPosition(Vector3 v, Vector3 v2)
        {
            if (v.Equals(v2))
                return 0;
            if ((v - position).sqrMagnitude > (v2 - position).sqrMagnitude)
                return 1;
            return -1;
        }
        var sotrtedByDistance = new List<Vector3>(verticies);
        sotrtedByDistance.Sort(DistanceToPosition);
        return IndexOf(sotrtedByDistance[0]);
    }

    private int[] getClosestTriangle(Vector3 position)
    {
        int DistanceToPosition(Triangle t, Triangle t2)
        {
            var v = getCenter(t);
            var v2 = getCenter(t2);

            if (v.Equals(v2))
                return 0;
            if ((v - position).sqrMagnitude > (v2 - position).sqrMagnitude)
                return 1;
            return -1;
        }
        var sotrtedByDistance = triangles.ToList();
        if (triangles.Count == 0)
            return new int[0];
        sotrtedByDistance.Sort(DistanceToPosition);
        return sotrtedByDistance[0].ToArray();
    }

    private int[] get3Closest(Vector3 position)
    {
        if (verticies.Count == 0)
            return new int[0];
        int DistanceToPosition(Vector3 v, Vector3 v2)
        {
            if (v.Equals(v2))
                return 0;
            if ((v - position).sqrMagnitude > (v2 - position).sqrMagnitude)
                return 1;
            return -1;
        }
        var sotrtedByDistance = new List<Vector3>(verticies);
        sotrtedByDistance.Sort(DistanceToPosition);
        var n = sotrtedByDistance[0].Equals(position) ? 1 : 0;
        var c = Mathf.Min(3, verticies.Count - n);
        return sotrtedByDistance.GetRange(n, c).Select(v => IndexOf(v)).ToArray();
    }

    public void AddNode(Vector3 position)
    {
        Assert.IsFalse(verticies.Contains(position), "Verticy already exists!");
        verticies.Add(position);

        var nodePrefab = Instantiate(previewNodePrefab, transform);
        nodePrefab.transform.position = position;
        singleNodes.Add(IndexOf(position),nodePrefab);

        UpdateMesh();
    }

    public void AddNodeAndConnect(Vector3 position)
    {
        AddNode(position);

        if (vCount <= 4)
        {
            AddTriangleInitial();
            return;
        }

        if(vCount > 4 && MeshEditMode.Initial == mode)
            mode = MeshEditMode.AddNode; // leave initial mode;
              
        var p = IndexOf(position);
        int[] closest3 = getClosestTriangle(position); // try to find the closest face
        if(closest3.Length==3)
            triangles.Remove(new Triangle(closest3[0], closest3[1], closest3[2])); // Remove
        else
            closest3 = get3Closest(position); // else get 3 closest verticies to position as index

        triangles.Add(new Triangle(p, closest3[0], closest3[1])); // Add triangles to the closest nodes
        triangles.Add(new Triangle(p, closest3[1], closest3[2]));
        triangles.Add(new Triangle(p, closest3[0], closest3[2]));

        UpdateMesh();
    }

    public void RemoveNode(int index)
    {
        if(singleNodes.ContainsKey(index))
        {
            Destroy(singleNodes[index]);
            singleNodes.Remove(index);

            foreach(int i in singleNodes.Keys.ToArray())
            {
                if (i < index)
                    continue;
                singleNodes[i - 1] = singleNodes[i];
                singleNodes.Remove(i);
            }

        }

        List<Triangle> triangles_old = triangles.FindAll(x => x.Contains(index));

        HashSet<int> neighbors = new();
        foreach (var tri in triangles_old)
            foreach (int i in tri.ToArray())
                neighbors.Add(i);

        foreach (var n1 in neighbors)
            foreach (var n2 in neighbors)
                foreach (var n3 in neighbors)
                {
                    if (n1 == index || n2 == index || n3 == index)
                        continue;
                    if (n1 == n2 || n1 == n3 || n2 == n3)
                        continue;
                    if (!triangles_old.Contains(new Triangle(n1, n2, index)) ||
                        !triangles_old.Contains(new Triangle(n2, n3, index)) ||
                        !triangles_old.Contains(new Triangle(n1, n3, index)))
                        continue;
                    triangles.Add(new Triangle(n1, n2, n3));
                }

        foreach (Triangle triangle in triangles_old)
            triangles.Remove(triangle);
        verticies.Remove(verticies[index]);
        foreach (var t in triangles)
            t.Fix(index);
        UpdateMesh();
    }

    public void SelectEnter(SelectEnterEventArgs args)
    {
        var interactor = args.interactorObject; 
        if (currentInteractor) return;
        currentInteractor = interactor.transform;

        switch (mode)
        {
            case MeshEditMode.Initial:
            case MeshEditMode.AddNode:
            case MeshEditMode.AddNodeAndFace:
            case MeshEditMode.DeleteNode:
                
                previewNode = Instantiate(previewNodePrefab, transform);
                break;
            case MeshEditMode.MoveNode:
                // get closest node+offset
                break;
            default:
                break;
        }
    }


    public void SelectExit(SelectExitEventArgs args)
    {
        var interactor = args.interactorObject;
        if (currentInteractor && currentInteractor.Equals(interactor.transform))
            currentInteractor = null;
        else
            return;

        var position = interactor.transform.position;


        switch (mode)
        {
            case MeshEditMode.Initial:
            case MeshEditMode.AddNodeAndFace:
                AddNodeAndConnect(position);
                break;
            case MeshEditMode.AddNode:
                AddNode(position);
                break;
            case MeshEditMode.DeleteNode:
                RemoveNode(getClosest(position));
                if (verticies.Count == 0)
                {
                    // Cancel Delete
                    mode = MeshEditMode.Initial;
                }
                break;
            case MeshEditMode.AddTriangle:
                if(selectedNodes.Count == 3)
                {
                    triangles.Add(new Triangle(selectedNodes.ToArray()));
                    UpdateMesh();
                }
                if (selectedNodes.Count == 4)
                {
                    triangles.Add(new Triangle(selectedNodes.GetRange(0,3).ToArray()));
                    triangles.Add(new Triangle(selectedNodes.GetRange(1,3).ToArray()));
                    //foreach (var node in selectedNodes)
                    //{
                    //    var others = selectedNodes.FindAll(n=> n != node);
                    //    triangles.Add(new Triangle(others.ToArray()));
                    //}
                    UpdateMesh();
                }
                foreach (var node in selectedNodes)
                    singleNodes[node].GetComponentInChildren<MeshRenderer>().sharedMaterial = defaultMaterial;                
                    break;
            default:
                break;
        }
        foreach (var l in previewLines)
            l.enabled = false;
        if (previewNode)
            Destroy(previewNode);
        selectedNodes.Clear();
    }

    public void ResetModel()
    {
        mode = MeshEditMode.Initial;
        currentInteractor = null;
        if (previewNode)
            Destroy(previewNode);
        foreach (var node in singleNodes.Values)
            Destroy(node);
        singleNodes.Clear();
        verticies = new List<Vector3>();
        triangles = new List<Triangle>();
        UpdateMesh();
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        InfoText.text = string.Format("M: {0} \n V: {1} T: {2} S: {3}", mode.ToString(), vCount, triangles.Count, selectedNodes.Count );

        foreach (var it in singleNodes)
            it.Value.transform.position = verticies[it.Key];

        
        if (currentInteractor)
        {
            var p = currentInteractor.position;
            
            if (mode == MeshEditMode.AddNodeAndFace || mode == MeshEditMode.Initial)
            {
                if (previewNode)
                    previewNode.transform.position = p;
                var c = get3Closest(p);
                if(c.Length > 0)
                {
                    for (int j = 0; j < c.Length; j++)
                    {
                        var lr = previewLines[j];
                        lr.enabled = true;
                        lr.SetPositions(new Vector3[] { p, verticies[c[j]] });
                    }
                }                       
            }
            if (mode == MeshEditMode.DeleteNode && previewNode)
            {
                var pd = verticies[getClosest(p)]; // get position of closest verticy
                previewNode.transform.position = pd;
            }
            if (mode == MeshEditMode.MoveNode)
            {
                int i = getClosest(p);
                var c = verticies[i];
                if ((p - c).magnitude < 0.05f)
                    verticies[i] = p;
                UpdateMesh();
            }
            if (mode == MeshEditMode.AddNode)
            {
                if (previewNode)
                    previewNode.transform.position = p;
            }
            if (mode == MeshEditMode.AddTriangle)
            {
                int i = getClosest(p);
                var c = verticies[i];
                if((p-c).magnitude < 0.03f)
                {
                    if (!selectedNodes.Contains(i) && selectedNodes.Count < 4)
                    {
                        selectedNodes.Add(i);
                        singleNodes[i].GetComponentInChildren<MeshRenderer>().sharedMaterial = selectedMaterial;
                    }
                }

                if(selectedNodes.Count>0)
                    for (int j = 0; j < selectedNodes.Count; j++)
                    {
                        var lr = previewLines[j];
                        lr.enabled = true;
                        //lr.transform.position = p;
                        //lr.transform.LookAt(verticies[selectedNodes[j]]);
                        lr.SetPositions(new Vector3[] { p, verticies[selectedNodes[j]] });                    
                    }
            }
        }
    }

    private void OnEnable()
    {
        model = GetComponent<MeshFilter>().mesh;

        defaultMaterial = previewNodePrefab.GetComponentInChildren<MeshRenderer>().sharedMaterial;
        selectedMaterial = new Material(defaultMaterial);
        selectedMaterial.color = selectedColor;

        for(int i=1;i<=4;i++)
        {
            var l = new GameObject("LineRenderer " + i);
            l.transform.parent = transform;
            var lr = l.AddComponent<LineRenderer>();
            lr.enabled = false;
            lr.widthMultiplier = 0.01f;
            lr.material = lineMaterial;
            previewLines.Add(lr);
        }

        interactable = GetComponent<XRSimpleInteractable>();
        interactable.selectEntered.AddListener(SelectEnter);
        interactable.selectExited.AddListener(SelectExit);

        // Load verticies and triangles from Mesh
        verticies = model.vertices.ToList();
        triangles = model.triangles
            .Select((elem, index) => new { elem, index })
            .GroupBy(x => x.index / 3) // create groups of 3
            .Select(x => new Triangle(
                          x.ElementAt(0).elem,
                          x.ElementAt(1).elem,
                          x.ElementAt(2).elem
            )).ToList();
        // Todo fix duplicate verticies

        if (vCount > 3)
            mode = MeshEditMode.AddNode; // leave initial mode;
    }

    private void OnDisable()
    {
        interactable.selectEntered.RemoveListener(SelectEnter);
        interactable.selectExited.RemoveListener(SelectExit);
    }

}

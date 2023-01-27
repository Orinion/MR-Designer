using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Controller that applies the interactions of the user to the model.
/// The interaction mode is selected using a menu that calls SetMode.
/// 
/// </summary>
public class ModelController : MonoBehaviour
{
    /// <summary>
    /// The mesh used to visualize the triangles of the model
    /// </summary>
    Mesh model;
    /// <summary>
    /// The current interaction mode set by the menu
    /// </summary>
    public MeshEditMode mode = MeshEditMode.Initial;
    /// <summary>
    /// The SimpleInteractable is used to detect interactions by the user
    /// </summary>
    XRSimpleInteractable interactable;

    /// <summary>
    /// List of verticies of the model
    /// </summary>
    List<Vector3> verticies = new();
    /// <summary>
    /// List of triangles of the model
    /// </summary>
    List<Triangle> triangles = new();

    /// <summary>
    /// Property for easy access to the verticy count
    /// </summary>
    public int vCount => verticies.Count;

    /// <summary>
    /// The maximum distance used for interactions like selecting nodes
    /// </summary>
    public float interactionDistance = 0.03f;

    /// <summary>
    /// Prefab used to visualize verticies, since the mesh renderer only visualizes triangles
    /// </summary>
    [SerializeField]
    private GameObject previewNodePrefab;

    /// <summary>
    /// Reference to the text field used to display the current status
    /// </summary>
    [SerializeField]
    private TMP_Text InfoText;

    /// <summary>
    /// Color used to visualize selected verticies
    /// </summary>
    [SerializeField]
    private Color selectedColor;
    /// <summary>
    /// Material generated using the selectedColor
    /// </summary>
    private Material selectedMaterial;
    /// <summary>
    /// Reference to the original material used for unselecting
    /// </summary>
    private Material defaultMaterial;
    /// <summary>
    /// Material used to draw lines to improve the visualization of verticies
    /// </summary>
    [SerializeField]
    private Material lineMaterial;

    /// <summary>
    /// Reference to the transform of the current interactor
    /// </summary>
    private Transform currentInteractor;
    /// <summary>
    /// Used to better visualize interactions, for example when selecting triangles for deletion
    /// </summary>
    private GameObject previewNode;


    /// <summary>
    /// markers for single verticies
    /// </summary>
    private Dictionary<int, GameObject> singleNodes = new();
    /// <summary>
    /// selected nodes for new triangles
    /// </summary>
    private List<int> selectedNodes = new();
    /// <summary>
    /// lines for highlighting selected verticies
    /// </summary>
    private List<LineRenderer> previewLines = new();

    /// <summary>
    /// Sets the interaction mode
    /// </summary>
    /// <param name="newMode"></param>
    public void SetMode(int newMode)
    {
        if (mode.Equals(MeshEditMode.Initial) && newMode != (int)MeshEditMode.DeleteNode) return;
        mode = (MeshEditMode)newMode;
    }

    /// <summary>
    /// The possible interaction modes
    /// </summary>
    public enum MeshEditMode
    {
        /// <summary>
        /// Used when there are less than 4 nodes, just adds new nodes to create initial 3D Object
        /// </summary>
        Initial,
        /// <summary>
        /// Simply adds verticies without connecting them
        /// </summary>
        AddNode,
        /// <summary>
        /// Adds new Verticy and connects to verticies of closest triangle
        /// </summary>
        AddNodeAndFace,
        /// <summary>
        /// Connect 3 Verticies to create a Triangle, or 4 to create 2 triangles
        /// </summary>
        AddTriangle,
        /// <summary>
        /// Grab and move closest verticy while held
        /// </summary>
        MoveNode,
        /// <summary>
        /// Delete closest verticy or triangle on release
        /// </summary>
        DeleteNode,
    }

    /// <summary>
    /// Class used to model a single triangle. 
    /// The verticies are stored as indicies, since it is also done by the Mesh class.
    /// </summary>
    public class Triangle
    {
        int index1, index2, index3;

        /// <summary>
        /// Create Triangle using 3 indicies of verticies
        /// </summary>
        public Triangle(int i1, int i2, int i3)
        {
            Assert.IsTrue(i1 != i2 && i1 != i3 && i2 != i3, "Duplicate index!");
            index1 = i1;
            index2 = i2;
            index3 = i3;
        }

        /// <summary>
        /// Create Triangle using an array of size 3
        /// </summary>
        /// <param name="indicies">The int array with 3 indicies</param>
        public Triangle(int[] indicies)
        {
            index1 = indicies[0];
            index2 = indicies[1];
            index3 = indicies[2];

            Assert.IsTrue(index1 != index2 && index1 != index3 && index2 != index3, "Duplicate index!");
        }

        /// <summary>
        /// Returns an array of indicies of the connected verticies
        /// </summary>
        /// <returns>Integer array with the 3 indicies</returns>
        public int[] ToArray()
        {
            return new int[]{ index1, index2, index3 };
        }


        /// <summary>
        /// For compatbility with renderers that render only one side, both possible orientations are returned
        /// </summary>
        /// <returns>An array of indicies containing two triangles</returns>
        public int[] ToDoubleArray()
        {
            return new int[] { index1, index2, index3, index1, index3,index2};
        }

        /// <summary>
        /// Helper to check if both connect the same verticies
        /// </summary>
        /// <param name="other">The triangle used for comparison</param>
        /// <returns>True if both connect the same three verticies</returns>
        public bool Equals(Triangle other)
        {
            return other.Contains(index1) && other.Contains(index2) && other.Contains(index3);
        }

        /// <summary>
        /// Check if the verticy is also connected by the triangle
        /// </summary>
        /// <param name="index">The index of the verticy</param>
        /// <returns>True if the verticy is connected by this triangle</returns>
        public bool Contains(int index)
        {
            return index1 == index || index2 == index || index3 == index;
        }

        /// <summary>
        /// Fixes the indicies of triangles with a higher index when a verticy is removed
        /// </summary>
        /// <param name="index">The index of the deleted vertecy</param>
        public void Fix(int index)
        {
            Assert.IsTrue(index1 != index && index2 != index && index3 != index, "Removed index!");
            if (index1 > index) index1 -= 1;
            if (index2 > index) index2 -= 1;
            if (index3 > index) index3 -= 1;
        }

    }

    /// <summary>
    /// Get Centerposition of list of verticies
    /// </summary>
    /// <param name="indicies">The indicies of verticies</param>
    /// <returns>The average position of the verticies</returns>
    private Vector3 getCenter(int[] indicies)
    {
        var vectors = indicies.Select(x => verticies[x]);
        Vector3 sum = Vector3.zero;
        foreach(var v in vectors)
            sum += v;
        return sum / indicies.Length;
    }

    /// <summary>
    /// Get Centerposition of a triangle
    /// </summary>
    /// <param name="t">The triangle</param>
    /// <returns>The center position of the triangle</returns>
    private Vector3 getCenter(Triangle t)
    {
        return getCenter(t.ToArray());
    }

    /// <summary>
    /// Apply the verticies and triangles to the mesh model
    /// </summary>
    private void UpdateMesh()
    {
        model.Clear();
        model.vertices = verticies.ToArray();
        model.triangles = triangles.SelectMany(x => x.ToDoubleArray()).ToArray(); // convert list of arrays to single array
    }

    /// <summary>
    /// In the initial mode, the verticies are automatically connected.
    /// Afterwards the AddNode mode is selected
    /// </summary>
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

    /// <summary>
    /// Finds the verticy at the position and returns its index
    /// </summary>
    /// <param name="position">The position of the verticy</param>
    /// <returns>The index of the verticy</returns>
    private int IndexOf(Vector3 position) => verticies.IndexOf(position);

    /// <summary>
    /// Finds the verticy closest to the specified position and returns its index
    /// </summary>
    /// <param name="position">The position used for computing the distance</param>
    /// <returns>The index of the closest verticy</returns>
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

    /// <summary>
    /// Finds the triangle whos center is closest to the specified position and returns it
    /// </summary>
    /// <param name="position">The position used for computing the distance</param>
    /// <returns>The triangle with the smallest distance from the center to the position</returns>
    private Triangle getClosestTriangle(Vector3 position)
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
            return null;
        sotrtedByDistance.Sort(DistanceToPosition);
        return sotrtedByDistance[0];
    }

    /// <summary>
    /// Finds and returns the 3 closest verticies of the position
    /// </summary>
    /// <param name="position">The position used for computing the distance</param>
    /// <returns>An array with indicies of the 3 closest verticies</returns>
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

    /// <summary>
    /// Add a verticy at the specified position to the model
    /// </summary>
    /// <param name="position">The position at which the verticy should be</param>
    public void AddNode(Vector3 position)
    {
        Assert.IsFalse(verticies.Contains(position), "Verticy already exists!");
        verticies.Add(position);

        var nodePrefab = Instantiate(previewNodePrefab, transform);
        nodePrefab.transform.position = position;
        singleNodes.Add(IndexOf(position),nodePrefab);

        UpdateMesh();
    }

    /// <summary>
    /// Add a verticy at the specified position and connect it to the closest triangle
    /// </summary>
    /// <param name="position">The position at which the verticy should be</param>
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
        var t = getClosestTriangle(position); // try to find the closest face
        int[] closest3 = null;
        if(t != null) 
        {
            triangles.Remove(t); // Remove
            closest3 = t.ToArray();
        }            
        else
            closest3 = get3Closest(position); // else get 3 closest verticies to position as index

        triangles.Add(new Triangle(p, closest3[0], closest3[1])); // Add triangles to the closest nodes
        triangles.Add(new Triangle(p, closest3[1], closest3[2]));
        triangles.Add(new Triangle(p, closest3[0], closest3[2]));

        UpdateMesh();
    }

    /// <summary>
    /// Remove the verticy with the specified index
    /// </summary>
    /// <param name="index">The index of the verticy</param>
    public void RemoveNode(int index)
    {
        // Remove node of the removed vertex
        if(singleNodes.ContainsKey(index))
        {
            Destroy(singleNodes[index]);
            singleNodes.Remove(index);

            // fix indicies of other nodes
            foreach(int i in singleNodes.Keys.ToArray())
            {
                if (i < index)
                    continue;
                singleNodes[i - 1] = singleNodes[i];
                singleNodes.Remove(i);
            }

        }

        List<Triangle> triangles_old = triangles.FindAll(x => x.Contains(index));
        // find neighbors of removed verticy
        HashSet<int> neighbors = new();
        foreach (var tri in triangles_old)
            foreach (int i in tri.ToArray())
                neighbors.Add(i);

        // try to reconect old neighbors of the verticy
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

        // remove triangles that connected the removed verticy
        foreach (Triangle triangle in triangles_old)
            triangles.Remove(triangle);

        verticies.Remove(verticies[index]);

        // fix indicies of the triangles
        foreach (var t in triangles)
            t.Fix(index);

        // apply to model
        UpdateMesh();
    }

    /// <summary>
    /// Event called when interaction starts.
    /// Depending on the mode a previewNode is created to visualize interactions
    /// </summary>
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

    /// <summary>
    /// Event called when an interaction ends.
    /// Depending on the mode changes to the model are done.
    /// Visualizations of interactions are disabled.
    /// </summary>
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
                var c = getClosest(position);
                var pc = (verticies[c]  - position).magnitude;
                var t = getClosestTriangle(position);
                if(t == null || pc < (getCenter(t) - position).magnitude) // Node closer than triangle?
                {
                    if (pc < interactionDistance*2)
                        RemoveNode(c);
                }                    
                else
                {
                    if ((getCenter(t) - position).magnitude < interactionDistance*2)
                        triangles.Remove(t);
                }
                UpdateMesh();

                if (verticies.Count == 0)
                {
                    // Cancel Delete Mode
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
                    triangles.Add(new Triangle(new int[] { selectedNodes[0], selectedNodes[2], selectedNodes[3] }));
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

    /// <summary>
    /// Reset the model to have no verticies and triangles
    /// </summary>
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


    /// <summary>
    /// Update is called once per frame.
    /// Updates the info text and active visualizations.
    /// </summary>
    void FixedUpdate()
    {
        //update text of menu
        InfoText.text = string.Format("M: {0} \n V: {1} T: {2} S: {3}", mode.ToString(), vCount, triangles.Count, selectedNodes.Count );
        
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
                var pt = getCenter(getClosestTriangle(p));// get position of closest triangle
                if (pt != null && (pt - p).magnitude < (pd - p).magnitude) // Node closer or triangle?
                    previewNode.transform.position = pt;
                else
                    previewNode.transform.position = pt;
            }
            if (mode == MeshEditMode.MoveNode)
            {
                int i = getClosest(p);
                var c = verticies[i];
                if ((p - c).magnitude < interactionDistance * 2)
                {
                    verticies[i] = p;
                    singleNodes[i].Value.transform.position = p;
                }

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
                if((p-c).magnitude < interactionDistance)
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

    /// <summary>
    /// Setup everyting on enable
    /// </summary>
    private void OnEnable()
    {
        model = GetComponent<MeshFilter>().mesh;

        // Setup materials for selecting
        defaultMaterial = previewNodePrefab.GetComponentInChildren<MeshRenderer>().sharedMaterial;
        selectedMaterial = new Material(defaultMaterial);
        selectedMaterial.color = selectedColor;

        // Create 4 LineRenederers
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

        // Add interaction Eventlisteners
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

    /// <summary>
    /// Remove everyting on disable
    /// </summary>
    private void OnDisable()
    {
        // Remove interaction eventlisteners
        interactable.selectEntered.RemoveListener(SelectEnter);
        interactable.selectExited.RemoveListener(SelectExit);

        // Delete Linerenderer
        foreach (var lr in previewLines)
            Destroy(lr);
        previewLines.Clear();

        // Delete previewNodes
        foreach (var node in singleNodes.Values)
            Destroy(node);
        singleNodes.Clear();

        // Delete Previewnode
        if (previewNode)
            Destroy(previewNode);
    }

}

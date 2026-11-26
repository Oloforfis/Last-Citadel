using UnityEngine;

[RequireComponent(typeof(Health))]
public class HealthBar : MonoBehaviour
{
    [Header("Внешний вид")]
    [SerializeField] private float width = 1.2f;
    [SerializeField] private float thickness = 0.12f;
    [SerializeField] private float offsetAboveCollider = 0.3f;
    [SerializeField] private Color fillColor = Color.green;

    private Health health;
    private BoxCollider box;
    private Camera cam;

    private Transform pivot;
    private Transform background;
    private Transform fill;
    private Material fillMaterial;

    void Awake()
    {
        health = GetComponent<Health>();
        cam = Camera.main;
        box = GetComponent<BoxCollider>();

        CreateVisual();
    }

    void CreateVisual()
    {
        pivot = new GameObject("HealthBar").transform;
        pivot.SetParent(transform, false);

        SetLayerRecursive(pivot.gameObject, LayerMask.NameToLayer("Ignore Raycast"));

        background = CreateQuad("Background", new Color(0f, 0f, 0f, 0.7f));
        background.localScale = new Vector3(width, thickness, 1f);

        fill = CreateQuad("Fill", fillColor);
        fillMaterial = fill.GetComponent<MeshRenderer>().material;
        UpdateFill(1f);
    }

    Transform CreateQuad(string name, Color color)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(quad.GetComponent<Collider>());

        quad.name = name;
        quad.transform.SetParent(pivot, false);
        quad.transform.localPosition = Vector3.zero;

        MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = color;
        renderer.material = mat;

        return quad.transform;
    }

    void Update()
    {
        if (pivot == null) return;

        PositionAboveCollider();
        FaceCamera();
        UpdateFillByHealth();
    }

    void PositionAboveCollider()
    {
        if (box != null)
        {
            Bounds bounds = box.bounds;
            pivot.position = new Vector3(
                bounds.center.x,
                bounds.max.y + offsetAboveCollider,
                bounds.center.z);
        }
        else
        {
            pivot.position = transform.position + Vector3.up * (1f + offsetAboveCollider);
        }
    }

    void FaceCamera()
    {
        if (cam != null)
            pivot.rotation = cam.transform.rotation;
        else
            cam = Camera.main;
    }

    void UpdateFillByHealth()
    {
        float ratio = health.max_count > 0f
            ? Mathf.Clamp01(health.current_count / health.max_count)
            : 1f;

        UpdateFill(ratio);
    }

    void UpdateFill(float ratio)
    {
        if (fill == null) return;

        float fillWidth = width * ratio;
        fill.localScale = new Vector3(fillWidth, thickness, 1f);

        fill.localPosition = new Vector3(-width * 0.5f + fillWidth * 0.5f, 0f, -0.01f);

        if (fillMaterial != null)
            fillMaterial.color = Color.Lerp(Color.red, fillColor, ratio);
    }

    void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}

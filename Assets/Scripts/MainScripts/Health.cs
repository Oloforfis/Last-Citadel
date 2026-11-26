using System;
using UnityEngine;
using UnityEngine.Serialization;
using static UnityEngine.GraphicsBuffer;

public class Health : MonoBehaviour
{
    public float max_count = 0f;
    [FormerlySerializedAs("current_count")]
    [SerializeField] private float _current_count = 0f;

    public float current_count
    {
        get => _current_count;
        set
        {
            _current_count = value;

            Debug.Log("У " + gameObject.name + " осталось " + _current_count + " здоровья.");

            if (_current_count <= 0f)
            {
                Debug.Log(gameObject.name + " уничтожен.");
                Destroy(gameObject);
            }
        }
    }

    void Awake()
    {
        if (GetComponent<HealthBar>() == null)
            gameObject.AddComponent<HealthBar>();
    }
}

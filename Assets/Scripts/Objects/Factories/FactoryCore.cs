using UnityEngine;

public class FactoryCore : MonoBehaviour
{
    [SerializeField]
    private FactoryDefinition definition;
    public FactoryDefinition Definition => definition;

    void Awake()
    {
        if (definition == null)
        {
            Debug.LogError($"FactoryCore: FactoryDefinition is not assigned in {gameObject.name}");
        }
    }
}
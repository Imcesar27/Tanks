using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RandomEnemySpawner : MonoBehaviour
{
    [Header("Visual Settings")]
    public Color enemyColor = Color.black;

    [Header("Spawn Settings")]
    public GameObject tankPrefab; // Tu prefab de tanque
    public int maxEnemies = 5;
    public float spawnInterval = 10f;
    public float minDistanceFromPlayers = 15f; // Distancia mínima de los jugadores
    public int maxSpawnAttempts = 50; // Intentos máximos para encontrar posición válida
    
    [Header("Map Boundaries")]
    public Vector3 mapCenter = Vector3.zero;
    public Vector3 mapSize = new Vector3(50f, 0f, 50f); // Tamaño del mapa (X, Y, Z)
    
    [Header("Terrain Settings")]
    public LayerMask groundLayer = 1; // Capa del suelo
    public LayerMask obstacleLayer = 0; // Capas de obstáculos a evitar
    public float groundCheckDistance = 10f;
    public float obstacleCheckRadius = 3f;
    
    [Header("AI Settings")]
    public float aiDetectionRange = 15f;
    public float aiAttackRange = 10f;
    public float aiFireRate = 1f;
    public float aiPatrolRadius = 10f;
    public float aiMoveSpeed = 5f;
    
    [Header("Debug")]
    public bool showSpawnArea = true;
    public Color spawnAreaColor = Color.green;
    
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private GameManager gameManager; // Referencia al GameManager
    
    void Start()
    {
        // Buscar el GameManager
        gameManager = FindObjectOfType<GameManager>();
        
        // Iniciar el spawn de enemigos
        StartCoroutine(SpawnEnemiesCoroutine());
    }
    
    IEnumerator SpawnEnemiesCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            
            // Limpiar enemigos destruidos de la lista
            CleanupDestroyedEnemies();
            
            // Spawnear nuevo enemigo si no hemos alcanzado el máximo
            if (spawnedEnemies.Count < maxEnemies)
            {
                SpawnRandomEnemy();
            }
        }
    }
    
    void SpawnRandomEnemy()
    {
        Vector3 spawnPosition = FindValidSpawnPosition();
        
        if (spawnPosition != Vector3.zero)
        {
            GameObject enemy = CreateEnemyTank(spawnPosition);
            if (enemy != null)
            {
                spawnedEnemies.Add(enemy);
                Debug.Log($"Enemy spawned at position: {spawnPosition}");
            }
        }
        else
        {
            Debug.LogWarning("No se pudo encontrar una posición válida para spawn después de múltiples intentos");
        }
    }
    
    Vector3 FindValidSpawnPosition()
    {
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            // Generar posición aleatoria dentro de los límites del mapa
            Vector3 randomPosition = GenerateRandomPosition();
            
            // Verificar si la posición es válida
            if (IsValidSpawnPosition(randomPosition))
            {
                return randomPosition;
            }
        }
        
        return Vector3.zero; // No se encontró posición válida
    }
    
    Vector3 GenerateRandomPosition()
    {
        float randomX = Random.Range(mapCenter.x - mapSize.x / 2f, mapCenter.x + mapSize.x / 2f);
        float randomZ = Random.Range(mapCenter.z - mapSize.z / 2f, mapCenter.z + mapSize.z / 2f);
        
        // Y será determinado por el raycast al suelo
        Vector3 randomPos = new Vector3(randomX, mapCenter.y + groundCheckDistance, randomZ);
        
        return randomPos;
    }
    
    bool IsValidSpawnPosition(Vector3 position)
    {
        // 1. Verificar que hay suelo debajo
        if (!HasGroundBelow(position))
            return false;
        
        // 2. Verificar que no hay obstáculos
        if (HasObstaclesNearby(position))
            return false;
        
        // 3. Verificar distancia mínima de los jugadores
        if (!IsAwayFromPlayers(position))
            return false;
        
        // 4. Verificar distancia de otros enemigos
        if (!IsAwayFromOtherEnemies(position))
            return false;
        
        return true;
    }
    
    bool HasGroundBelow(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position, Vector3.down, out hit, groundCheckDistance, groundLayer))
        {
            return true;
        }
        return false;
    }
    
    bool HasObstaclesNearby(Vector3 position)
    {
        // Ajustar Y al nivel del suelo
        RaycastHit hit;
        if (Physics.Raycast(position, Vector3.down, out hit, groundCheckDistance, groundLayer))
        {
            position.y = hit.point.y + 1f; // Un poco arriba del suelo
        }
        
        // Verificar obstáculos en un radio
        Collider[] obstacles = Physics.OverlapSphere(position, obstacleCheckRadius, obstacleLayer);
        return obstacles.Length > 0;
    }
    
    bool IsAwayFromPlayers(Vector3 position)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        
        foreach (GameObject player in players)
        {
            float distance = Vector3.Distance(position, player.transform.position);
            if (distance < minDistanceFromPlayers)
            {
                return false;
            }
        }
        
        return true;
    }
    
    bool IsAwayFromOtherEnemies(Vector3 position)
    {
        float minDistanceBetweenEnemies = 8f; // Distancia mínima entre enemigos
        
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                float distance = Vector3.Distance(position, enemy.transform.position);
                if (distance < minDistanceBetweenEnemies)
                {
                    return false;
                }
            }
        }
        
        return true;
    }
    
    GameObject CreateEnemyTank(Vector3 spawnPosition)
    {
        // Ajustar Y al nivel del suelo
        RaycastHit hit;
        if (Physics.Raycast(spawnPosition, Vector3.down, out hit, groundCheckDistance, groundLayer))
        {
            spawnPosition.y = hit.point.y;
        }
        
        // Crear rotación aleatoria
        Quaternion randomRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        
        // Instanciar el tanque
        GameObject enemyTank = Instantiate(tankPrefab, spawnPosition, randomRotation);
        
        // Configurar como enemigo
        SetupEnemyTank(enemyTank);
        
        return enemyTank;
    }
    
    void SetupEnemyTank(GameObject tank)
    {
        // Cambiar tag
        
        // Desactivar controles de jugador COMPLETAMENTE
        TankMovement movement = tank.GetComponent<TankMovement>();
        if (movement != null)
        {
            // Asignar un número de jugador inválido para evitar input
            movement.m_PlayerNumber = -1;
            movement.enabled = false;
        }
        
        TankShooting shooting = tank.GetComponent<TankShooting>();
        if (shooting != null)
        {
            // Asignar un número de jugador inválido para evitar input
            shooting.m_PlayerNumber = -1;
            shooting.enabled = false;
        }
        
        // Agregar IA
        TankAI aiComponent = tank.AddComponent<TankAI>();
        
        // Configurar IA con valores aleatorios para variedad
        aiComponent.detectionRange = Random.Range(aiDetectionRange * 0.8f, aiDetectionRange * 1.2f);
        aiComponent.attackRange = Random.Range(aiAttackRange * 0.8f, aiAttackRange * 1.2f);
        aiComponent.fireRate = Random.Range(aiFireRate * 0.7f, aiFireRate * 1.3f);
        aiComponent.patrolRadius = Random.Range(aiPatrolRadius * 0.7f, aiPatrolRadius * 1.3f);
        aiComponent.moveSpeed = Random.Range(aiMoveSpeed * 0.8f, aiMoveSpeed * 1.2f);
        
        // Cambiar color a rojo para enemigos
        MeshRenderer[] renderers = tank.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            renderer.material.color = Color.black;
        }
        
        // Desactivar canvas del UI si existe
        Canvas canvas = tank.GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            canvas.gameObject.SetActive(false);
        }
        
        // Asegurar que no responda a input del jugador
        MonoBehaviour[] allComponents = tank.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in allComponents)
        {
            // Mantener solo la IA y componentes esenciales
            if (component is TankMovement || component is TankShooting)
            {
                component.enabled = false;
            }
        }
    }
    
    void CleanupDestroyedEnemies()
    {
        // Remover enemigos que ya no existen de la lista
        spawnedEnemies.RemoveAll(enemy => enemy == null);
    }
    
    // Métodos públicos para control externo
    public void SetMaxEnemies(int max)
    {
        maxEnemies = max;
    }
    
    public void SetSpawnInterval(float interval)
    {
        spawnInterval = interval;
    }
    
    public void SpawnEnemyNow()
    {
        if (spawnedEnemies.Count < maxEnemies)
        {
            SpawnRandomEnemy();
        }
    }
    
    public void ClearAllEnemies()
    {
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        spawnedEnemies.Clear();
    }
    
    public int GetEnemyCount()
    {
        CleanupDestroyedEnemies();
        return spawnedEnemies.Count;
    }
    
    // Configuración de IA
    public void SetAIParameters(float detectionRange, float attackRange, float fireRate, float patrolRadius, float moveSpeed)
    {
        aiDetectionRange = detectionRange;
        aiAttackRange = attackRange;
        aiFireRate = fireRate;
        aiPatrolRadius = patrolRadius;
        aiMoveSpeed = moveSpeed;
    }
    
    // Visualización en el editor
    void OnDrawGizmos()
    {
        if (showSpawnArea)
        {
            Gizmos.color = spawnAreaColor;
            Gizmos.DrawWireCube(mapCenter, mapSize);
            
            // Mostrar área de exclusión alrededor de jugadores
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            Gizmos.color = Color.black;
            foreach (GameObject player in players)
            {
                if (player != null)
                {
                    Gizmos.DrawWireSphere(player.transform.position, minDistanceFromPlayers);
                }
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Mostrar información detallada cuando está seleccionado
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(mapCenter, mapSize);
        
        // Mostrar posiciones de enemigos actuales
        Gizmos.color = Color.red;
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                Gizmos.DrawWireSphere(enemy.transform.position, obstacleCheckRadius);
            }
        }
    }
}
using UnityEngine;
using System.Collections;

public class TankAI : MonoBehaviour
{
    [Header("AI Settings")]
    public float detectionRange = 15f;
    public float attackRange = 10f;
    public float fireRate = 1f;
    public float patrolRadius = 10f;
    public float moveSpeed = 5f;
    public float rotationSpeed = 50f;
    
    [Header("AI Behavior")]
    public float patrolWaitTime = 2f;
    public float stuckCheckTime = 3f;
    public float minMoveDistance = 1f;
    
    private Transform target;
    private Vector3 patrolCenter;
    private Vector3 currentPatrolTarget;
    private float lastFireTime;
    private float lastStuckCheckTime;
    private Vector3 lastPosition;
    
    // Estados de la IA
    private enum AIState
    {
        Patrolling,
        Chasing,
        Attacking
    }
    
    private AIState currentState = AIState.Patrolling;
    
    // Referencias a componentes
    private TankMovement tankMovement;
    private TankShooting tankShooting;
    private Rigidbody tankRigidbody;
    
    void Start()
    {
        // Guardar posición inicial como centro de patrulla
        patrolCenter = transform.position;
        
        // Obtener referencias a componentes
        tankMovement = GetComponent<TankMovement>();
        tankShooting = GetComponent<TankShooting>();
        tankRigidbody = GetComponent<Rigidbody>();
        
        // Desactivar controles del jugador
        if (tankMovement != null)
        {
            tankMovement.enabled = false;
        }
        
        // Configurar el sistema de disparo para IA
        if (tankShooting != null)
        {
            tankShooting.enabled = false; // Desactivar el script original
        }
        
        // Inicializar posición para detectar si está atascado
        lastPosition = transform.position;
        lastStuckCheckTime = Time.time;
        
        // Comenzar patrulla
        SetNewPatrolTarget();
    }
    
    void Update()
    {
        // Buscar el jugador más cercano
        FindNearestPlayer();
        
        // Actualizar comportamiento según el estado
        switch (currentState)
        {
            case AIState.Patrolling:
                Patrol();
                break;
            case AIState.Chasing:
                ChaseTarget();
                break;
            case AIState.Attacking:
                AttackTarget();
                break;
        }
        
        // Verificar si está atascado
        CheckIfStuck();
    }
    
    void FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float nearestDistance = Mathf.Infinity;
        Transform nearestPlayer = null;
        
        foreach (GameObject player in players)
        {
            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestPlayer = player.transform;
                }
            }
        }
        
        // Cambiar estado según la distancia al jugador
        if (nearestPlayer != null)
        {
            target = nearestPlayer;
            
            if (nearestDistance <= attackRange)
            {
                currentState = AIState.Attacking;
            }
            else if (nearestDistance <= detectionRange)
            {
                currentState = AIState.Chasing;
            }
            else
            {
                currentState = AIState.Patrolling;
                target = null;
            }
        }
        else
        {
            currentState = AIState.Patrolling;
            target = null;
        }
    }
    
    void Patrol()
    {
        // Moverse hacia el objetivo de patrulla
        MoveTowards(currentPatrolTarget);
        
        // Si llegó al objetivo, esperar y elegir nuevo objetivo
        if (Vector3.Distance(transform.position, currentPatrolTarget) < 2f)
        {
            StartCoroutine(WaitAndSetNewPatrolTarget());
        }
    }
    
    void ChaseTarget()
    {
        if (target != null)
        {
            MoveTowards(target.position);
        }
    }
    
    void AttackTarget()
    {
        if (target != null)
        {
            // Apuntar al objetivo
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
            
            // Disparar si es momento
            if (Time.time >= lastFireTime + (1f / fireRate))
            {
                Fire();
                lastFireTime = Time.time;
            }
            
            // Mantener distancia (no acercarse demasiado)
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            if (distanceToTarget > attackRange * 0.7f)
            {
                MoveTowards(target.position);
            }
        }
    }
    
    void MoveTowards(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        
        // Rotar hacia el objetivo
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }
        
        // Mover hacia adelante
        if (tankRigidbody != null)
        {
            tankRigidbody.MovePosition(transform.position + transform.forward * moveSpeed * Time.deltaTime);
        }
        else
        {
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        }
    }
    
void Fire()
{
    // Buscar el componente de disparo
    TankShooting shootingComponent = GetComponent<TankShooting>();
    if (shootingComponent != null && shootingComponent.m_Shell != null && shootingComponent.m_FireTransform != null)
    {
        // Crear proyectil - m_Shell es un Rigidbody, necesitamos su gameObject
        Rigidbody shellInstance = Instantiate(shootingComponent.m_Shell, 
                                            shootingComponent.m_FireTransform.position, 
                                            shootingComponent.m_FireTransform.rotation);
        
        // Configurar velocidad del proyectil
        if (shellInstance != null)
        {
            // Usar una fuerza por defecto si no existe m_LaunchForce
            float launchForce = 15f; // Ajusta este valor según necesites
            
            // Si tu TankShooting tiene un campo diferente para la fuerza, cámbialo aquí
            // Por ejemplo: launchForce = shootingComponent.LaunchForce;
            
            shellInstance.linearVelocity = launchForce * shootingComponent.m_FireTransform.forward;
        }
        
        // Efectos de sonido si existen
        AudioSource shootingAudio = shootingComponent.m_ShootingAudio;
        AudioClip fireClip = shootingComponent.m_FireClip;
        
        if (shootingAudio != null && fireClip != null)
        {
            shootingAudio.clip = fireClip;
            shootingAudio.Play();
        }
    }
}
    
    void SetNewPatrolTarget()
    {
        // Generar punto aleatorio dentro del radio de patrulla
        Vector2 randomPoint = Random.insideUnitCircle * patrolRadius;
        currentPatrolTarget = patrolCenter + new Vector3(randomPoint.x, 0, randomPoint.y);
        
        // Asegurar que el punto esté en el suelo
        RaycastHit hit;
        if (Physics.Raycast(currentPatrolTarget + Vector3.up * 10f, Vector3.down, out hit, 20f))
        {
            currentPatrolTarget.y = hit.point.y;
        }
    }
    
    IEnumerator WaitAndSetNewPatrolTarget()
    {
        yield return new WaitForSeconds(patrolWaitTime);
        SetNewPatrolTarget();
    }
    
    void CheckIfStuck()
    {
        // Verificar si el tanque se ha movido lo suficiente
        if (Time.time >= lastStuckCheckTime + stuckCheckTime)
        {
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);
            
            if (distanceMoved < minMoveDistance && currentState != AIState.Attacking)
            {
                // Está atascado, cambiar objetivo
                if (currentState == AIState.Patrolling)
                {
                    SetNewPatrolTarget();
                }
                
                // Rotar aleatoriamente para intentar desatascarse
                transform.Rotate(0, Random.Range(-90f, 90f), 0);
            }
            
            lastPosition = transform.position;
            lastStuckCheckTime = Time.time;
        }
    }
    
    // Métodos para ajustar parámetros desde otros scripts
    public void SetDetectionRange(float range)
    {
        detectionRange = range;
    }
    
    public void SetAttackRange(float range)
    {
        attackRange = range;
    }
    
    public void SetFireRate(float rate)
    {
        fireRate = rate;
    }
    
    public void SetPatrolRadius(float radius)
    {
        patrolRadius = radius;
    }
    
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }
    
    // Visualización en el editor
    void OnDrawGizmosSelected()
    {
        // Mostrar rango de detección
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Mostrar rango de ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Mostrar radio de patrulla
        Gizmos.color = Color.green;
        Vector3 center = Application.isPlaying ? patrolCenter : transform.position;
        Gizmos.DrawWireSphere(center, patrolRadius);
        
        // Mostrar objetivo actual de patrulla
        if (Application.isPlaying && currentState == AIState.Patrolling)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(currentPatrolTarget, 1f);
            Gizmos.DrawLine(transform.position, currentPatrolTarget);
        }
    }
}
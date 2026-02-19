using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Configuración del Juego")]
    public int m_NumRoundsToWin = 5; // Número de rondas que un jugador debe ganar para ganar el juego
    public int m_MaxLosses = 5; // Número máximo de derrotas permitidas
    public float m_StartDelay = 3f; // Delay entre las fases de RoundStarting y RoundPlaying
    public float m_EndDelay = 3f; // Delay entre las fases de RoundPlaying y RoundEnding
    public float m_MaxGameTime = 240f; // Tiempo máximo del juego en segundos (4 minutos)
    
    [Header("Referencias UI")]
    public Text m_MessageText; // Referencia al texto para mostrar mensajes
    public Text m_TimerText; // Referencia al texto del temporizador
    
    [Header("Componentes del Juego")]
    public CameraControl m_CameraControl; // Referencia al script de CameraControl
    public GameObject m_TankPrefab; // Referencia al Prefab del Tanque
    public TankManager[] m_Tanks; // Array de TankManagers para controlar cada tanque
    
    // Variables privadas
    private int m_RoundNumber; // Número de ronda
    private WaitForSeconds m_StartWait; // Delay hasta que la ronda empieza
    private WaitForSeconds m_EndWait; // Delay hasta que la ronda acaba
    private TankManager m_RoundWinner; // Referencia al ganador de la ronda
    private TankManager m_GameWinner; // Referencia al ganador del juego
    private bool m_GameEnded = false; // Control si el juego ha terminado
    private float m_GameStartTime; // Tiempo de inicio del juego
    private float m_CurrentGameTime; // Tiempo actual del juego
    private bool m_TimeUp = false; // Indica si se acabó el tiempo

    private void Start()
    {
        m_StartWait = new WaitForSeconds(m_StartDelay);
        m_EndWait = new WaitForSeconds(m_EndDelay);
        
        // Inicializar tiempo
        m_GameStartTime = Time.time;
        m_CurrentGameTime = 0f;
        
        // Inicializar derrotas de todos los tanques
        InitializeTanks();
        
        SpawnAllTanks(); // Generar tanques
        SetCameraTargets(); // Ajustar cámara
        
        // Configurar UI
        // No necesitamos configurar botones ya que usaremos solo texto
        
        StartCoroutine(GameLoop()); // Iniciar juego
    }
    
    private void Update()
    {
        if (!m_GameEnded)
        {
            UpdateTimer();
        }
    }
    
    private void InitializeTanks()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].m_Losses = 0; // Inicializar derrotas
        }
    }
    
    private void UpdateTimer()
    {
        m_CurrentGameTime = Time.time - m_GameStartTime;
        float timeLeft = m_MaxGameTime - m_CurrentGameTime;
        
        if (timeLeft <= 0 && !m_TimeUp)
        {
            m_TimeUp = true;
            EndGameByTimeout();
        }
        
        // Actualizar UI del temporizador
        if (m_TimerText != null)
        {
            int minutes = Mathf.FloorToInt(timeLeft / 60f);
            int seconds = Mathf.FloorToInt(timeLeft % 60f);
            m_TimerText.text = string.Format("Tiempo: {0:00}:{1:00}", 
                Mathf.Max(0, minutes), Mathf.Max(0, seconds));
            
            // Cambiar color si queda poco tiempo (último minuto)
            if (timeLeft <= 60f && timeLeft > 0)
            {
                m_TimerText.color = Color.red;
            }
            else if (timeLeft <= 0)
            {
                m_TimerText.color = Color.red;
                m_TimerText.text = "¡TIEMPO AGOTADO!";
            }
            else
            {
                m_TimerText.color = Color.white;
            }
        }
    }
    
    private void EndGameByTimeout()
    {
        m_GameEnded = true;
        
        // Cuando se acaba el tiempo, el jugador con más victorias gana
        TankManager timeoutWinner = GetPlayerWithMostWins();
        
        if (timeoutWinner != null)
        {
            m_GameWinner = timeoutWinner;
            string gameOverMessage = GetTimeoutWinnerMessage();
            m_MessageText.text = gameOverMessage;
        }
        else
        {
            // Empate perfecto - ambos tienen las mismas victorias
            string gameOverMessage = GetTimeoutTieMessage();
            m_MessageText.text = gameOverMessage;
        }
        
        // Iniciar corutina para reiniciar después de un tiempo
        StartCoroutine(RestartGameAfterDelay());
    }
    
    private TankManager GetPlayerWithMostWins()
    {
        if (m_Tanks.Length < 2) return null;
        
        TankManager player1 = m_Tanks[0];
        TankManager player2 = m_Tanks[1];
        
        if (player1.m_Wins > player2.m_Wins)
            return player1;
        else if (player2.m_Wins > player1.m_Wins)
            return player2;
        else
            return null; // Empate
    }

    private void SpawnAllTanks()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            // ...los creo, ajusto el número de jugador y las referencias necesarias para controlarlo
            m_Tanks[i].m_Instance =
                Instantiate(m_TankPrefab, m_Tanks[i].m_SpawnPoint.position, m_Tanks[i].m_SpawnPoint.rotation) as GameObject;
            m_Tanks[i].m_PlayerNumber = i + 1;
            m_Tanks[i].Setup();
        }
    }
    
    private void SetCameraTargets()
    {
        // Creo un array de Transforms del mismo tamaño que el número de tanques
        Transform[] targets = new Transform[m_Tanks.Length];
        // Recorro los Transforms...
        for (int i = 0; i < targets.Length; i++)
        {
            // ...lo ajusto al transform del tanque apropiado
            targets[i] = m_Tanks[i].m_Instance.transform;
        }
        // Estos son los targets que la cámara debe seguir
        m_CameraControl.m_Targets = targets;
    }
    
    // llamado al principio y en cada fase del juego después de otra
    private IEnumerator GameLoop()
    {
        while (!m_GameEnded)
        {
            // Empiezo con la corutina RoundStarting y no retorno hasta que finalice
            yield return StartCoroutine(RoundStarting());
            
            if (m_GameEnded) break;
            
            // Cuando finalice RoundStarting, empiezo con RoundPlaying y no retorno hasta que finalice
            yield return StartCoroutine(RoundPlaying());
            
            if (m_GameEnded) break;
            
            // Cuando finalice RoundPlaying, empiezo con RoundEnding y no retorno hasta que finalice
            yield return StartCoroutine(RoundEnding());
            
            if (m_GameEnded) break;
            
            // Verificar condiciones de fin de juego
            if (m_GameWinner != null)
            {
                EndGame();
                break;
            }
            
            // Verificar si algún jugador ha alcanzado el máximo de derrotas
            TankManager loser = GetPlayerWithMaxLosses();
            if (loser != null)
            {
                // El otro jugador gana
                m_GameWinner = GetOtherPlayer(loser);
                EndGame();
                break;
            }
        }
    }

    private IEnumerator RoundStarting()
    {
        if (m_GameEnded) yield break;
        
        // Cuando empiece la ronda reseteo los tanques e impido que se muevan.
        ResetAllTanks();
        DisableTankControl();
        
        // Ajusto la cámara a los tanques reseteados.
        m_CameraControl.SetStartPositionAndSize();
        
        // Incremento la ronda y muestro el texto informativo.
        m_RoundNumber++;
        m_MessageText.text = "RONDA " + m_RoundNumber;
        
        // Espero a que pase el tiempo de espera antes de volver al bucle.
        yield return m_StartWait;
    }

    private IEnumerator RoundPlaying()
    {
        if (m_GameEnded) yield break;
        
        // Cuando empiece la ronda dejo que los tanques se muevan.
        EnableTankControl();
        
        // Borro el texto de la pantalla.
        m_MessageText.text = string.Empty;
        
        // Mientras haya más de un tanque y no se haya acabado el tiempo...
        while (!OneTankLeft() && !m_TimeUp && !m_GameEnded)
        {
            // ... vuelvo al frame siguiente.
            yield return null;
        }
        
        if (m_TimeUp)
        {
            m_GameEnded = true;
        }
    }

    private IEnumerator RoundEnding()
    {
        if (m_GameEnded) yield break;
        
        // Deshabilito el movimiento de los tanques.
        DisableTankControl();
        
        // Borro al ganador de la ronda anterior.
        m_RoundWinner = null;
        
        // Miro si hay un ganador de la ronda.
        m_RoundWinner = GetRoundWinner();
        
        // Si lo hay, incremento su puntuación y las derrotas del perdedor
        if (m_RoundWinner != null)
        {
            m_RoundWinner.m_Wins++;
            
            // Incrementar derrotas del perdedor
            TankManager loser = GetOtherPlayer(m_RoundWinner);
            if (loser != null)
            {
                loser.m_Losses++;
            }
        }
        
        // Compruebo si alguien ha ganado el juego.
        m_GameWinner = GetGameWinner();
        
        // Verificar si algún jugador ha alcanzado el máximo de derrotas
        if (m_GameWinner == null)
        {
            TankManager playerWithMaxLosses = GetPlayerWithMaxLosses();
            if (playerWithMaxLosses != null)
            {
                m_GameWinner = GetOtherPlayer(playerWithMaxLosses);
            }
        }
        
        // Genero el mensaje según si hay un ganador del juego o no.
        string message = EndMessage();
        m_MessageText.text = message;
        
        // Espero a que pase el tiempo de espera antes de volver al bucle.
        yield return m_EndWait;
    }
    
    private void EndGame()
    {
        m_GameEnded = true;
        DisableTankControl();
        
        if (m_GameWinner != null)
        {
            string gameOverMessage = GetGameOverMessage();
            m_MessageText.text = gameOverMessage;
        }
        else
        {
            // Caso donde no hay ganador (empate perfecto)
            string gameOverMessage = GetTieGameMessage();
            m_MessageText.text = gameOverMessage;
        }
        
        // Iniciar corutina para reiniciar después de un tiempo
        StartCoroutine(RestartGameAfterDelay());
    }
    
    private string GetGameOverMessage()
    {
        if (m_GameWinner == null) return "";
        
        string message = "";
        message += "🏆 " + m_GameWinner.m_ColoredPlayerText + " GANA EL JUEGO! 🏆\n\n";
        
        // Estadísticas del ganador
        message += "ESTADÍSTICAS DEL GANADOR:\n";
        message += "━━━━━━━━━━━━━━━━━━━━\n";
        message += "Victorias: " + m_GameWinner.m_Wins + "\n";
        message += "Derrotas: " + m_GameWinner.m_Losses + "\n";
        
        // Calcular efectividad
        // int totalRounds = m_GameWinner.m_Wins + m_GameWinner.m_Losses;
        // if (totalRounds > 0)
        // {
        //     float winPercentage = ((float)m_GameWinner.m_Wins / totalRounds) * 100f;
        //     // message += "Efectividad: " + winPercentage.ToString("F1") + "%\n";
        // }
        
        message += "\n";
        
        // Información del oponente
        TankManager opponent = GetOtherPlayer(m_GameWinner);
        if (opponent != null)
        {
            message += opponent.m_ColoredPlayerText + " (Oponente):\n";
            message += "Victorias: " + opponent.m_Wins + " | ";
            message += "Derrotas: " + opponent.m_Losses + "\n\n";
        }
        
        // Razón de la victoria
        if (m_GameWinner.m_Wins >= m_NumRoundsToWin)
        {
            message += "Victoria por: " + m_NumRoundsToWin + " rondas ganadas\n";
        }
        else if (opponent != null && opponent.m_Losses >= m_MaxLosses)
        {
            message += "Victoria por: Oponente alcanzó " + m_MaxLosses + " derrotas\n";
        }
        
        // Tiempo de la partida
        message += "Tiempo de partida: " + GetFormattedDetailedTime() + "\n\n";
        // message += "Reiniciando en 10 segundos...";
        
        return message;
    }
    
    private string GetTimeoutWinnerMessage()
    {
        if (m_GameWinner == null) return "";
        
        string message = "";
        message += "⏰ TIEMPO AGOTADO ⏰\n\n";
        message += "🏆 " + m_GameWinner.m_ColoredPlayerText + " GANA! 🏆\n\n";
        
        // Estadísticas finales
        message += "ESTADÍSTICAS FINALES:\n";
        message += "━━━━━━━━━━━━━━━━━━━━\n";
        
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            message += m_Tanks[i].m_ColoredPlayerText + ": ";
            message += m_Tanks[i].m_Wins + " victorias, ";
            message += m_Tanks[i].m_Losses + " derrotas\n";
        }
        
        message += "\nVictoria por mejor rendimiento\n";
        message += "Tiempo total: " + GetFormattedDetailedTime() + "\n\n";
        message += "Reiniciando en 10 segundos...";
        
        return message;
    }
    
    private string GetTieGameMessage()
    {
        string message = "";
        message += "🤝 EMPATE PERFECTO 🤝\n\n";
        
        // Estadísticas finales
        message += "ESTADÍSTICAS FINALES:\n";
        message += "━━━━━━━━━━━━━━━━━━━━\n";
        
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            message += m_Tanks[i].m_ColoredPlayerText + ": ";
            message += m_Tanks[i].m_Wins + " victorias, ";
            message += m_Tanks[i].m_Losses + " derrotas\n";
        }
        
        message += "\nAmbos jugadores empataron\n";
        message += "Tiempo total: " + GetFormattedDetailedTime() + "\n\n";
        message += "Reiniciando en 10 segundos...";
        
        return message;
    }
    
    private string GetTimeoutTieMessage()
    {
        string message = "";
        message += "⏰ TIEMPO AGOTADO ⏰\n\n";
        message += "🤝 EMPATE PERFECTO 🤝\n\n";
        
        // Estadísticas finales
        message += "ESTADÍSTICAS FINALES:\n";
        message += "━━━━━━━━━━━━━━━━━━━━\n";
        
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            message += m_Tanks[i].m_ColoredPlayerText + ": ";
            message += m_Tanks[i].m_Wins + " victorias, ";
            message += m_Tanks[i].m_Losses + " derrotas\n";
        }
        
        message += "\nEmpate por mismo rendimiento\n";
        message += "Tiempo total: " + GetFormattedDetailedTime() + "\n\n";
        message += "Reiniciando en 10 segundos...";
        
        return message;
    }
    
    private IEnumerator RestartGameAfterDelay()
    {
        yield return new WaitForSeconds(10f);
        RestartGame();
    }
    
    private string GetFinalScoreText()
    {
        string scoreText = "PUNTUACIÓN FINAL:\n\n";
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            scoreText += m_Tanks[i].m_ColoredPlayerText + 
                        ": " + m_Tanks[i].m_Wins + " victorias, " + 
                        m_Tanks[i].m_Losses + " derrotas\n";
        }
        return scoreText;
    }
    
    private string GetFormattedTime()
    {
        int totalMinutes = Mathf.FloorToInt(m_CurrentGameTime / 60f);
        int totalSeconds = Mathf.FloorToInt(m_CurrentGameTime % 60f);
        return string.Format("{0:00}:{1:00}", totalMinutes, totalSeconds);
    }
    
    private string GetFormattedDetailedTime()
    {
        int totalMinutes = Mathf.FloorToInt(m_CurrentGameTime / 60f);
        int totalSeconds = Mathf.FloorToInt(m_CurrentGameTime % 60f);
        
        if (totalMinutes > 0)
            return string.Format("{0} minutos y {1} segundos", totalMinutes, totalSeconds);
        else
            return string.Format("{0} segundos", totalSeconds);
    }
    
    private TankManager GetPlayerWithMaxLosses()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            if (m_Tanks[i].m_Losses >= m_MaxLosses)
            {
                return m_Tanks[i];
            }
        }
        return null;
    }
    
    private TankManager GetOtherPlayer(TankManager player)
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            if (m_Tanks[i] != player)
            {
                return m_Tanks[i];
            }
        }
        return null;
    }

    // Usado para comprobar si queda más de un tanque.
    private bool OneTankLeft()
    {
        // Contador de tanques.
        int numTanksLeft = 0;
        // recorro los tanques...
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            // ... si está activo, incremento el contador.
            if (m_Tanks[i].m_Instance.activeSelf)
                numTanksLeft++;
        }
        // Devuelvo true si queda 1 o menos, false si queda más de uno.
        return numTanksLeft <= 1;
    }
    
    // Comprueba si algún tanque ha ganado la ronda (si queda un tanque o menos).
    private TankManager GetRoundWinner()
    {
        // Recorro los tanques...
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            // ... si solo queda uno, es el ganador y lo devuelvo.
            if (m_Tanks[i].m_Instance.activeSelf)
                return m_Tanks[i];
        }
        // SI no hay ninguno activo es un empate, así que devuelvo null.
        return null;
    }
    
    // Comprueba si hay algún ganador del juego.
    private TankManager GetGameWinner()
    {
        // Recorro los tanques...
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            // ... si alguno tiene las rondas necesarias, ha ganado y lo devuelvo.
            if (m_Tanks[i].m_Wins == m_NumRoundsToWin)
                return m_Tanks[i];
        }
        // Si no, devuelvo null.
        return null;
    }
    
    // Devuelve el texto del mensaje a mostrar al final de cada ronda.
    private string EndMessage()
    {
        // Por defecto no hay ganadores, así que es empate.
        string message = "¡EMPATE!";
        
        // Si hay un ganador de ronda cambio el mensaje.
        if (m_RoundWinner != null)
            message = m_RoundWinner.m_ColoredPlayerText + " ¡GANA LA RONDA!";
        
        // Retornos de carro.
        message += "\n\n\n\n";
        
        // Recorro los tanques y añado sus puntuaciones.
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            message += m_Tanks[i].m_ColoredPlayerText + ": " + 
                      m_Tanks[i].m_Wins + " victorias, " + 
                      m_Tanks[i].m_Losses + " derrotas\n";
        }
        
        // Si hay un ganador del juego, cambio el mensaje entero para reflejarlo.
        if (m_GameWinner != null)
        {
            message = m_GameWinner.m_ColoredPlayerText + " ¡GANA EL JUEGO!";
        }
        
        return message;
    }
    
    // Para resetear los tanques (propiedades, posiciones, etc.).
    private void ResetAllTanks()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].Reset();
        }
    }
    
    // Habilita el control del tanque
    private void EnableTankControl()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].EnableControl();
        }
    }
    
    // Deshabilita el control del tanque
    private void DisableTankControl()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].DisableControl();
        }
    }
    
    // Método para reiniciar el juego
    private void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
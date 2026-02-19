using UnityEngine;



public class TankMovement : MonoBehaviour

{

    public int m_PlayerNumber = 1;              // Utilizado para identificar qué tanque es de cada jugador.  Configurado por el tank's manager.

    public float m_Speed = 12f;                 // Rapidez con la que el tanque se mueve alante y atrás.

    public float m_TurnSpeed = 180f;            // Rapidez de giro del tanque en grados por segundo.

    public AudioSource m_MovementAudio;         // Referencia al audio del motor del tanque (diferente del de disparo).

    public AudioClip m_EngineIdling;            // Audio a reproducir cuando el tanque no se mueve.

    public AudioClip m_EngineDriving;           // Audio a reproducir cuando el tanque se está moviendo.

    public float m_PitchRange = 0.2f;           // Cantidad de variación de afinación del audio del motor (para que sea más real).





    private string m_MovementAxisName;          // Nombre del eje para moerse alante y atrás.

    private string m_TurnAxisName;              // Nombre del eje para girar.

    private Rigidbody m_Rigidbody;              // Referencia del componente para mover el tanque.

    private float m_MovementInputValue;         // Valor actual de entrada para el movimiento.

    private float m_TurnInputValue;             // Valor actual de entrada para el giro.

    private float m_OriginalPitch;              // Valor del pitch de la fuente de audio al inicio.





    private void Awake ()

    {

        m_Rigidbody = GetComponent<Rigidbody> ();

    }





    private void OnEnable ()

    {

        // Al arrancar/habilitar el tanque, deshabilitamos la kinemática del tanque para que se pueda mover.

        m_Rigidbody.isKinematic = false;



        // Reseteamos los valores de entrada.

        m_MovementInputValue = 0f;

        m_TurnInputValue = 0f;

    }





    private void OnDisable ()

    {

        // Al parar/deshabilitar el tanque, habilitamos la kinemática del tanque para que se pare.

        m_Rigidbody.isKinematic = true;

    }





    private void Start ()

    {

        // NOmbres de los ejes según el número de jugador.

        m_MovementAxisName = "Vertical" + m_PlayerNumber;

        m_TurnAxisName = "Horizontal" + m_PlayerNumber;



        // Almaceno la afinación original del audio del motor.

        m_OriginalPitch = m_MovementAudio.pitch;

    }





    private void Update ()

    {

        // Almaceno los valores de entrada.

        m_MovementInputValue = Input.GetAxis (m_MovementAxisName);

        m_TurnInputValue = Input.GetAxis (m_TurnAxisName);



        //Llamo a la función que gestiona el audio del motor

        EngineAudio ();

    }





    private void EngineAudio ()

    {

        // Si no hay entrada, es que está quieto...

        if (Mathf.Abs (m_MovementInputValue) < 0.1f && Mathf.Abs (m_TurnInputValue) < 0.1f)

        {

            // ... y si estaba reproduciendo el audio de moverse...

            if (m_MovementAudio.clip == m_EngineDriving)

            {

                // ... cambio el audio al de estar parado y lo reproduzco.

                m_MovementAudio.clip = m_EngineIdling;

                m_MovementAudio.pitch = Random.Range (m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);

                m_MovementAudio.Play ();

            }

        }

        else

        {

            //Si hay entrada es que se está moviendo. Si estaba reproduciendo eld e estar parado...

            if (m_MovementAudio.clip == m_EngineIdling)

            {

                // ... cambio el audio al de moverse y lo reproduzco.

                m_MovementAudio.clip = m_EngineDriving;

                m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);

                m_MovementAudio.Play();

            }

        }

    }





    private void FixedUpdate ()

    {

        // Ajusto la posición y rotación.

        Move ();

        Turn ();

    }





    private void Move ()

    {

        // Creo un vector en la dirección en la que apunta el tanque, con una magnitud basada en la entrada, la velocidad y el tiempo entre frames.

        Vector3 movement = transform.forward * m_MovementInputValue * m_Speed * Time.deltaTime;



        // Aplico ese vector de movimiento al rigidbody del tanque.

        m_Rigidbody.MovePosition(m_Rigidbody.position + movement);

    }





    private void Turn ()

    {

        // Calculo el número de grados de rotación basandome en la entrada, la velocidad y el tiempo entre frames.

        float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime;



        // Convierto ese número en una rotación en el eje Y.

        Quaternion turnRotation = Quaternion.Euler (0f, turn, 0f);



        // AAplico esa rotación al rigidbody del tanque.

        m_Rigidbody.MoveRotation (m_Rigidbody.rotation * turnRotation);

    }

}
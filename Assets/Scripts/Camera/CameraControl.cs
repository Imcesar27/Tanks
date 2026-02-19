using UnityEngine;



public class CameraControl : MonoBehaviour

{

    public float m_DampTime = 0.2f; //Tiempo de espera para mover la cámara

    public float m_ScreenEdgeBuffer = 4f; //Pequeño padding para que los tanques no se pegen a los bordes

    public float m_MinSize = 6.5f; //Tamaño mínimo de zoom

    [HideInInspector] public Transform[] m_Targets; //Array de tanques, no se mostrarán en el inspector cuando haya Game Manager





    private Camera m_Camera; //la cámara

    private float m_ZoomSpeed; //velocidad de zoom

    private Vector3 m_MoveVelocity; //velocidad de movimiento

    private Vector3 m_DesiredPosition; //posición a la que quiero llegar





    private void Awake()

    {

        //Al arrrancar cogemos la cámara

        m_Camera = GetComponentInChildren<Camera>();

    }





    private void FixedUpdate()

    {

        Move(); //Mueve la cámara

        Zoom(); //ajusta el tamaño de la cámara

    }





    private void Move()

    {

        //Busco la posicón intermedia entre los dos tanques

        FindAveragePosition();



        //Muevo la cámara de forma suave

        transform.position = Vector3.SmoothDamp(transform.position, m_DesiredPosition, ref m_MoveVelocity, m_DampTime);

    }





    private void FindAveragePosition()

    {

        Vector3 averagePos = new Vector3();

        int numTargets = 0;



        //Recorre la cantidad de tanques activos, captura su posición y asigna a m_DesiredPosition el punto medio entre ellos (en el eje Y)

        for (int i = 0; i < m_Targets.Length; i++)

        {

            //Si no está activo me lo salto

            if (!m_Targets[i].gameObject.activeSelf)

                continue;



            //incremento el valor a la media y el número de elementos

            averagePos += m_Targets[i].position;

            numTargets++;

        }



        //Si hay elementos, hago la media

        if (numTargets > 0)

            averagePos /= numTargets;



        //mantengo el valor de y

        averagePos.y = transform.position.y;



        //La posición deseada es la media

        m_DesiredPosition = averagePos;

    }





    private void Zoom()

    {

        //Buscamos la posición requerida de zoom (size) y la asignamos a la cámara

        float requiredSize = FindRequiredSize();

        //Ajusto el tamaño de la cámara de forma suave

        m_Camera.orthographicSize = Mathf.SmoothDamp(m_Camera.orthographicSize, requiredSize, ref m_ZoomSpeed, m_DampTime);

    }





    private float FindRequiredSize()

    {

        //Teniendo en cuenta la posición deseada

        Vector3 desiredLocalPos = transform.InverseTransformPoint(m_DesiredPosition);



        float size = 0f;



        //recorremos los tanques activos y cojemos la posición más alta (el que estaría más lejos del centro)

        for (int i = 0; i < m_Targets.Length; i++)

        {

            //Si no está activo me lo salto

            if (!m_Targets[i].gameObject.activeSelf)

                continue;



            //posición del tanque en el espacio de la cámara

            Vector3 targetLocalPos = transform.InverseTransformPoint(m_Targets[i].position);



            //diferencia entre la deseada y la actual

            Vector3 desiredPosToTarget = targetLocalPos - desiredLocalPos;



            //escojo el máximo entre el tamaño de cámara actual y la distancia del tanque (arriba o abajo)

            size = Mathf.Max (size, Mathf.Abs (desiredPosToTarget.y));



            //escojo el máximo entre el tamaño de cámara actual y la distancia del tanque (izqueirda o derecha)

            size = Mathf.Max (size, Mathf.Abs (desiredPosToTarget.x) / m_Camera.aspect);

        }



        //Aplicamos el padding

        size += m_ScreenEdgeBuffer;



        //Comprobamos que al menos tenemos el zoom mínimo

        size = Mathf.Max(size, m_MinSize);



        return size;

    }



    //La usaremos en el GameManager para resetear la posición y el zoom en cada escena

    public void SetStartPositionAndSize()

    {

        //Buscamos la posición deseada

        FindAveragePosition();



        //ajustamos la posición de la cámara (sin damping porque va a ser al entrar)

        transform.position = m_DesiredPosition;



        //buscamos y ajustamos el tamaño de la cámara

        m_Camera.orthographicSize = FindRequiredSize();

    }

}

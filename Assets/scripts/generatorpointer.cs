using UnityEngine;

public class generatorpointer : MonoBehaviour
{
    public float speed = 100f;

    private float currentAngle = 315f;
    private bool movingLeft = true;

    void Start()
    {
        currentAngle = 315f;
    }

    void Update()
    {
        if (movingLeft)
            currentAngle -= speed * Time.deltaTime;
        else
            currentAngle += speed * Time.deltaTime;

        if (currentAngle <= 135f)
        {
            currentAngle = 135f;
            movingLeft = false;
        }

        if (currentAngle >= 315f)
        {
            currentAngle = 315f;
            movingLeft = true;
        }

        transform.localRotation =
            Quaternion.Euler(0f, 0f, currentAngle);
    }
    public float GetCurrentAngle()
    {
        return currentAngle;
    }
}

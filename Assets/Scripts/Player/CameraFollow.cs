using UnityEngine;

public class CameraFollow : MonoBehaviour // カメラをプレイヤーに追従する
{
    [SerializeField] Transform target;

    void LateUpdate()
    {
        transform.position = new Vector3(
            target.position.x,
            target.position.y,
            -10f
        );
    }
}
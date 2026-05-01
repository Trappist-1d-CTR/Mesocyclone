using UnityEngine;

public class DetectPlayer : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            transform.parent.SendMessage("ShelterHangar");
        }
    }
}

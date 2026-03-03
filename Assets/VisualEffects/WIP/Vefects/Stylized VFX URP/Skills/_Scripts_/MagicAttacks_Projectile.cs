using System.Collections;
using System.Collections.Generic;
using UnityEngine.VFX;
using UnityEngine;

public class MagicAttacks_Projectile : MonoBehaviour
{
    private Vector3 projectileDir;
    public GameObject FX_Hit;

    VisualEffect FX_Projectile;
    VisualEffect FX_ProjectileTail;

    AudioSource SFX_Projectile;

    /// <summary>Performs initial setup after all Awake calls complete.</summary>
    private void Start()
    {
        FX_Projectile = gameObject.transform.GetChild(0).GetComponent<VisualEffect>();
        FX_ProjectileTail = gameObject.transform.GetChild(1).GetComponent<VisualEffect>();
        SFX_Projectile = gameObject.GetComponent<AudioSource>();

    }

    /// <summary>Sets the up.</summary>
    public void Setup(Vector3 projectileDir)
    {
        this.projectileDir = projectileDir;
    }

    /// <summary>Runs per-frame update logic.</summary>
    private void Update()
    {
        float moveSpeed = 60f;
        transform.position += projectileDir * moveSpeed * Time.deltaTime;
        Destroy(gameObject, 5f);
    }

    /// <summary>Handles the trigger enter event.</summary>
    private void OnTriggerEnter(Collider col)
    {
        Instantiate(FX_Hit, col.transform.position, Quaternion.identity);
        
        Destroy(FX_Projectile);
        FX_ProjectileTail.Stop();
        SFX_Projectile.Stop();

        Destroy(gameObject, 3f);
    }
}

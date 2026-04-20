using UnityEngine;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Config;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;

namespace Scripts.Utilities
{
public class Demo_FreeCam : MonoBehaviour
{
    // All tuning values live in Scripts.Data.Config.DemoFreeCamConfig.

    private float doubleClickTime = .15f;
    private float cooldown = 0;

    //Cache last pos and rot be able to undo last focus object action.
    Quaternion prevRot = new Quaternion();
    Vector3 prevPos = new Vector3();

    /// <summary>Performs initial setup after all Awake calls complete.</summary>
    private void Start()
    {
        SavePosAndRot();
    }

    void Update()
    {
        if (!DemoFreeCamConfig.DoFocus)
            return;

        //Double click for focus
        if (cooldown > 0 && Input.GetKeyDown(KeyCode.Mouse0))
            FocusObject();
        if (Input.GetKeyDown(KeyCode.Mouse0))
            cooldown = doubleClickTime;

        //--------UNDO FOCUS---------
        if (Input.GetKey(DemoFreeCamConfig.FirstUndoKey))
        {
            if (Input.GetKeyDown(DemoFreeCamConfig.SecondUndoKey))
                GoBackToLastPosition();
        }

        cooldown -= Time.deltaTime;
    }

    /// <summary>Runs per-frame logic after all Update calls.</summary>
    private void LateUpdate()
    {
        Vector3 move = Vector3.zero;

        //Move and rotate the camera

        if (Input.GetKey(DemoFreeCamConfig.ForwardKey))
            move += Vector3.forward * DemoFreeCamConfig.MoveSpeed;
        if (Input.GetKey(DemoFreeCamConfig.BackKey))
            move += Vector3.back * DemoFreeCamConfig.MoveSpeed;
        if (Input.GetKey(DemoFreeCamConfig.LeftKey))
            move += Vector3.left * DemoFreeCamConfig.MoveSpeed;
        if (Input.GetKey(DemoFreeCamConfig.RightKey))
            move += Vector3.right * DemoFreeCamConfig.MoveSpeed;

        //By far the simplest solution I could come up with for moving only on the Horizontal plane - no rotation, just cache y
        if (Input.GetKey(DemoFreeCamConfig.FlatMoveKey))
        {
            float origY = transform.position.y;

            transform.Translate(move);
            transform.position = new Vector3(transform.position.x, origY, transform.position.z);

            return;
        }

        float mouseMoveY = Input.GetAxis(DemoFreeCamConfig.MouseY);
        float mouseMoveX = Input.GetAxis(DemoFreeCamConfig.MouseX);

        //Move the camera when anchored
        if (Input.GetKey(DemoFreeCamConfig.AnchoredMoveKey))
        {
            move += Vector3.up * mouseMoveY * -DemoFreeCamConfig.MoveSpeed;
            move += Vector3.right * mouseMoveX * -DemoFreeCamConfig.MoveSpeed;
        }

        //Rotate the camera when anchored
        if (Input.GetKey(DemoFreeCamConfig.AnchoredRotateKey))
        {
            transform.RotateAround(transform.position, transform.right, mouseMoveY * -DemoFreeCamConfig.RotationSpeed);
            transform.RotateAround(transform.position, Vector3.up, mouseMoveX * DemoFreeCamConfig.RotationSpeed);
        }

        transform.Translate(move);

        //Scroll to zoom
        float mouseScroll = Input.GetAxis(DemoFreeCamConfig.ZoomAxis);
        transform.Translate(Vector3.forward * mouseScroll * DemoFreeCamConfig.ZoomSpeed);
    }

    /// <summary>Focus object.</summary>
    private void FocusObject()
    {
        //To be able to undo
        SavePosAndRot();

        //If we double-clicked an object in the scene, go to its position
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, DemoFreeCamConfig.FocusLimit))
        {
            GameObject target = hit.collider.gameObject;
            Vector3 targetPos = target.transform.position;
            Vector3 targetSize = hit.collider.bounds.size;

            transform.position = targetPos + GetOffset(targetPos, targetSize);

            transform.LookAt(target.transform);
        }
    }

    /// <summary>Save pos and rot.</summary>
    private void SavePosAndRot()
    {
        prevRot = transform.rotation;
        prevPos = transform.position;
    }

    /// <summary>Go back to last position.</summary>
    private void GoBackToLastPosition()
    {
        transform.position = prevPos;
        transform.rotation = prevRot;
    }

    /// <summary>Gets the offset.</summary>
    private Vector3 GetOffset(Vector3 targetPos, Vector3 targetSize)
    {
        Vector3 dirToTarget = targetPos - transform.position;

        float focusDistance = Mathf.Max(targetSize.x, targetSize.z);
        focusDistance = Mathf.Clamp(focusDistance, DemoFreeCamConfig.MinFocusDistance, focusDistance);

        return -dirToTarget.normalized * focusDistance;
    }
}

}
